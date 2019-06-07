using d360.core.entities;
using d360.model;
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
        public MembershipController(ICommunityContext community, ICompanyContext company)
            : base(community, company)
        {
            _company = company;
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
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("users"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Gets a list of Users.", typeof(ResourceApiViewModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
        ]
        public async Task<HttpResponseMessage> GetUsers(Guid? Uid = null, string FirstName = null, string LastName = null, core.enums.State? State = null, bool? IsAdministrator = null, int _pageSize = 5, int _pageNum = 1)
        {
            string finalSql = "";
            string joinsSql = " left join Asset A on A.Object = 'Resource' and A.ObjectID = gr.ResourceID ";
            string whereSql = "";
            string selectSql = $"select gr.uid, ResourceID, FirstName, LastName, Email, IsAdministrator, LastLoggedInOn, gr.State";
            string countSql = "select count(*) from [reporting].[Global_Resource] gr ";

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
                    queries.Add(" uid = @uid");
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
            finalSql = selectSql + " from[reporting].[Global_Resource] gr " + joinsSql + " " + whereSql;
            countSql += joinsSql + " " + whereSql;
            {
                if (_pageSize < 1) _pageSize = 1;
                if (_pageNum < 1) _pageNum = 1;
                model.pageNum = _pageNum;
                model.pageSize = _pageSize;
                string offsetSql = $" Order by ResourceID offset {_pageSize * (_pageNum - 1)} rows fetch next {_pageSize} rows only";
                finalSql += offsetSql;
            }
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
            string sql = @"
                           select  gr.uid,
                                   gr.FirstName,
                                   gr.LastName ,
                                   gr.Email,
                                   gr.IsAdministrator,
                                   gr.LastLoggedInOn,
                                   gr.State
                                   from[reporting].[Global_Resource] as gr
                                       inner join [dbo].[ResourceGroup] rg on rg.ResourceID = gr.ResourceID
                                       inner join [dbo].[Group] g on g.ID = rg.GroupID
									   inner join [dbo].[Asset] a on a.uid = '"
                                    + groupUid + "' where g.ID = a.ObjectID";
            string countSql = @"
                           select count(*)
                                   from[reporting].[Global_Resource] as gr
                                       inner join [dbo].[ResourceGroup] rg on rg.ResourceID = gr.ResourceID
                                       inner join [dbo].[Group] g on g.ID = rg.GroupID
									   inner join [dbo].[Asset] a on a.uid = '"
                                    + groupUid + "' where g.ID = a.ObjectID";
            var firstName = "";
            var lastName = "";
            var pageSize = 5;
            var pageNum = 1;
            DynamicParameters dbArgs = new DynamicParameters();
            List<string> queries = new List<string>();
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
                            sql += " and gr.FirstName = @firstName";
                            countSql += " and gr.FirstName = @firstName";
                            break;
                        case "_lastname":
                            lastName = q.Value;
                            dbArgs.Add("lastName", q.Value);
                            sql += " and gr.lastName = @lastName";
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

            if (pageSize > 0 || pageNum > 0)
            {
                if (pageSize < 1) pageSize = 1;
                if (pageNum < 1) pageNum = 1;
                model.pageNum = pageNum;
                model.pageSize = pageSize;
                string offsetSql = $" Order by gr.ResourceID offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
                sql += offsetSql;
            }
            var results = await Company.QueryAsync<dynamic>(sql, dbArgs);
            var count = await Company.QueryAsync<int>(countSql, dbArgs);
            model.items = results;
            model.total = count.FirstOrDefault();
            return Request.CreateResponse(HttpStatusCode.OK, model);
        }
    }
}