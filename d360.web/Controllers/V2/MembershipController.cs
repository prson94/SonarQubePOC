using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.entities.Views;
using d360.model;
using d360.model.DataAccessLayer;
using d360.model.helpers;
using d360.model.helpers.filters;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Dapper;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using SpreadsheetLight;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using static d360.core.entities.Resource;
namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/membership"),
        Authorize,
        StringEnumController
    ]
    public class MembershipController : BaseV2ApiController
    {
        ICompanyContext _company;
        IMembershipRepository membershipRepository;
        IAssetRepository assetRepository;
        public MembershipController(ICommunityContext community, ICompanyContext company, IMembershipRepository membershipRepository, IAssetRepository assetRepository)
            : base(community, company)
        {
            _company = company;
            this.membershipRepository = membershipRepository;
            this.assetRepository = assetRepository;
        }
        /// <summary>
        /// Retrieves a list of users.
        /// Advanced filtering is done using _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
        /// *  For comparison operators you can use eq (equal), ne (not equal), gt (greater than), ge (greater than or equal), lt (less than), le (less than or equal) and ct (contains) which allows usage of (*) symbol as wildcard
        /// *  Chaining of filter expressions is done using 'and' or 'or' logical operator. IE. city eq 'Redmond' OR city ct 'Lo'.
        /// 
        /// </summary>
        /// <param name="Uid">The uid of the user.</param>
        /// <param name="ResourceID">The id of the user.</param>
        /// <param name="FirstName">First Name of user.</param>
        /// <param name="LastName">Last Name of user.</param>
        /// <param name="State">Select the state of the user from the options in the dropdown.</param>
        /// <param name="IsAdministrator">Is the user an adminstrator or not.</param>
        /// <param name="_pageSize">The number of results to return per page. The default is 5 users per page and max value is 250.</param>
        /// <param name="_pageNum">The page number to return results for.</param>
        /// <param name="_order">The order field to return results by.</param>
        /// <param name="_direction">The direction in which to return results by asc/desc. </param>
        /// <param name="_filter">The filter expression used to filter assets by all listable and non-listable fields. Asterisk (*) symbol can be used as a wild card character to match any character.</param>
        /// <param name="_simpleFilter">The text or phrase you want to find within the listable fields of an asset. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.</param>
        /// <param name="_includeOrganization">Include the users organization uid if they are part of an organization.</param>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("users"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.OK, "Gets a list of Users.", typeof(ResourceApiViewModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid PageSize/PageNum value provided. Number is too large"),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetUsers(Guid? Uid = null, int? ResourceID = null, string FirstName = null, string LastName = null, core.enums.CompanyResourceState? State = null, bool? IsAdministrator = null, string _pageSize = "5", string _pageNum = "1", string _order = "ResourceID", string _direction = "asc", string _filter = "", string _simpleFilter = "", bool _includeOrganization = false)
        {
            try
            {

                var settings = Community.GetCompanySettings();
                bool IsCurrentUser = false;

                if (ResourceID != null)
                {
                    if (ResourceID == Company.CurrentResourceID)
                    {
                        IsCurrentUser = true;
                    }
                }

                if (!Company.CurrentResourceIsAdmin && (settings["ShowResources"] ?? "").ToUpper() != "TRUE" && IsCurrentUser == false)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, "Forbidden", $"Access denied"));

                string finalSql = "";
                string joinsSql = $@" outer apply (select object,objectid from Asset A1 where A1.Object = 'Resource' and A1.ObjectID = gr.ResourceID) A 
                                        {(_includeOrganization ?
                                                    @" left join dbo.OrganizationResource org on org.ResourceID = GR.ResourceID 
                                                    left join dbo.asset ao on ao.Object like 'Organization' and ao.ObjectID = org.OrganizationID "
                                    : "")}";
                string whereSql = "";
                string selectSql = $@"select
                    gr.uid,
                    {(_includeOrganization ? " ao.Uid as OrganizationUid, " : "")} 
                    gr.ResourceID, 
                    gr.FirstName, 
                    gr.LastName,
                    gr.Email,
                    gr.IsAdministrator,
                    gr.LastLoggedInOn, 
                    case gr.State 
                         when 1 then 'Active'
                         when 2 then 'InActive'
                         when 3 then 'Deleted' end as State,
                    gr.CreatedOn";
                string countSql = "select count(*) from [reporting].[Global_Resource] gr ";
                string orderBySQL = $"";
                long pageSize;
                long pageNum;

                DynamicParameters dbArgs = new DynamicParameters();
                List<string> queries = new List<string>();
                ResourceApiViewModel model = new ResourceApiViewModel();
                List<string> fieldColumns = new List<string>();
                List<string> fieldJoins = new List<string>();
                Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", _pageSize }, { "_pageNum", _pageNum } };
                string isValid = isPageSizeAndNumValid(pageParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad request submitted", isValid));
                }

                var fieldTypes = _company.FieldTypes.Where(f => f.Object == "ResourceType" && f.ObjectID == 1).ToList();

                IDictionary<string, string> customFields = new Dictionary<string, string>();
                var queryParams = Request.GetQueryNameValuePairs();
                getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);

                if (Uid != null || ResourceID != null || FirstName != null || LastName != null || State != null || IsAdministrator != null)
                {
                    if (Uid != null)
                    {
                        dbArgs.Add("uid", Uid);
                        queries.Add(" gr.uid = @uid");
                    }
                    if (ResourceID != null)
                    {
                        dbArgs.Add("ResourceID", ResourceID);
                        queries.Add(" gr.ResourceID = @ResourceID");
                    }
                    if (FirstName != null)
                    {
                        dbArgs.Add("FirstName", FirstName);
                        queries.Add(" FirstName = @FirstName");
                    }
                    if (LastName != null)
                    {
                        dbArgs.Add("LastName", LastName);
                        queries.Add(" LastName = @LastName");
                    }
                    if (State != null)
                    {
                        dbArgs.Add("state", State);
                        queries.Add(" gr.state = @state");
                    }
                    if (IsAdministrator != null)
                    {
                        dbArgs.Add("isAdministrator", IsAdministrator);
                        queries.Add(" isAdministrator = @isAdministrator");
                    }
                }
                foreach (var col in fieldColumns)
                {
                    selectSql += "," + col;
                }
                foreach (var join in fieldJoins)
                {
                    joinsSql += " " + join;
                }
                foreach (FieldType customField in fieldTypes)
                {
                    if (queryParams.Any(x => x.Key == customField.Name))
                    {
                        var paramval = queryParams.FirstOrDefault(x => x.Key == customField.Name).Value;
                        queries.Add($"F{customField.ID}.FormattedValue = @field{customField.ID}");
                        dbArgs.Add($"@field{customField.ID}", paramval);
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "_filter"))
                {
                    var filterValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;

                    if (!string.IsNullOrEmpty(filterValue))
                    {
                        var filterExpressionParser = new FilterExpressionParser(Company, FilterExpressionParseType.CustomFields, false, true);
                        filterExpressionParser.LoadFieldTypes(fieldTypes, fieldColumns);
                        Dictionary<string, object> sqlParams = new Dictionary<string, object>();
                        List<int> filteredFieldIds = new List<int>();
                        queries.Add("(" + filterExpressionParser.Parse(filterValue, out sqlParams, out filteredFieldIds) + ")");

                        foreach (var item in sqlParams)
                        {
                            dbArgs.Add(item.Key, item.Value);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(_simpleFilter))
                {
                    dbArgs.Add("@simpleFilter", "%" + _simpleFilter + "%");
                    List<string> simpleFilters = new List<string>();

                    _simpleFilter = Company.GetEscapedFilterString(_simpleFilter);

                    foreach (var ft in fieldTypes.Where(x => x.IsListable == true))
                    {
                        if (ft.Type == "Lookup" && ft.AllowAllValue)
                        {
                            simpleFilters.Add($"(select case when F{ft.ID}.[Value] = '0' then @F{ft.ID}_AllValue else F{ft.ID}.FormattedValue end as value) like @simpleFilter");
                        }
                        else
                        {
                            simpleFilters.Add($"F{ft.ID}.FormattedValue like @simpleFilter");
                        }
                    }

                    List<string> defaultFields = new List<string> { "FirstName", "LastName", "Email", "IsAdministrator", "LastLoggedInOn", "CreatedOn" };

                    defaultFields.ForEach(f =>
                    {
                        if (f == "CreatedOn" || f == "LastLoggedInOn")
                        {
                            simpleFilters.Add($"cast({f} as datetime2) like @simpleFilter");
                        }
                        else
                        {
                            simpleFilters.Add($"{f} like @simpleFilter");
                        }
                    });

                    simpleFilters.Add(@"(case gr.State 
                     when 1 then 'Active'
                     when 2 then 'InActive'
                     when 3 then 'Deleted' end) like @simpleFilter");
                    queries.Add("(" + string.Join(" or ", simpleFilters) + ")");
                }

                if (Community.GetCompanySettingByKey<bool>("HideData3SixtyUsers"))
                {
                    queries.Add("email not like '%@infogix.com'");
                }

                if (queries.Count() > 0)
                {
                    whereSql += "where ";
                }

                for (int i = 0; i < queries.Count(); i++)
                {
                    whereSql += queries[i].ToString();
                    if (i < queries.Count() - 1)
                    {
                        whereSql += " and ";
                    }
                }
                List<string> validCols = new List<string> { "uid", "ResourceID", "FirstName", "LastName", "Email", "IsAdministrator", "LastLoggedInOn", "State", "CreatedOn" };
                validCols.AddRange(fieldTypes.Select(x => x.Name));

                if (validCols.All(x => x.ToLower() != _order.ToLower()))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad request submitted", "Invalid order by passed in the request"));

                if (!new string[] { "asc", "desc" }.Contains(_direction.ToLower()))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad request submitted", "Invalid order passed in the request"));

                orderBySQL = $"order by {_order} {_direction}";

                finalSql = selectSql + " from[reporting].[Global_Resource] gr " + joinsSql + " " + whereSql;
                countSql += joinsSql + " " + whereSql;

                long.TryParse(_pageSize, out pageSize);
                long.TryParse(_pageNum, out pageNum);

                model.pageNum = pageNum;
                model.pageSize = pageSize;
                string offsetSql = $" {orderBySQL} offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
                finalSql += offsetSql;

                var results = await Company.QueryAsync<dynamic>(finalSql, dbArgs, ApiTimeout);
                var countResults = await Company.QueryAsync<int>(countSql, dbArgs, ApiTimeout);

                var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;

                if (isStreamResponse)
                {
                    byte[] xlsResult = GetUsersExcelFromResults(results, fieldTypes);

                    var response = createFileResponseMessage(HttpStatusCode.OK, $"Users {System.DateTime.Now.ToShortDateString()}.xlsx", xlsResult);
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(response));

                }
                else
                {
                    model.items = results;
                    model.total = countResults.FirstOrDefault();
                    var response = Request.CreateResponse(HttpStatusCode.OK, model);
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(response));

                }

            }
            catch (FilterExpressionParserException ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Filter expression parse error", errorMessage));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Adds members to a group for a given group unique identifier.
        /// </summary>
        /// <param name="groupUid">The unique identifier of the Group.</param>
        /// <param name="users">The users that need to be added to the group</param>
        [
           HttpPost,
           MapToApiVersion("2.0"),
           Route("groups/{groupUid:Guid}/members"),
           SwaggerRequestExample(typeof(InsertUserToGroup), typeof(InsertUserToGroupExample)),
           SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
           SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request made, users not added to group", typeof(ErrorResponse)),
           SwaggerResponse(HttpStatusCode.NotFound, "Group or user(s) provided not found", typeof(ErrorResponse)),
           SwaggerResponse(HttpStatusCode.OK, "Members added to group.", typeof(List<Guid>)),
           SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<HttpResponseMessage> AddMembers(Guid groupUid, List<InsertUserToGroup> users)
        {
            var kvpGroupUid = new Dictionary<string, string> { { "Uid", groupUid.ToString() } };

            if (groupUid == Guid.Empty)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Group Uid passed in is empty. Please provided a valid group"));

            var isValidGroup = await this.membershipRepository.GetGroups(kvpGroupUid);

            List<ResourceGroup> resourceGroups = new List<ResourceGroup>();

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Access Denied"));

            if (isValidGroup.Total == 0)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Group UID provided is not a valid group UID. Group does not exist."));

            if (isValidGroup.items?.First()?.IsActiveDirectoryGroup == true)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Group UID provided is an active directory group and cannot be managed manually."));

            var duplicatedUsers = from u in users group u by u.Uid into user where user.Count() > 1 select user.Key;

            if (duplicatedUsers.Count() != 0)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Same User UID appears multiple times."));
            }

            if (users.Count == 0)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No user UIDs provided."));

            var id = Company.Filter<Asset>(x => x.uid == groupUid).SingleOrDefault().ObjectID;

            foreach (var user in users)
            {
                var userUid = new Dictionary<string, string> { { "Uid", user.Uid.ToString() } }; ;
                bool isValid = this.IsValidGuid(userUid, "uid");

                if (!isValid)
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "One or more user UIDs do not exist."));

                var isUser = this.assetRepository.GetAssetByUID(user.Uid);

                if (isUser == null || isUser.Object != "Resource")
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "One or more user UIDs passed in are not a user."));


                var isMember = Company.Filter<ResourceGroup>(x => x.GroupID == id && x.ResourceID == isUser.ObjectID).SingleOrDefault();

                if (isMember != null)
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"User {user.Uid.ToString()} is already a member of this group"));

                resourceGroups.Add(new ResourceGroup { GroupID = id, ResourceID = isUser.ObjectID });
            }

            try
            {
                foreach (var m in resourceGroups)
                    Company.Add(m);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage));
            }

            return Request.CreateResponse(HttpStatusCode.OK, users);
        }

        /// <summary>
        /// Retrieves members of a group for a given group unique identifier.
        /// </summary>
        /// <param name="groupUid">The unique identifier of the Group.</param>
        [
           HttpGet,
           MapToApiVersion("2.0"),
           Route("groups/{groupUid:Guid}/members"),
           SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
           SwaggerResponse(HttpStatusCode.OK, "Gets Members of a Group.", typeof(ResourceApiViewModel)),
           SwaggerResponse(HttpStatusCode.BadRequest, "Invalid PageSize/PageNum value provided. Number is too large"),
           SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
           SwaggerParameter("_firstName", "The First Name of the user.", DataType = "string", ParameterType = "query", Required = false),
           SwaggerParameter("_lastName", "The last name of the user.", DataType = "string", ParameterType = "query", Required = false),
           SwaggerParameter("_pageSize", "The number of results to return per page. The default is 5 users per page and max value is 250.", DataType = "integer", ParameterType = "query", Required = false),
           SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
       ]
        public async Task<HttpResponseMessage> GetMembers(Guid groupUid)
        {
            string finalSql = "";
            string joinsSql = " left join Asset A on A.Object = 'Resource' and A.ObjectID = gr.ResourceID ";
            string whereSql = "";
            List<string> fieldColumns = new List<string>();
            List<string> fieldJoins = new List<string>();
            string selectSql = $@"select gr.uid, 
                gr.ResourceID, gr.FirstName, gr.LastName, gr.Email, 
                gr.IsAdministrator, gr.LastLoggedInOn, 
                case 
                    when g.PrimaryOwnerResourceID = gr.ResourceID then 'Primary' 
                    when g.SecondaryOwnerResourceID = gr.ResourceID then 'Secondary' 
                    else null end 
                as [Owner],
                case gr.State 
                    when 1 then 'Active' 
                    when 2 then 'InActive'
                    when 3 then 'Deleted' end 
                as State ";
            string countSql = @"
                           select count(*)
                                   from[reporting].[Global_Resource] as gr
                                       inner join [dbo].[ResourceGroup] rg on rg.ResourceID = gr.ResourceID
                                       inner join [dbo].[Group] g on g.ID = rg.GroupID
									   inner join [dbo].[Asset] AB on AB.uid = '"
                                    + groupUid + "'";
            var firstName = "";
            var lastName = "";
            string pageSize = "5";
            string pageNum = "1";
            long _pageSize;
            long _pageNum;
            DynamicParameters dbArgs = new DynamicParameters();
            ResourceApiViewModel model = new ResourceApiViewModel();
            var queryParams = Request.GetQueryNameValuePairs();
            queryParams.ToList().ForEach(q =>
            {
                var key = q.Key.ToLower();
                if (key.StartsWith("_"))
                {
                    switch (key)
                    {
                        case "_firstname":
                            firstName = q.Value;
                            dbArgs.Add("firstName", q.Value);
                            whereSql += " and gr.FirstName = @firstName";
                            countSql += " and gr.FirstName = @firstName";
                            break;
                        case "_lastname":
                            lastName = q.Value;
                            dbArgs.Add("lastName", q.Value);
                            whereSql += " and gr.lastName = @lastName";
                            countSql += " and gr.LastName = @lastName";
                            break;
                        case "_pagesize":
                            pageSize = q.Value;
                            break;
                        case "_pagenum":
                            pageNum = q.Value;
                            break;
                    }
                }
            });

            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };
            string isValid = isPageSizeAndNumValid(pageParams);

            if (!string.IsNullOrEmpty(isValid))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, isValid));
            }

            var fieldTypes = _company.FieldTypes.Where(f => f.Object == "ResourceType" && f.ObjectID == 1).ToList();
            getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);
            foreach (var col in fieldColumns)
            {
                selectSql += "," + col;
            }
            foreach (var join in fieldJoins)
            {
                joinsSql += join;
            }

            foreach (FieldType customField in fieldTypes)
            {
                if (queryParams.Any(x => x.Key == customField.Name))
                {
                    var paramval = queryParams.FirstOrDefault(x => x.Key == customField.Name).Value;
                    whereSql += $" and F{customField.ID}.FormattedValue = @field{customField.ID}";
                    dbArgs.Add($"@field{customField.ID}", paramval);
                }
            }
            finalSql = selectSql + @" from[reporting].[Global_Resource] gr inner join [dbo].[ResourceGroup] rg on rg.ResourceID = gr.ResourceID 
                                      inner join[dbo].[Group] g on g.ID = rg.GroupID
                                      inner join[dbo].[Asset] AB on AB.uid = '"
                                      + groupUid + "'" + joinsSql + " where g.ID = AB.ObjectID" + whereSql;
            countSql += joinsSql + " where g.ID = AB.ObjectID" + whereSql;

            long.TryParse(pageSize, out _pageSize);
            long.TryParse(pageNum, out _pageNum);

            model.pageNum = _pageNum;
            model.pageSize = _pageSize;
            string offsetSql = $" Order by gr.ResourceID offset {_pageSize * (_pageNum - 1)} rows fetch next {_pageSize} rows only";
            finalSql += offsetSql;
            var results = await Company.QueryAsync<dynamic>(finalSql, dbArgs, ApiTimeout);
            var count = await Company.QueryAsync<int>(countSql, dbArgs, ApiTimeout);
            model.items = results;
            model.total = count.FirstOrDefault();
            return Request.CreateResponse(HttpStatusCode.OK, model);
        }

        [
           HttpGet,
           MapToApiVersion("2.0"),
           Route("groups/{groupId:int}"),
           ApiExplorerSettings(IgnoreApi = true)
       ]
        public async Task<HttpResponseMessage> GetGroupUid(int groupId)
        {
            string sql = $"SELECT uid FROM[dbo].[Asset] where Object = 'Group' and ObjectID =" + groupId;

            var results = await Company.QueryAsync<dynamic>(sql, ApiTimeout);
            return Request.CreateResponse(HttpStatusCode.OK, results);
        }

        /// <summary>
        /// Retrieves a list of groups
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("groups"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(GroupApiModels)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("Uid", "Uid of the group.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("Name", "Name of the group", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("ResourceUid", "Uid of user", DataType = "string", ParameterType = "query", Required = false)
        ]
        public async Task<IHttpActionResult> GetGroups()
        {
            var prefix = "Membership.GetGroups => ";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

                if (!this.IsValidGuid(queryParams, "uid"))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid uid is passed in the request"));
                }
                if (!this.IsValidGuid(queryParams, "resourceuid"))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid Resource Uid provided in request"));
                }
                var results = await this.membershipRepository.GetGroups(queryParams);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }


        /// <summary>
        /// Deletes the specified user from the specified group.
        /// </summary>
        /// <param name="groupUid">The unique identifier of the Group.</param>
        /// <param name="resourceUid">The unique identifier of the resource.</param>
        [
            HttpDelete,
            Route("groups/{groupUid:Guid}/{resourceUid:Guid}"),
            SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource / Group doesn't exist.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - Provided group could not be updated", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))

        ]
        public async Task<IHttpActionResult> DeleteGroupMember(Guid groupUid, Guid resourceUid)
        {
            var prefix = "Membership.DeleteGroupMember => ";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Unauthorized", "Access Denied."));

                var group = (await Company.QueryAsync<Group>(@"
select G.* from [Group] G 
inner join Asset a on A.Object = 'Group' and A.ObjectID = G.ID 
where a.uid = @groupUid", new { groupUid })).FirstOrDefault();

                var userId = _company.Assets.FirstOrDefault(x => x.Object == "Resource" && x.uid == resourceUid)?.ObjectID ?? 0;

                if (group?.PrimaryOwnerResourceID == userId)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", "Cannot delete Primary Owner of group."));

                var res = await Company.Database.Connection.ExecuteAsync(@"delete rg from [dbo].[ResourceGroup] rg inner join[reporting].[Global_Resource] gr on gr.uid = @resource inner join[dbo].[Asset] a on a.uid = @group and a.object = 'Group' inner join[dbo].[Group] g on g.ID = a.ObjectID where rg.ResourceID = gr.ResourceID and rg.GroupID = g.ID;  
                        Update G set  G.SecondaryOwnerResourceID = null
                        from[Group] AS G
                        inner join[dbo].[Asset] a on a.uid = @group and a.object = 'Group'
                        where G.ID = A.ObjectID and G.SecondaryOwnerResourceID = @user", new { resource = resourceUid, group = groupUid, user = userId });

                if (res > 0) return successMessageResponse(HttpStatusCode.OK, "User removed.", "User removed from group."); // deleted
                else return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", "Resource / Group doesn't exist"));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Deletes the specified users from Govern.
        /// </summary>
        /// <param name="users">A list of uids for users to delete.</param>
        [
            HttpDelete,
            Route("users"),
            SwaggerRequestExample(typeof(DeleteUserModel), typeof(DeleteUserExample)),
            SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource doesn't exist.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteUsers(List<DeleteUserModel> users)
        {
            var prefix = "Membership.DeleteUsers => ";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Unauthorized", $"Access denied"));

                if (users == null || users.Count() == 0)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad request submitted", $"No users provided in request."));

                List<UserApiDeleteModel> resources = new List<UserApiDeleteModel>();

                foreach (var u in users)
                {
                    resources.Add(new UserApiDeleteModel()
                    {
                        Uid = u.Uid
                    });
                }

                var execution = getApiExecution(users.Count);
                var result = membershipRepository.DeleteResources(execution, resources);

                if (result.StatusCode != HttpStatusCode.OK)
                    return await Task.FromResult(errorMessageResponse(result.StatusCode, result.Error, result.Message));

                return await Task.FromResult(successMessageResponse(result.StatusCode, "Success", result.Message));

            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }


        /// <summary>
        /// Adds the specified users.
        /// </summary>
        /// <remarks>
        ///###Users###
        /// <table>
        /// <tr><td>**Field**</td><td>**Required / Optional**</td><td>**Description**</td><td>**Validation**</td></tr>
        /// <tr><td>Username</td><td>Required</td><td>The email the user will use to login</td><td>Must be in a valid email format</td></tr>
        /// <tr><td>Firstname</td><td>Required</td><td>First name of the user</td><td></td></tr>
        /// <tr><td>Lastname</td><td>Required</td><td>Last name of the user</td><td></td></tr>
        /// <tr><td>Password</td><td>Optional</td><td>Password for the user, one will be generated randomly if not provided</td><td>Passwords must contain between 7 and 25 characters, at least 1 upper case and lower case letter and 1 number</td></tr>
        /// <tr><td>IsAdministrator</td><td>Required</td><td>Flag for whether or not the user should have administrator privileges</td><td></td></tr>
        /// <tr><td>ExecutionItemUid</td><td>Optional</td><td>Uid to track this item in the set of users in the request</td><td>Must be a valid Uid</td></tr>
        /// <tr><td>Fields</td><td>Optional</td><td>Set of field values for the user. If there are required fields, they must be provided here</td><td>Field values must be valid for their respective type</td></tr>
        /// </table>
        /// <br/>
        /// </remarks>        
        /// <param name="users">A list of users to add.</param>
        /// <param name="lookupFieldsPassedByValue">Optional query string parameter that allows you to pass list values numeric value instead of plain text value.  The default value for this is false.</param>
        [
            HttpPost,
            Route("users"),
            SwaggerRequestExample(typeof(UserApiInsertModel), typeof(UserPostExample)),
            SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource doesn't exist.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostUsers(List<UserApiInsertModel> users, bool lookupFieldsPassedByValue = false)
        {
            var prefix = "Membership.PostUsers => ";

            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, "Forbidden", $"Access denied"));

            if (users == null || users.Count == 0)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"Format of the request is not valid"));

            users.ForEach(u => u.IsNew = true);

            try
            {
                var execution = getApiExecution(users.Count);
                var results = await membershipRepository.UpsertUsers(execution, users, lookupFieldsPassedByValue, true, false);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Updates the specified users.
        /// </summary>
        /// <remarks>
        ///###Users###
        /// <table>
        /// <tr><td>**Field**</td><td>**Required / Optional**</td><td>**Description**</td><td>**Validation**</td></tr>
        /// <tr><td>uid</td><td>Required</td><td>The uid of the user record to update</td><td>Must be in a valid uid format</td></tr>
        /// <tr><td>Username</td><td>Required</td><td>The email the user will use to login</td><td>Must be in a valid email format</td></tr>
        /// <tr><td>Firstname</td><td>Required</td><td>First name of the user</td><td></td></tr>
        /// <tr><td>Lastname</td><td>Required</td><td>Last name of the user</td><td></td></tr>
        /// <tr><td>Password</td><td>Optional</td><td>Password for the user, one will be generated randomly if not provided</td><td>Passwords must contain between 7 and 25 characters, at least 1 upper case and lower case letter and 1 number</td></tr>
        /// <tr><td>IsAdministrator</td><td>Required</td><td>Flag for whether or not the user should have administrator privileges</td><td></td></tr>
        /// <tr><td>ExecutionItemUid</td><td>Optional</td><td>Uid to track this item in the set of users in the request</td><td>Must be a valid Uid</td></tr>
        /// <tr><td>State</td><td>Optional</td><td>State of the user record. If the State is not provided it will remain unchanged</td><td>Must be a valid State value. Valid values are Active, Inactive, and Deleted</td></tr>
        /// <tr><td>Fields</td><td>Optional</td><td>Set of field values for the user. If there are required fields, they must be provided here</td><td>Field values must be valid for their respective type</td></tr>
        /// </table>
        /// <br/>
        /// </remarks>        
        /// <param name="users">A list of users to update.</param>
        /// <param name="lookupFieldsPassedByValue">Optional query string parameter that allows you to pass list values numeric value instead of plain text value.  The default value for this is false.</param>
        /// <param name="IsChangePasswordReqeust">Optional query string parameter that allows you to password changed request.  The default value for this is false.</param>

        [
            HttpPut,
            Route("users"),
            SwaggerRequestExample(typeof(UserApiUpdateModel), typeof(UserPutExample)),
            SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource doesn't exist.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutUsers(List<UserApiUpdateModel> users, bool lookupFieldsPassedByValue = false, bool IsChangePasswordReqeust = false)
        {
            var prefix = "Membership.PutUsers => ";
            bool IsCurrentUser = false;

            if (!Company.CurrentResourceIsAdmin || IsChangePasswordReqeust)
            {
                if (users != null && users.Count == 1)
                {
                    foreach (var user in users)
                    {
                        var resource = Community.Filter<Resource>(i => i.Uid == user.uid, i => i.CompanyResources).SingleOrDefault();
                        if (resource != null)
                        {
                            if (resource.ID == Company.CurrentResourceID)
                            {
                                IsCurrentUser = true;
                            }
                        }
                    }
                }
            }

            //change password request Checks
            if (IsChangePasswordReqeust)
            {
                if (Community.CurrentCompanySsoModel.AuthenticationType != core.enums.AuthenticationType.Forms)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"IsChangePasswordReqeust set to true, Not allowed for authentication type other than Forms"));
                }
                if (!IsCurrentUser)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"IsChangePasswordReqeust set to true only for current user"));
                }
                if (users != null && users.Count > 1)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"Only one request accepted for IsChangePasswordReqeust set to true."));
                }
            }

            if (!Company.CurrentResourceIsAdmin && IsCurrentUser == false)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, "Forbidden", $"Access denied"));

            if (users == null || users.Count == 0)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"Format of the request is not valid"));

            users.ForEach(u => u.IsNew = false);

            try
            {
                var execution = getApiExecution(users.Count);
                var results = await membershipRepository.UpsertUsers(execution, users, lookupFieldsPassedByValue, false, IsChangePasswordReqeust);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Retrieves a list of favorite items for the current user
        /// </summary>
        /// <returns></returns>
        [
        HttpGet,
        Route("users/me/favorites"),
        SwaggerResponse(HttpStatusCode.OK, "", typeof(List<FavoriteApiModel>)),
        SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetFavorites()
        {
            var prefix = "Membership.GetFavorites => ";

            try
            {
                var results = await membershipRepository.GetFavorites(_company.CurrentResourceID);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Retrieves the Home Page the current user
        /// </summary>
        /// <returns></returns>
        [
        HttpGet,
        Route("users/me/getHomePage"),
        SwaggerResponse(HttpStatusCode.OK, "", typeof(bool)),
        SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
        ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetHomePage()
        {
            var prefix = "Membership.GetHomePage => ";

            try
            {
                var results = await membershipRepository.GetHomePage(_company.CurrentResourceID);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Clears the list of favorite items for the current user
        /// </summary>
        /// <returns></returns>
        [
        HttpDelete,
        Route("users/me/favorites"),
        SwaggerResponse(HttpStatusCode.OK, ""),
        SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> ClearFavorites()
        {
            var prefix = "Membership.ClearFavorites => ";

            try
            {
                var result = membershipRepository.DeleteFavorites(_company.CurrentResourceID);

                if (result.StatusCode != HttpStatusCode.OK)
                    return await Task.FromResult(errorMessageResponse(result.StatusCode, result.Error, result.Message));

                return await Task.FromResult(successMessageResponse(result.StatusCode, "Success", result.Message));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Given a route, toggles the favorite status on/off for the current user
        /// </summary>
        /// <returns></returns>
        [
            HttpPut,
            Route("users/me/favorites"),
            SwaggerRequestExample(typeof(FavoriteApiModel), typeof(FavoriteApiModelExample)),
            SwaggerResponse(HttpStatusCode.Created, "Favorite status toggled."),
            SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> ToggleFavorite(FavoriteApiModel favorite)
        {
            return await ToggleFavoriteOrHomepage(favorite, false);
        }

        /// <summary>
        /// Given a route, toggles the homepage status on/off for the current user
        /// </summary>
        /// <returns></returns>
        [
            HttpPut,
            Route("users/me/homepage"),
            SwaggerRequestExample(typeof(FavoriteApiModel), typeof(FavoriteApiModelExample)),
            SwaggerResponse(HttpStatusCode.Created, "Homepage status toggled."),
            SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> ToggleHomepage(FavoriteApiModel favorite)
        {
            var currentHome = Company.Filter<Favorite>(x => x.ResourceID == _company.CurrentResourceID && x.IsHomePage).FirstOrDefault();
            bool isNewHomePage = true;
            if (currentHome != null)
            {
                if (currentHome.Name == favorite.Name && currentHome.Type == favorite.Type.ToString() && favorite.Route == currentHome.Route)
                    isNewHomePage = false;
            }
            return await ToggleFavoriteOrHomepage(favorite, isNewHomePage);
        }

        private async Task<IHttpActionResult> ToggleFavoriteOrHomepage(FavoriteApiModel favorite, bool isHomepage = false)
        {
            var prefix = "Membership.ToggleFavoriteOrHomepage => ";

            try
            {
                if (string.IsNullOrWhiteSpace(favorite.Name))
                {
                    string message = "Name is required.";
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Name.", message));
                }
                else
                {
                    favorite.Name = favorite.Name.Trim();
                }
                if (favorite.Type == FavoriteType.Page && string.IsNullOrWhiteSpace(favorite.Route))
                {
                    string message = "Favorites of type Page cannot have an empty route.";
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Type and Route.", message));
                }
                else
                {
                    favorite.Route = favorite.Route.Trim();
                }
                bool result = await membershipRepository.ToggleFavorite(_company.CurrentResourceID, favorite, isHomepage);
                if (result)
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.Created)));
                else
                {
                    string message = "Uid Invalid for " + favorite.Type.ToString();
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Uid.", message));
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error.", errorMessage));
            }
        }

        /// <summary>
        /// Deletes a group based on the specified group uid.
        /// </summary>
        /// <param name="groups">The group(s) that need to be deleted</param>
        [
            HttpDelete,
            Route("groups"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(DeleteGroupModel), typeof(DeleteGroupExample)),
            SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))

        ]
        public async Task<IHttpActionResult> DeleteGroup(List<DeleteGroupModel> groups)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            if (groups.Count() < 1)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No Groups provided in request"));

            var execution = getApiExecution(groups.Count);

            var result = membershipRepository.DeleteGroups(execution, groups);

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result)));
        }

        private bool IsValidGuid(IEnumerable<KeyValuePair<string, string>> queryParams, string paramName)
        {
            bool isValid = true;
            if (queryParams.ToList().Any(q => q.Key.ToLower() == paramName.ToLower()))
            {
                Guid uid;
                var uidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == paramName.ToLower()).Value;
                if (Guid.TryParse(uidString, out uid))
                    isValid = true;
                else
                    isValid = false;

            }
            return isValid;
        }

        /// <summary>
        /// Updates a group based on the specified group uid.
        /// </summary>
        /// <param name="groups">The groups that need to be updated</param>
        [
            HttpPut,
            Route("groups"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(UpdateGroup), typeof(UpdateGroupExample)),
            SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "There are no groups in this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))

        ]
        public async Task<IHttpActionResult> UpdateGroup(List<UpdateGroupModel> groups)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            if (groups.Count < 1)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "There are no groups in this request."));

            var isValid = groups.All(x => x.Uid.HasValue);

            if (!isValid)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Uid must be provided in all requests"));

            var execution = getApiExecution(groups.Count);

            var result = membershipRepository.UpdateGroups(execution, groups);

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result)));
        }

        /// <summary>
        /// Add a group based on the data provided in request.
        /// </summary>
        /// <param name="groups">The groups that will be added</param>
        [
            HttpPost,
            Route("groups"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(AddGroup), typeof(AddGroupExample)),
            SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "There are no groups in this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))

        ]
        public async Task<IHttpActionResult> AddGroup(List<AddGroupModel> groups)
        {
            List<UpdateGroupModel> models = new List<UpdateGroupModel>();

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            if (groups.Count < 1)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "There are no groups in this request."));

            foreach (var i in groups)
            {
                if (i.Name == null)
                {
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Name is missing in one or more of the groups in the payload. Name must be provided."));
                }

                models.Add(new UpdateGroupModel
                {
                    Description = i.Description,
                    Name = i.Name,
                    PrimaryOwnerUid = i.PrimaryOwnerUid,
                    SecondaryOwnerUid = i.SecondaryOwnerUid,
                    IsActiveDirectoryGroup = i.IsActiveDirectoryGroup
                });
            }

            var execution = getApiExecution(groups.Count);

            var result = membershipRepository.AddGroups(execution, models);

            Company.CreateOrUpdateTypeDisplayValuesAsync(1, core.SystemObjects.GroupType.ToString());

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result)));
        }


        /// <summary>
        /// Retrive a summary of organizations for a given organization type
        /// </summary>
        /// <param name="organizationTypeUid">The uid of the organization type</param>
        [
     HttpGet,
     MapToApiVersion("2.0"),
     Route("organizations/{organizationTypeUid:Guid}"),
     SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
     SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
     SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
     SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by AssetId.", DataType = "string", ParameterType = "query", Required = false),
     SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
     SwaggerParameter("_filter", "The filter expression used to filter organisations by name and accepted users email. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
     SwaggerResponse(HttpStatusCode.OK, "Gets a list of Organizations.", typeof(List<OrganizationModel>)),
     SwaggerResponse(HttpStatusCode.BadRequest, "Invalid Parameters provided"),
     SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied: User is not an administrator"),
     SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
 ]
        public async Task<IHttpActionResult> GetOrganizationsByType(Guid organizationTypeUid)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            if (!Company.Any<AssetType>(x => x.uid == organizationTypeUid && x.Object == core.SystemObjects.OrganizationType.ToString()))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid organizationTypeUid provided"));
            }

            var queryParams = Request.GetQueryNameValuePairs();

            string isValid = isPageSizeAndNumValid(queryParams);

            if (string.IsNullOrEmpty(isValid) && queryParams.Any(q => q.Key == "_order"))
            {
                string[] allowedValues = new string[] { "name", "acceptedbyusername", "acceptedon", "administratoremail" };
                var order = queryParams.ToList().FirstOrDefault(q => q.Key == "_order").Value.ToLower();
                if (!allowedValues.Contains(order))
                {
                    isValid = $"{order} is not a valid _order field";
                }
            }

            if (string.IsNullOrEmpty(isValid) && queryParams.Any(q => q.Key == "_direction"))
            {
                string[] allowedValues = new string[] { "asc", "desc" };
                var directionFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction");

                if (!allowedValues.Contains(directionFilter.Value.Trim().ToLower()))
                {
                    isValid = "Invalid _direction provided";
                }
            }

            if (!string.IsNullOrEmpty(isValid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", isValid));
            }
            try
            {
                List<OrganizationModel> organizations = await membershipRepository.GetOrganizationsByType(organizationTypeUid, queryParams);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, organizations)));
            }
            catch (FilterExpressionParserException ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Filter expression parse error", errorMessage));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }


        /// <summary>
        /// Gets details about a single organization.
        /// </summary>
        /// <param name="organizationUid">Uid of the organization</param>
        [
             HttpGet,
             MapToApiVersion("2.0"),
             Route("organization/{organizationUid:Guid}"),
             SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
             SwaggerResponse(HttpStatusCode.OK, "Gets details about a single organization.", typeof(List<OrganizationDetailModel>)),
             SwaggerResponse(HttpStatusCode.BadRequest, "Invalid Organization Uid"),
             SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied: User is not an administrator"),
             SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetOrganizationsDetails(Guid organizationUid)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {
                OrganizationDetailModel organizationDetails = await membershipRepository.GetOrganizationsDetails(organizationUid);
                if (organizationDetails == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Organization Uid", "Invalid Organization Uid provided"));
                }
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, organizationDetails)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        private byte[] GetUsersExcelFromResults(IEnumerable<dynamic> results, List<FieldType> fieldTypes)
        {
            List<Tuple<string, string, string>> fieldMap = new List<Tuple<string, string, string>>();
            fieldMap.Add(new Tuple<string, string, string>("First name", "FirstName", "Text"));
            fieldMap.Add(new Tuple<string, string, string>("Last name", "LastName", "Text"));
            fieldMap.Add(new Tuple<string, string, string>("Email", "Email", "Text"));
            fieldTypes.Where(x => x.IsListable == true).ToList().ForEach(ft =>
             {
                 fieldMap.Add(new Tuple<string, string, string>(ft.FriendlyName, ft.Name, ft.Type));
             });
            fieldMap.Add(new Tuple<string, string, string>("Created on", "CreatedOn", "Date"));
            fieldMap.Add(new Tuple<string, string, string>("Last logged in on", "LastLoggedInOn", "Date"));
            fieldMap.Add(new Tuple<string, string, string>("Administrator?", "IsAdministrator", "Boolean"));
            fieldMap.Add(new Tuple<string, string, string>("Status", "State", "Text"));
            fieldMap.Add(new Tuple<string, string, string>("User UID", "uid", "Text"));


            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Users");

            int colIndex = 1;
            int rowIndex = 1;
            foreach (var f in fieldMap)
            {
                document.SetCellValue(rowIndex, colIndex, f.Item1);
                colIndex++;
            }

            foreach (var row in results)
            {
                rowIndex++;
                colIndex = 1;

                foreach (var f in fieldMap)
                {
                    var val = (((row as IDictionary<string, object>)[$"{f.Item2}"]) ?? "").ToString();
                    SetCellValue(document, rowIndex, colIndex, f.Item3, val);
                    colIndex++;
                }
            }

            var stream = new MemoryStream();
            document.SaveAs(stream);
            var result = stream.ToArray();
            return result;
        }

        /// <summary>
        /// Retrieve the current users API credentials
        /// </summary>
        /// <returns></returns>
        [
        HttpGet,
        Route("users/me/apikey"),
        SwaggerResponse(HttpStatusCode.OK, "", typeof(List<ApiKeyDetailModel>)),
        SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
        SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetApikey()
        {
            var prefix = "Membership.GetApikey => ";

            var showAllUsersAPIKey = false;
            var settings = Community.GetCompanySettings();
            if (settings.Any(i => i.Key == "ShowAllUsersAPIKey"))
            {
                showAllUsersAPIKey = bool.Parse(settings["ShowAllUsersAPIKey"]);
            }


            if (!Company.CurrentResourceIsAdmin && !showAllUsersAPIKey)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, "Forbidden", $"Access denied"));
            }

            try
            {
                var resource = Community.GetById<Resource>(_company.CurrentResourceID);

                if (resource is null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid user information", "Invalid user information"));
                };

                var apikeydetail = new ApiKeyDetailModel
                {
                    apikey = resource.APIPublicKey, //publickey
                    apiSecret = resource.APIPrivateKey //privatekey
                };

                if (apikeydetail.apikey == null || apikeydetail.apiSecret == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid user information", "Invalid user information"));
                }
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, apikeydetail)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Retrieve the roles of current user (Administrator/User)
        /// </summary>
        /// <returns></returns>
        [
        HttpGet,
        Route("users/me/roles"),
        SwaggerResponse(HttpStatusCode.OK, "", typeof(List<string>)),
        SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
        SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IHttpActionResult> GetUserRoles()
        {
            var prefix = "Membership.GetUserRoles => ";

            try
            {
                List<string> roles = new List<string>();

                if (Company.CurrentResourceIsAdmin)
                {
                    roles.Add("Administrator");
                }
                else
                {
                    roles.Add("User");
                }

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, roles)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Updates the watch status of an Asset/Asset Type for the requesting user.
        /// </summary>
        /// <param name="model">Request model containing the Asset/Asset Type to be watched/unwatched</param>
        [
            HttpPut,
            Route("users/me/watches"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),            
            SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid request model parameters provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> UpdateWatches(UpdateUserWatchModel model)
        {
            int id = -1;
            string type = "";
            string name = "";
            string parentName = "";
            bool includeChildren = false;            

            if (model.assetTypeUid == null && model.assetUid == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Either assetTypeUid or assetUid must be provided"));
            }

            if (model.assetTypeUid != null && model.assetUid != null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Only one of assetTypeUid or assetUid should be provided"));
            }

            if (model.assetTypeUid != null)
            {
                if((model.assetTypeUid.Value == Guid.Empty) || !Company.Any<AssetType>(x => x.uid == model.assetTypeUid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid assetTypeUid provided"));
                }
                else
                {
                    var assetType = assetRepository.GetAssetTypeByUID(model.assetTypeUid.Value);
                    id = assetType.ObjectID;
                    type = assetType.Object;
                    name = assetType.Name;
                    includeChildren = true;
                }                
            }
           
            if (model.assetUid != null)
            {
                
                if((model.assetUid.Value == Guid.Empty) || !Company.Any<Asset>(x => x.uid == model.assetUid.Value))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid assetUid provided"));
                }
                else
                {
                    var asset = Company.Filter<AssetDetail>(x => x.uid == model.assetUid.Value).FirstOrDefault();
                    id = asset.ObjectID;
                    type = asset.Object;
                    name = asset.DisplayValue;
                    parentName = asset.TypeName;
                }               
            }

            var followDetail = Company.Filter<FollowDetail>(i => i.ObjectID == id && i.ObjectType == type && i.ResourceID == Company.CurrentResourceID).FirstOrDefault();

            if(model.watches && followDetail != null)
            {

                if (followDetail.HardFollow)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", string.Format("You are already watching {0}.", (model.assetTypeUid != null) ? $"type '{name}'" : $"'{name}'")));
                }
                else
                {
                    if (followDetail != null && !followDetail.HardFollow)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You are currently watching '{name}' via it's parent, '{parentName}'."));
                    }
                }                
            }

            if(!model.watches)
            {
                if(followDetail == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", string.Format("You are not currently watching {0}.", (model.assetTypeUid != null) ? $"type '{name}'" : $"'{name}'")));
                }
                if(followDetail != null && !followDetail.HardFollow)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You are currently watching this item's parent, '{parentName}'.  You can not unwatch this item individually."));
                }
            }

            bool success = Company.UpdateFollowStatus((SystemObjects)Enum.Parse(typeof(SystemObjects), type), id, null, includeChildren);            

            return await Task.FromResult<IHttpActionResult>(successMessageResponse(HttpStatusCode.OK, "Success", string.Format("You are {0} watching {1}.", (success) ? "now" : "no longer", (model.assetTypeUid != null) ? $"type '{name}'" : $"'{name}'"))); ;
        }



        /// <summary>
        /// Checks the watch status of an Asset for the requesting user.
        /// </summary>
        /// <param name="assetTypeUid">Uid of the asset type</param>
        /// <param name="assetUid">Uid of the asset</param>
        [
            HttpGet,
            Route("api/v2/membership/users/me/watches/{assetTypeUid:Guid}/{assetUid:Guid}"),            
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid parameters provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetWatchStatusOfAsset(Guid assetTypeUid, Guid? assetUid)
        {            
            return await Task.FromResult(GetWatchStatusForUser(assetTypeUid, assetUid));
        }

        /// <summary>
        /// Checks the watch status of an Asset Type for the requesting user.
        /// </summary>
        /// <param name="assetTypeUid">Uid of the asset type</param>
        [
            HttpGet,
            Route("api/v2/membership/users/me/watches/{assetTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid parameters provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetWatchStatusOfAssetType(Guid assetTypeUid)
        {
            return await Task.FromResult(GetWatchStatusForUser(assetTypeUid, null));
        }

        private IHttpActionResult GetWatchStatusForUser(Guid assetTypeUid, Guid? assetUid)
        {
            bool response = false;

            if ((assetTypeUid == Guid.Empty) || !Company.Any<AssetType>(x => x.uid == assetTypeUid))
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid assetTypeUid provided.");
            }

            var assetType = assetRepository.GetAssetTypeByUID(assetTypeUid);

            if (assetUid != null)
            {

                if ((assetUid.Value == Guid.Empty) || !Company.Any<Asset>(x => x.uid == assetUid.Value))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid assetUid provided.");
                }
                else
                {
                    var asset = Company.Filter<AssetDetail>(x => x.uid == assetUid.Value).FirstOrDefault();

                    if (asset.AssetTypeUid != assetTypeUid)
                    {
                        return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid assetUid does not match the asset type provided.");
                    }

                    response = Company.Any<Follow>(F => F.ObjectID == asset.ObjectID && F.ObjectType == asset.Object && F.ResourceID == Company.CurrentResourceID);
                }

            }
            
            if(!response)
            {
                response = Company.Any<Follow>(F => F.ObjectID == assetType.ObjectID && F.ObjectType == assetType.Object && F.ResourceID == Company.CurrentResourceID);
            }

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response));
        }
    }
}
