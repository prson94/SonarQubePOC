using d360.core.entities;
using d360.core.entities.Membership;
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
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("users"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.OK, "Gets a list of Users.", typeof(ResourceApiViewModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid PageSize/PageNum value provided. Number is too large"),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetUsers(Guid? Uid = null, string FirstName = null, string LastName = null, core.enums.CompanyResourceState? State = null, bool? IsAdministrator = null, string _pageSize = "5", string _pageNum = "1", string _order = "ResourceID", string _direction = "asc", string _filter = "", string _simpleFilter = "")
        {
            try
            {

                var settings = Community.GetCompanySettings();
                if (!Company.CurrentResourceIsAdmin && (settings["ShowResources"] ?? "").ToUpper() != "TRUE")
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, "Forbidden", $"Access denied"));

                string finalSql = "";
                string joinsSql = " left join Asset A on A.Object = 'Resource' and A.ObjectID = gr.ResourceID ";
                string whereSql = "";
                string selectSql = @"select gr.uid, ResourceID, FirstName, LastName, Email, IsAdministrator, LastLoggedInOn, 
                    case gr.State 
                     when 1 then 'Active'
                     when 2 then 'InActive'
                     when 3 then 'Deleted' end as State ";
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

                if (Uid != null || FirstName != null || LastName != null || State != null || IsAdministrator != null)
                {
                    if (Uid != null)
                    {
                        dbArgs.Add("uid", Uid);
                        queries.Add(" gr.uid = @uid");
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
                    joinsSql += join;
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

                    foreach (var field in fieldTypes.Where(x => x.IsListable == true))
                    {
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
                    }
                    List<string> defaultFields = new List<string> { "FirstName", "LastName", "Email", "IsAdministrator", "LastLoggedInOn" };

                    defaultFields.ForEach(f =>
                    {
                        simpleFilters.Add($"{f} like @simpleFilter");
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
                List<string> validCols = new List<string> { "uid", "ResourceID", "FirstName", "LastName", "Email", "IsAdministrator", "LastLoggedInOn", "State" };
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
   SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
]
        public async Task<HttpResponseMessage> AddMembers(Guid groupUid, List<InsertUserToGroup> users)
        {
            var kvpGroupUid = new Dictionary<string, string> { { "Uid", groupUid.ToString() } };
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
           SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
           SwaggerParameter("_firstName", "The First Name of the user.", DataType = "string", ParameterType = "query", Required = false),
           SwaggerParameter("_lastName", "The last name of the user.", DataType = "string", ParameterType = "query", Required = false),
           SwaggerParameter("_pageSize", "The number of results to return per page. The default is 5 users per page and max value is 250.", DataType = "integer", ParameterType = "query", Required = false),
           SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
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
    SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
    SwaggerParameter("Uid", "Uid of the group.", DataType = "string", ParameterType = "query", Required = false),
    SwaggerParameter("Name", "Name of the group", DataType = "string", ParameterType = "query", Required = false)

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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))

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
            SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource doesn't exist.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteUsers(List<string> users)
        {
            var prefix = "Membership.DeleteUsers => ";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Unauthorized", $"Access denied"));


                List<UserApiDeleteModel> resources = new List<UserApiDeleteModel>();

                foreach (var u in users)
                {
                    if (Guid.TryParse(u, out Guid res))
                    {
                        resources.Add(new UserApiDeleteModel()
                        {
                            Uid = res
                        });
                    }
                    else
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"The value [{u}] is not a valid uid."));
                    }
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
        [
            HttpPost,
            Route("users"),
            SwaggerRequestExample(typeof(UserApiInsertModel), typeof(UserPostExample)),
            SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource doesn't exist.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostUsers(List<UserApiInsertModel> users)
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
                var results = await membershipRepository.UpsertUsers(execution, users);
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
        [
            HttpPut,
            Route("users"),
            SwaggerRequestExample(typeof(UserApiUpdateModel), typeof(UserPutExample)),
            SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource doesn't exist.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutUsers(List<UserApiUpdateModel> users)
        {
            var prefix = "Membership.PutUsers => ";

            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, "Forbidden", $"Access denied"));

            if (users == null || users.Count == 0)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"Format of the request is not valid"));

            users.ForEach(u => u.IsNew = false);

            try
            {
                var execution = getApiExecution(users.Count);
                var results = await membershipRepository.UpsertUsers(execution, users);
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
        SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
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
        SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
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
                } else
                {
                    favorite.Name = favorite.Name.Trim();
                }
                if (favorite.Type == FavoriteType.Page && string.IsNullOrWhiteSpace(favorite.Route))
                {
                    string message = "Favorites of type Page cannot have an empty route.";
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Type and Route.", message));
                } else
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))

        ]
        public async Task<IHttpActionResult> DeleteGroup(List<DeleteGroupModel> groups)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            if(groups.Count() < 1)
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))

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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))

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
    }
}
