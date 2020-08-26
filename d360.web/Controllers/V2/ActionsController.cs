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
using Newtonsoft.Json;
using static d360.core.entities.Resource;
using System.Web.Http.Description;
using d360.core;

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
        /// <param name="_pageSize">The number of results to return per page. The default is 5 actions per page and max value is 250.</param>
        /// <param name="_pageNum">The page number to return results for.</param>
        /// <param name="_order">The field to use to order the results.</param>
        /// <param name="_direction">The direction in which to order the results (asc/desc). Used in conjunction with _order.</param>
        [
           HttpGet,
           MapToApiVersion("2.0"),
           Route(""),
           SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
           SwaggerResponse(HttpStatusCode.OK, "Gets all actions.", typeof(ResourceApiViewModel)),
           SwaggerResponse(HttpStatusCode.NotFound, "Uid {uid} not found."),
           SwaggerResponse(HttpStatusCode.BadRequest, "Invalid PageSize/PageNum value provided. Number is too large"),
           SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
       ]
        public async Task<IHttpActionResult> GetActions(string actionTypeUid = null, string assetUid = null, string _pageSize = "5", string _pageNum = "1", string _order = null, string _direction = "asc")
        {
            List<string> selectColumns = new List<string>() { 
                "I.Uid", "I.CompletedOn",
                "A.Uid as AssetUid", "A.AssetTypeUid", "A.TypeName as AssetTypeName", 
                "IT.uid as ActionTypeUid", "IT.Name as ActionTypeName", 
                "I.CreatedOn", "CR.Uid as CreatedByUid", "I.UpdatedOn", "UR.Uid as UpdatedByUid" 
            };
            List<string> queries = new List<string>();
            List<string> fieldJoins = new List<string>() {
                "inner join[dbo].[IssueType] IT on IT.ID = I.IssueTypeID", 
                "left join AssetDetail A on A.Object = I.Object and A.ObjectID = I.ObjectID",
                "left join[reporting].[Global_Resource] CR on CR.ResourceID = I.CreatedBy",
                "left join[reporting].[Global_Resource] UR on UR.ResourceID = I.UpdatedBy"
            };

            DynamicParameters dbArgs = new DynamicParameters();
            ResourceApiViewModel model = new ResourceApiViewModel();
            bool isOrderByFieldValid = false;
            long pageSize;
            long pageNum;

            #region Determine paging
            if (string.IsNullOrEmpty(_pageSize))
                _pageSize = "5";

            if (string.IsNullOrEmpty(_pageNum))
                _pageNum = "1";

            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", _pageSize }, { "_pageNum", _pageNum } };
            string isValid = isPageSizeAndNumValid(pageParams);
            
            if (!string.IsNullOrEmpty(isValid))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, isValid));
            }

            long.TryParse(_pageSize, out pageSize);
            long.TryParse(_pageNum, out pageNum);

            model.pageNum = pageNum;
            model.pageSize = pageSize;

            #endregion

            #region Determine order by

            switch (_direction)
            {
                case "asc":
                case "desc":
                    break;
                default:
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid value for _direction. Allowed values are: asc; desc"));
            }

            if (string.IsNullOrEmpty(_order))
            {
                _order = $"I.CreatedOn";
                isOrderByFieldValid = true;
            }
            else 
            {
                _order = _order.Trim();
                switch (_order) {
                    case "CompletedOn":
                        _order = $"I.CompletedOn";
                        isOrderByFieldValid = true;
                        break;
                    case "AssetUid":
                        _order = $"CAST(A.uid AS VARCHAR(36))";
                        isOrderByFieldValid = true;
                        break;
                    case "AssetTypeUid":
                        _order = $"CAST(A.AssetTypeUid AS VARCHAR(36))";
                        isOrderByFieldValid = true;
                        break;
                    case "ActionTypeName":
                        _order = $"IT.Name";
                        isOrderByFieldValid = true;
                        break;
                    case "ActionTypeUid":
                        _order = $"CAST(IT.uid AS VARCHAR(36))";
                        isOrderByFieldValid = true;
                        break;
                    case "CreatedOn":
                        _order = $"I.CreatedOn";
                        isOrderByFieldValid = true;
                        break;
                    case "CreatedByUid":
                        _order = $"CAST(CR.uid AS VARCHAR(36))";
                        isOrderByFieldValid = true;
                        break;
                    case "UpdatedOn":
                        _order = $"I.UpdatedOn";
                        isOrderByFieldValid = true;
                        break;
                    case "UpdatedByUid":
                        _order = $"CAST(UR.uid AS VARCHAR(36))";
                        isOrderByFieldValid = true;
                        break;
                }
            }

            #endregion

            var queryParams = Request.GetQueryNameValuePairs();

            if (!string.IsNullOrEmpty(actionTypeUid) && !string.IsNullOrWhiteSpace(actionTypeUid))
            {
                if (Guid.TryParse(actionTypeUid, out Guid atGuid))
                {
                    IssueType issueType = this.issueRepository.GetIssueTypeByUID(atGuid);

                    if (issueType == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Action Type with Uid {actionTypeUid} could not be found."));
                    else
                    {
                        queries.Add("IT.[Uid] = @actionTypeUid");
                        dbArgs.Add("actionTypeUid", actionTypeUid);

                        var fieldTypes = Company.Filter<FieldType>(f => f.Object == "IssueType" && f.ObjectID == issueType.ID).ToList();
                        getFieldSql(fieldTypes, dbArgs, fieldJoins, selectColumns, "'Issue'", "I.ID");

                        foreach (FieldType customField in fieldTypes)
                        {
                            if (queryParams.Any(x => x.Key == customField.Name))
                            {
                                var dynamicFieldFilterValue = queryParams.FirstOrDefault(x => x.Key == customField.Name).Value;

                                queries.Add($"F{customField.ID}.FormattedValue = @field{customField.ID}");

                                dbArgs.Add($"@field{customField.ID}", dynamicFieldFilterValue);
                            }

                            if (_order.ToLower() == customField.Name.ToLower())
                            {
                                _order = $"F{customField.ID}.FormattedValue";
                                isOrderByFieldValid = true;
                            }
                        }
                    }

                }
                else
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Action Type Not Found", $"Invalid GUID {actionTypeUid}."));
                }
            }

            if (!isOrderByFieldValid)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Order By Field Not Found", $"The field you specified for sorting ({_order}) could not be found."));
            }

            if (!string.IsNullOrEmpty(assetUid) && !string.IsNullOrWhiteSpace(assetUid))
            {
                if (Guid.TryParse(assetUid, out Guid aGuid))
                {
                    Asset asset = this.assetRepository.GetAssetByUID(aGuid);
                    if (asset == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Asset Not found", $"Asset with Uid {assetUid} could not be found."));

                    queries.Add("A.[Uid] = @assetUid");
                    dbArgs.Add("assetUid", assetUid);
                }
                else
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Asset Not found", $"Invalid GUID {assetUid}."));
                }
            }

            #region Build SQL statements

            string workflowCheckSql = "exists (select 1 from workflow.Item where Object = 'Issue' and ObjectID = I.ID)";
            string columns = string.Join(", ", selectColumns);
            string conditions = string.Empty;
            string joins = string.Join(" ", fieldJoins);
            if (queries.Count() > 0)
            {
                conditions += " and " + string.Join(" and ", queries);
                conditions = conditions.Trim();
            }
           
            string resultsSql = $"select {columns} from Issue I {joins} where {workflowCheckSql} {conditions} order by {_order} {_direction} offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
            string countSql = string.IsNullOrEmpty(conditions) ? 
                $"select count(*) from Issue I where {workflowCheckSql}" : 
                $"select count(*) from Issue I {joins} where {workflowCheckSql} {conditions}";

            #endregion

            var count = await Company.QueryAsync<int>(countSql, dbArgs);
            var results = await Company.QueryAsync<dynamic>(resultsSql, dbArgs);
            model.total = count.FirstOrDefault();
            model.items = results;

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model)));
        }

        /// <summary>
        /// Gets detailed field information regarding a specific asset that a user selects from the Asset Browser UI.
        /// </summary>
        /// <param name="model">The uid of the asset that we are getting field information for.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            Route("alerts"),
            ApiExplorerSettings(IgnoreApi = true),
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(AssetBrowserDiagramAsset)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> RetrieveAlertsForAssets(AssetBrowserAlertRequest model)
        {
            try
            {
                var sql = @"
select	I.uid as 'uid', 
        A.uid as 'asset.uid',
		coalesce(A.icon, 'fa-book') as 'asset.icon',
		A.TypeName + ' > ' + A.DisplayValue as 'asset.displayValue',
		IT.name as 'action.name', 
		reporting.StripHTML(F.FormattedValue) as 'action.description'
from	AssetDetail A
        inner join @uids U on U.Uid = A.Uid
		inner join Issue I on I.Object = A.Object and I.ObjectID = A.ObjectID
		left join IssueType IT on IT.ID = I.IssueTypeID
		left join FieldType FT on FT.Object = 'IssueType' and FT.ObjectID = IT.ID and (FT.Name = 'Description' or FT.Name = 'ProblemDesc')
		left join Field F on F.FieldTypeID = FT.ID and F.ObjectType = 'Issue' and F.ObjectID = I.ID
where	I.CompletedOn is null
        and exists (select 1 from workflow.Item where Object = 'Issue' and ObjectID = I.ID)
for json path";

                if (model == null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have passed an empty or invalid set of criteria.");
                }
                else if (model.assets.Count == 0)
                {
                    AssetBrowserAlert[] alerts = new AssetBrowserAlert[0];
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.NoContent, alerts)));
                }

                var reader = await Company.QueryAsync<string>(sql,
                    new
                    {
                        uids = model.assets.Select(i => i.uid).Distinct().AsTableValuedParameter(
                            "dbo.UidTable",
                            new List<string>() { "Uid" }
                            )
                    }, timeout: 100);
                var json = string.Join("", reader);

                var returnModel = JsonConvert.DeserializeObject<AssetBrowserAlert[]>(json);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, returnModel)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", "BrowserController.GetDiagramAlerts" },
                    { "model", JsonConvert.SerializeObject(model) }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
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

        /// <summary>
        /// Creates a workflow action type
        /// </summary>
        /// <param name="model">The information of the workflow action type to be created</param>
        [
            Route("type"),
            HttpPost,
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Workflow Action Type successfully created.", typeof(AddIssueTypeApiModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> AddWorkflowActionType(AddWorkFlowAction model)
        {
            var prefix = "Issues.AddWorkflowActionType => ";
            AddIssueTypeApiModel result = new AddIssueTypeApiModel();
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, "Forbidden", $"Access denied"));

                if (model.Uid != null)
                {
                    var validUid = Company.IssueTypes.Any(i => i.uid == model.Uid);

                    if (validUid)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"Uid provided already in use."));
                }

                if (string.IsNullOrEmpty(model.Name.Trim()))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"Empty string provided for Name. Cannot be empty."));

                if (model.Name.Trim().Length > 250)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"Name provided must be less then 250 characters in length."));

                var validName = Company.IssueTypes.Any(i => i.Name.ToLower() == model.Name.Trim().ToLower());

                if (validName)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"Name must be unique. Workflow action already exists with this name"));

                if (model.Uid == null || model.Uid == Guid.Empty)
                    model.Uid = Guid.NewGuid();

                var res = await Company.Database.Connection.ExecuteAsync(@" insert into [dbo].[IssueType]([Name],[Description],[IsSystem],[UpdatedOn]
                ,[UpdatedBy],[uid]) values(@name,@desc,0,@date,@user,@uid)",
                new { name = model.Name.Trim(), desc = model.Description, user = Company.CurrentResourceID, uid = model.Uid, date = DateTime.UtcNow });


                if (res > 0)
                {
                    var issueType = Company.IssueTypes.Where(i => i.Name.ToLower() == model.Name.ToLower()).FirstOrDefault();
                    Company.Add(new FieldType
                    {
                        ObjectID = issueType.ID,
                        Object = SystemObjects.IssueType.ToString(),
                        IsListable = true,
                        IsRequired = true,
                        IsEditable = true,
                        FriendlyName = "Description",
                        Name = "ProblemDesc",
                        SortOrder = 1,
                        Type = DataType.Html.ToString()
                    });
                }

                result.Uid = (Guid)model.Uid;
                result.Message = "Action Type is created";
                result.Success = true;

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result)));
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
    }
}