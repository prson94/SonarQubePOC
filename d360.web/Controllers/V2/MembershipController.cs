using d360.core.entities;
using d360.core.entities.Membership;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Dapper;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
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
        public MembershipController(ICommunityContext community, ICompanyContext company, IMembershipRepository membershipRepository)
            : base(community, company)
        {
            _company = company;
            this.membershipRepository = membershipRepository;
            
        }
        /// <summary>
        /// Retrieves a list of users.
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
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("users"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Gets a list of Users.", typeof(ResourceApiViewModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
        ]
        public async Task<HttpResponseMessage> GetUsers(Guid? Uid = null, string FirstName = null, string LastName = null, core.enums.CompanyResourceState? State = null, bool? IsAdministrator = null, int _pageSize = 5, int _pageNum = 1, string _order = "ResourceID", string _direction = "asc")
        {
            string finalSql = "";
            string joinsSql = " left join Asset A on A.Object = 'Resource' and A.ObjectID = gr.ResourceID ";
            string whereSql = "";
            string selectSql = $"select gr.uid, ResourceID, FirstName, LastName, Email, IsAdministrator, LastLoggedInOn, case gr.State " +
                $" when 1 then 'Active'" +
                $"when 2 then 'InActive'" +
                $"when 3 then 'Deleted' end as State ";
            string countSql = "select count(*) from [reporting].[Global_Resource] gr ";
            string orderBySQL = $"";

            DynamicParameters dbArgs = new DynamicParameters();
            List<string> queries = new List<string>();
            ResourceApiViewModel model = new ResourceApiViewModel();
            List<string> fieldColumns = new List<string>();
            List<string> fieldJoins = new List<string>();

            if (_pageNum < 1) _pageNum = 1;

            if (_pageSize < 1) _pageSize = 1;
            if (_pageSize > 250) _pageSize = 250;

            var fieldTypes = _company.FieldTypes.Where(f => f.Object == "ResourceType" && f.ObjectID == 1).ToList();

            IDictionary<string, string> customFields = new Dictionary<string, string>();
            var queryParams = Request.GetQueryNameValuePairs();
            getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);

            if (_pageSize > 0 || _pageNum > 0)
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
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid order by passed in the request");
            if (!new string[] { "asc", "desc" }.Contains(_direction.ToLower())) 
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid order passed in the request");

            orderBySQL = $"order by {_order} {_direction}";

            finalSql = selectSql + " from[reporting].[Global_Resource] gr " + joinsSql + " " + whereSql;
            countSql += joinsSql + " " + whereSql;

            if (_pageSize < 1) _pageSize = 1;
            if (_pageNum < 1) _pageNum = 1;
            model.pageNum = _pageNum;
            model.pageSize = _pageSize;
            string offsetSql = $" {orderBySQL} offset {_pageSize * (_pageNum - 1)} rows fetch next {_pageSize} rows only";
            finalSql += offsetSql;

            var results = await Company.QueryAsync<dynamic>(finalSql, dbArgs);
            var countResults = await Company.QueryAsync<int>(countSql, dbArgs);
            model.items = results;
            model.total = countResults.FirstOrDefault();
            return Request.CreateResponse(HttpStatusCode.OK, model);
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
           SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
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
            var pageSize = 5;
            var pageNum = 1;
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
                            if (int.TryParse(q.Value, out pageSize))
                            {
                                if (pageSize < 1) pageSize = 1;
                            }
                            if (pageSize > 250) pageSize = 250; // max page size is 250 people.
                            break;
                        case "_pagenum":
                            if (int.TryParse(q.Value, out pageNum))
                            {
                                if (pageNum < 1) pageNum = 1;
                            }
                            break;
                    }
                }
            });

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

            if (pageSize > 0 || pageNum > 0)
            {
                if (pageSize < 1) pageSize = 1;
                if (pageNum < 1) pageNum = 1;
                model.pageNum = pageNum;
                model.pageSize = pageSize;
                string offsetSql = $" Order by gr.ResourceID offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
                finalSql += offsetSql;
            }
            var results = await Company.QueryAsync<dynamic>(finalSql, dbArgs);
            var count = await Company.QueryAsync<int>(countSql, dbArgs);
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

            var results = await Company.QueryAsync<dynamic>(sql);
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
    SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
    SwaggerParameter("Uid", "Uid of the group.", DataType = "string", ParameterType = "query", Required = false),
    SwaggerParameter("Name", "Name of the group", DataType = "string", ParameterType = "query", Required = false)

]
        public async Task<IHttpActionResult> GetGroups()
        {
            var prefix = "Membership.GetGroups => ";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

                if  (!this.IsValidGuid(queryParams,"uid")){
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
            SwaggerResponse(HttpStatusCode.OK, "Success"),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource / Group doesn't exist."),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Access denied / you are not an admin and dont have access to perform this operation.")

        ]
        public async Task<HttpResponseMessage> DeleteGroupMember(Guid groupUid,Guid resourceUid)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Access Denied"));

            var res = await Company.Database.Connection.ExecuteAsync("delete rg from [dbo].[ResourceGroup] rg inner join[reporting].[Global_Resource] gr on gr.uid = @resource inner join[dbo].[Asset] a on a.uid = @group and a.object = 'Group' inner join[dbo].[Group] g on g.ID = a.ObjectID where rg.ResourceID = gr.ResourceID and rg.GroupID = g.ID", new { resource = resourceUid, group = groupUid });

            if (res > 0) return Request.CreateResponse(HttpStatusCode.OK); // deleted

            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Not found - Resource / Group doesn't exist"));
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

    }
}