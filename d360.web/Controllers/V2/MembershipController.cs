using d360.core.entities;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Dapper;
using Microsoft.Web.Http;
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
        Authorize
    ]
    public class MembershipController : BaseV2ApiController
    {
        public MembershipController(ICommunityContext community, ICompanyContext company)
            : base(community, company)
        {
        }
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("users"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of FollowingBreakdown.", typeof(ResourceApiViewModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerParameter("_uid", "The uid of the user.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_firstName", "First Name of user.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_lastName", "Last Name of user.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_state", "What state is the user.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_isAdministrator", "Is the user an adminstrator or not.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default is 5 users per page and max value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
        ]
        public async Task<HttpResponseMessage> GetUsers()
        {
            string sql = "select uid, ResourceID, FirstName, LastName, Email, IsAdministrator, LastLoggedInOn, State from [reporting].[Global_Resource] ";
            string countSql = "select count(*) from [reporting].[Global_Resource] ";
            var uid = "";
            var firstName = "";
            var lastName = "";
            var state = -1;
            bool isAdministrator = false;
            bool is_AdminParam = false;
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
                        case "_uid":
                            uid = q.Value;
                            dbArgs.Add("uid", q.Value);
                            queries.Add(" uid = @uid");
                            break;
                        case "_firstname":
                            firstName = q.Value;
                            dbArgs.Add("FirstName", q.Value);
                            queries.Add(" FirstName = @FirstName");
                            break;
                        case "_lastname":
                            lastName = q.Value;
                            dbArgs.Add("LastName", q.Value);
                            queries.Add(" LastName = @LastName");
                            break;
                        case "_state":
                            int.TryParse(q.Value, out state);
                            dbArgs.Add("state", q.Value);
                            queries.Add(" state = @state");
                            break;
                        case "_isadministrator":
                            is_AdminParam = true;
                            bool.TryParse(q.Value, out isAdministrator);
                            dbArgs.Add("isAdministrator", isAdministrator);
                            queries.Add(" isAdministrator = @isAdministrator");
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
            if (uid != "" || firstName != "" || lastName != "" || state != -1 || is_AdminParam)
            {
                sql += "where ";
                countSql += "where ";
            }
            for (int i = 0; i < queries.Count(); i++)
            {
                sql += queries[i].ToString();
                countSql += queries[i].ToString();
                if (i < queries.Count() - 1)
                {
                    sql += " and ";
                    countSql += " and ";
                }
            }
            if (pageSize > 0 || pageNum > 0)
            {
                if (pageSize < 1) pageSize = 1;
                if (pageNum < 1) pageNum = 1;
                model.pageNum = pageNum;
                model.pageSize = pageSize;
                string offsetSql = $" Order by ResourceID offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
                sql += offsetSql;
            }
            var results = await Company.QueryAsync<dynamic>(sql, dbArgs);
            var count = await Company.QueryAsync<int>(countSql, dbArgs);
            #region GetDynamicFields
            foreach (IDictionary<string, object> item in results)
            {
                int resourceId = 0;
                if (int.TryParse(item["ResourceID"].ToString(), out resourceId))
                {
                    IQueryable<FieldWithRelation> list = Company.GetFieldRelationsByObject(core.SystemObjects.Resource, resourceId);
                    list.ToList().ForEach(y => { item.Add(y.Name, y.FormattedValue); });
                }
            }
            #endregion
            model.items = results;
            model.total = count.FirstOrDefault();
            return Request.CreateResponse(HttpStatusCode.OK, model);
        }
        [
           HttpGet,
           MapToApiVersion("2.0"),
           Route("groups/{groupUid:Guid}/members"),
           SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
           SwaggerResponse(HttpStatusCode.OK, "A list of FollowingBreakdown.", typeof(ResourceApiViewModel)),
           SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
           SwaggerParameter("_firstName", "The First Name of the user.", DataType = "string", ParameterType = "query", Required = false),
           SwaggerParameter("_lastName", "The last name of the user.", DataType = "string", ParameterType = "query", Required = false),
           SwaggerParameter("_pageSize", "The number of results to return per page. The default is 5 users per page and max value is 250.", DataType = "integer", ParameterType = "query", Required = false),
           SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
       ]
        public async Task<HttpResponseMessage> GetMembers(Guid groupUid)
        {
            string sql = @"
                           select  gr.FirstName,
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