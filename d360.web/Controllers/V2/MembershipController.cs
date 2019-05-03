using d360.core.entities;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

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
            SwaggerResponse(HttpStatusCode.OK, "A list of FollowingBreakdown.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerParameter("_uid", "The number of re", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_firstName", "The page number to return results for.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_lastName", "The page number to return results for.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_state", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_isAdministrator", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default is 5 users per page and max value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
        ]
        public async Task<HttpResponseMessage> GetUsers()
        {


            string sql = "select uid, ResourceID, FirstName, LastName, Email, IsAdministrator, LastLoggedInOn, State from [reporting].[Global_Resource] ";
            var uid = "";
            var firstName = "";
            var lastName = "";
            var state = -1;
            var isAdministrator = -1;
            var pageSize = 5;
            var pageNum = 1;
            List<string> queries = new List<string>();

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
                            break;
                        case "_firstname":
                            firstName = q.Value;
                            break;
                        case "_lastname":
                            lastName = q.Value;
                            break;
                        case "_state":
                            int.TryParse(q.Value, out state);
                            break;
                        case "_isadministrator":
                            int.TryParse(q.Value, out isAdministrator);
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

            if (uid != "" || firstName != "" || lastName != "" || state != -1 || isAdministrator != -1)
            {
                sql += "where ";
            }



            if (uid != "")
            {
                queries.Add(" uid = '" + uid + "'");
            }
            if (firstName != "")
            {
                queries.Add(" FirstName = '" + firstName + "'");
            }
            if (lastName != "")
            {
                queries.Add(" LastName = '" + lastName + "'");
            }
            if (state != -1)
            {
                queries.Add(" state = " + state);
            }
            if (isAdministrator != -1)
            {
                queries.Add(" isAdministrator = " + isAdministrator);
            }

            for (int i = 0; i < queries.Count(); i++)
            {
                sql += queries[i].ToString();
                if (i < queries.Count() - 1)
                {
                    sql += " and ";
                }
            }

            if (pageSize > 0 || pageNum > 0)
            {
                if (pageSize < 1) pageSize = 1;
                if (pageNum < 1) pageNum = 1;

                string offsetSql = $" Order by ResourceID offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
                sql += offsetSql;
            }

            var results = await Company.QueryAsync<dynamic>(sql);

            #region GetDynamicFields

            //as individual items in the response
            foreach (IDictionary<string, object> item in results)
            {
                int resourceId = 0;
                if (int.TryParse(item["ResourceID"].ToString(), out resourceId))
                {
                    IQueryable<FieldWithRelation> list = Company.GetFieldRelationsByObject(core.SystemObjects.Resource, resourceId);
                    list.ToList().ForEach(y => { item.Add(y.Name, y.FormattedValue); });
                }
            }
            //as an single array of items in the response
            results.ToList().ForEach(x =>
            {
                if (x.ResourceID > 0)
                {
                    IQueryable<FieldWithRelation> list = Company.GetFieldRelationsByObject(core.SystemObjects.Resource, x.ResourceID);
                    x.fieldsAsArray = list.Select(y => new { y.Name, y.FormattedValue });
                }
            });
            #endregion

            return Request.CreateResponse(HttpStatusCode.OK, results);
        }
    }
}