using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
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
    /// <summary>
    /// This service houses all endpoints handling actions management in Govern.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/actions"),
        Authorize
    ]
    public class ActionsController : BaseV2ApiController
    {
        IIssueRepository issueRepository;
        IAssetRepository assetRepository;

        public ActionsController(ICommunityContext community, ICompanyContext company, IIssueRepository repository, IAssetRepository assetRepository)
            : base(community, company)
        {
            this.issueRepository = repository;
            this.assetRepository = assetRepository;
        }

        /// <summary>
        /// Returns all actions.
        /// </summary>
        /// <param name="actionTypeUid">The unique identifier of an action type</param>
        /// <param name="assetUid">The unique identifier of an asset</param>
        [
           HttpGet,
           MapToApiVersion("2.0"),
           Route(""),
           SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
           SwaggerResponse(HttpStatusCode.OK, "Gets all actions.", typeof(ResourceApiViewModel)),
           SwaggerResponse(HttpStatusCode.NotFound, "Uid {uid} not found."),
           SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
           SwaggerParameter("_pageSize", "The number of results to return per page. The default is 5 users per page and max value is 250.", DataType = "integer", ParameterType = "query", Required = false),
           SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
           SwaggerParameter("_order", "The way in which to order the results.", DataType = "string", ParameterType = "query", Required = false),
       ]
        public async Task<IHttpActionResult> GetIssues(string actionTypeUid = null, string assetUid = null)
        {
            string finalSql = "";
            string countSql = @"SELECT 
                                    count(*)
                                     FROM[dbo].[Issue] I
                                    inner join[dbo].[IssueType] IT on IT.ID = I.IssueTypeID
                                    left join[dbo].Asset A on A.Object = I.Object and A.ObjectID = I.ObjectID
                                    left join[dbo].AssetType AT on AT.Object = I.ObjectType and AT.ObjectID = I.ObjectTypeID
                                    left join[reporting].[Global_Resource] R on R.ResourceID = I.CreatedBy
                                    left join[reporting].[Global_Resource] GR on GR.ResourceID = I.UpdatedBy";
            string selectSql = @"SELECT 
                                    A.uid as 'AssetUid',
                                    AT.uid as 'AssetTypeUid',
                                    IT.Name as 'ActionTypeName',
                                    IT.uid as 'ActionTypeUid',
                                    I.CreatedOn,
                                    R.uid as CreatedByUid,
                                    I.UpdatedOn,
                                    GR.uid as UpdatedByUid";
            string whereSql = "";
            string joinsSql = " ";
            List<string> queries = new List<string>();
            List<string> fieldColumns = new List<string>();
            List<string> fieldJoins = new List<string>();

            DynamicParameters dbArgs = new DynamicParameters();
            ResourceApiViewModel model = new ResourceApiViewModel();
            var pageSize = 5;
            var pageNum = 1;
            var queryParams = Request.GetQueryNameValuePairs();
            queryParams.ToList().ForEach(q =>
            {
                var key = q.Key.ToLower();
                if (key.StartsWith("_"))
                {
                    switch (key)
                    {
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

            if (actionTypeUid != null)
            {
                Guid atGuid = new Guid();
                if (Guid.TryParse(actionTypeUid, out atGuid))
                {
                    IssueType issueType = this.issueRepository.GetIssueTypeByUID(atGuid);

                    if (issueType == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Action Type with Uid {actionTypeUid} could not be found."));
                    else
                    {
                        var fieldTypes = Company.FieldTypes.Where(f => f.Object == "IssueType" && f.ObjectID == issueType.ID).ToList();
                        getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);

                        foreach (var col in fieldColumns)
                        {
                            selectSql += "," + col;
                        }

                        foreach (var join in fieldTypes)
                        {
                            string fieldJoin = "left join Field F" + join.ID + " on F" + join.ID + ".FieldTypeID =" + join.ID + " and F" + join.ID + ".[ObjectType] = 'Issue' and F" + join.ID + ".[ObjectID] = " + join.Fields.FirstOrDefault().ObjectID + " ";
                            joinsSql += fieldJoin;
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
                    }

                }
                else
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Invaild GUID {actionTypeUid}."));
                }
            }

            if (assetUid != null)
            {
                Guid aGuid = new Guid();
                if (Guid.TryParse(assetUid, out aGuid))
                {
                    Asset asset = this.assetRepository.GetAssetByUID(aGuid);

                    if (asset == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset with Uid {assetUid} could not be found."));
                }
                else
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Invaild GUID {assetUid}."));
                }
            }

            if (actionTypeUid != null)
            {
                queries.Add("IT.uid = @actionTypeUid");
                dbArgs.Add("actionTypeUid", actionTypeUid);
            }
            if (assetUid != null)
            {
                queries.Add("A.uid = @assetUid");
                dbArgs.Add("assetUid", assetUid);
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
                whereSql += " where ";
            }

            for (int i = 0; i < queries.Count(); i++)
            {
                whereSql += queries[i].ToString();
                if (i < queries.Count() - 1)
                {
                    whereSql += " and ";
                }
            }
            finalSql = selectSql + @" FROM[dbo].[Issue] I
                                    inner join[dbo].[IssueType] IT on IT.ID = I.IssueTypeID
                                    left join[dbo].Asset A on A.Object = I.Object and A.ObjectID = I.ObjectID
                                    left join[dbo].AssetType AT on AT.Object = I.ObjectType and AT.ObjectID = I.ObjectTypeID
                                    left join[reporting].[Global_Resource] R on R.ResourceID = I.CreatedBy
                                    left join[reporting].[Global_Resource] GR on GR.ResourceID = I.UpdatedBy" + joinsSql + whereSql;
            countSql += whereSql;
            if (pageSize > 0 || pageNum > 0)
            {
                if (pageSize < 1) pageSize = 1;
                if (pageNum < 1) pageNum = 1;
                model.pageNum = pageNum;
                model.pageSize = pageSize;
                string offsetSql = $" Order by gr.ResourceID offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
                finalSql += offsetSql;
            }
            var count = await Company.QueryAsync<int>(countSql, dbArgs);
            var results = await Company.QueryAsync<dynamic>(finalSql, dbArgs);
            model.total = count.FirstOrDefault();
            model.items = results;
            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model)));
        }

        /// <summary>
        /// Returns all actions types that are defined in Govern.  
        /// 
        /// </summary>
        /// <returns>A list of actions types</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A full list of actions types.", typeof(List<IssueTypeApiModel>)),
        ]
        public async Task<HttpResponseMessage> GetIssueTypes()
        {
            var issueTypes = await issueRepository.GetIssueTypes();

            return Request.CreateResponse(issueTypes);
        }

        /// <summary>
        /// Returns actions types that are associated with a particular asset type
        /// </summary>
        /// <param name="AssetTypeUid">Asset Type Uid</param>
        /// <returns>A list of actions types</returns>
        [HttpGet,
            Route("types/{AssetTypeUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(IssueTypeApiModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset Type with Uid {uid} not found."),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
            ]
        public async Task<IHttpActionResult> GetAllocationByAssetTypeAsync(Guid AssetTypeUid)
        {
            var prefix = "Issues.GetAllocationByAssetTypeAsync => ";
            var errorMessage = "";

            try
            {
                AssetType assetType = this.assetRepository.GetAssetTypeByUID(AssetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {AssetTypeUid} could not be found."));

                var allocations=  await this.issueRepository.GetAllocationByAssetType(AssetTypeUid);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, allocations)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }
    }
}