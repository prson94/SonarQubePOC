using d360.core.entities;
using d360.core.exceptions;
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
using System.Data;
using Resources;
using d360.core.enums;
using System.Web;

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
        IAssetRepository assetRepository;
        ICommentRepository commentRepository;
        IIssueRepository issueRepository;
        IResponsibilityRepository responsibilityRepository;

        public ActionsController(ICoreComponentSet set, ICommentRepository comments, IIssueRepository issues, IAssetRepository assets, IResponsibilityRepository responsibilities)
            : base(set)
        {
            assetRepository = assets;
            commentRepository = comments;
            issueRepository = issues;
            responsibilityRepository = responsibilities;
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
           SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
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
                "inner join [dbo].[IssueType] IT on IT.ID = I.IssueTypeID",
                "left join AssetDetail A on A.Object = I.Object and A.ObjectID = I.ObjectID",
                "left join [reporting].[Global_Resource] CR on CR.ResourceID = I.CreatedBy",
                "left join [reporting].[Global_Resource] UR on UR.ResourceID = I.UpdatedBy"
            };

            DynamicParameters dbArgs = new DynamicParameters();
            ResourceApiViewModel model = new ResourceApiViewModel();
            bool isOrderByFieldValid = false;
            long pageSize;
            long pageNum;

            #region Determine paging
            if (string.IsNullOrEmpty(_pageSize))
            {
                _pageSize = "5";
            }

            if (string.IsNullOrEmpty(_pageNum))
            {
                _pageNum = "1";
            }

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
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidDirection));
            }

            if (string.IsNullOrEmpty(_order))
            {
                _order = $"I.CreatedOn";
                isOrderByFieldValid = true;
            }
            else
            {
                _order = _order.Trim();
                switch (_order)
                {
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
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.ActionTypeUidIsNotValid, actionTypeUid))).ConfigureAwait(false);
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
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ActionApiMessages.ActionTypeNotFound, string.Format(ApiMessages.InvalidGuid, actionTypeUid))).ConfigureAwait(false);
                }
            }

            if (!isOrderByFieldValid)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ActionApiMessages.OrderByFieldNotFound, string.Format(ActionApiMessages.OrderByFieldNotFoundMessage, _order))).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(assetUid) && !string.IsNullOrWhiteSpace(assetUid))
            {
                if (Guid.TryParse(assetUid, out Guid aGuid))
                {
                    Asset asset = this.assetRepository.GetAssetByUID(aGuid);
                    if (asset == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.AssetNotfound, string.Format(ActionApiMessages.AssetUidIsNotValid, assetUid))).ConfigureAwait(false);
                    }

                    queries.Add("A.[Uid] = @assetUid");
                    dbArgs.Add("assetUid", assetUid);
                }
                else
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.AssetNotfound, string.Format(ApiMessages.InvalidGuid, assetUid))).ConfigureAwait(false);
                }
            }

            #region Build SQL statements

            string columns = string.Join(", ", selectColumns);
            string conditions = string.Empty;
            string joins = string.Join(" ", fieldJoins);
            if (queries.Count() > 0)
            {
                conditions += " where " + string.Join(" and ", queries);
                conditions = conditions.Trim();
            }

            string resultsSql = $"select {columns} from Issue I {joins} {conditions} order by {_order} {_direction} offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
            string countSql = $"select count(*) from Issue I {joins} {conditions}";

            #endregion

            var count = await Company.QueryAsync<int>(countSql, dbArgs, ApiTimeout);
            var results = await Company.QueryAsync<dynamic>(resultsSql, dbArgs, ApiTimeout);
            model.total = count.FirstOrDefault();
            model.items = results;

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model))).ConfigureAwait(false);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
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
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.EmptyInvalidParameterSet);
                }
                else if (model.assets.Count == 0)
                {
                    AssetBrowserAlert[] alerts = new AssetBrowserAlert[0];
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.NoContent, alerts))).ConfigureAwait(false);
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

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, returnModel))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", "BrowserController.GetDiagramAlerts" },
                    { "model", JsonConvert.SerializeObject(model) }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns all defined actions types.
        /// </summary>
        /// <returns>A list of actions types</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A full list of actions types.", typeof(List<IssueTypeApiModel>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request Parameters are invalid.", typeof(List<IssueTypeApiModel>)),
            SwaggerResponse(HttpStatusCode.NotFound, "No matching uid for the Action Type/Asset Type/Asset Uid Provided.", typeof(List<IssueTypeApiModel>)),
            SwaggerParameter("_actionTypeUid", "Filter by provided action type Uid.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_resourceUid", "Filter by provided resource Uid.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetTypeUid", "Filter by provided asset type Uid.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetUid", "Filter by provided asset Uid.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_name", "Filter by provided name value.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_limitToActiveWorkflows", "Set to true to only return actions associated with an active workflow.", DataType = "boolean", ParameterType = "query", Required = false),
        ]
        public async Task<HttpResponseMessage> GetIssueTypes()
        {
            var queryParams = Request.GetQueryNameValuePairs();

            #region validate Parameters
            var actionTypeUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_actiontypeuid");

            if (actionTypeUidParam.Key != null)
            {
                if (Guid.TryParse(actionTypeUidParam.Value, out Guid actionTypeUid))
                {
                    var validUid = Company.IssueTypes.Any(i => i.uid == actionTypeUid);

                    if (!validUid)
                    {
                        return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format(ActionApiMessages.ActionTypeUidIsNotValid, actionTypeUid.ToString()))).ConfigureAwait(false);
                    }
                }
                else
                {
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ActionApiMessages.InvalidActionTypeUid)).ConfigureAwait(false);
                }
            }

            var assetTypeUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_assettypeuid");

            if (assetTypeUidParam.Key != null && assetTypeUidParam.Value != null && !string.IsNullOrWhiteSpace(assetTypeUidParam.Value))
            {
                if (Guid.TryParse(assetTypeUidParam.Value.Trim(), out Guid assetTypeUid))
                {
                    var assetType = Company.AssetTypes.FirstOrDefault(i => i.uid == assetTypeUid);                    

                    if (assetType == null)
                    {
                        return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString()))).ConfigureAwait(false);
                    }
                    else if (assetType.Class == AssetTypeClass.Diagram)
                    {
                        return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ActionApiMessages.InvalidAssetTypeUid)).ConfigureAwait(false);
                    }

                }
                else
                {
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ActionApiMessages.InvalidAssetTypeUid)).ConfigureAwait(false);
                }
            }

            var assetUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_assetuid");

            if (assetUidParam.Key != null && assetUidParam.Value != null && !string.IsNullOrWhiteSpace(assetUidParam.Value))
            {
                if (Guid.TryParse(assetUidParam.Value.Trim(), out Guid assetUid))
                {
                    var asset = Company.Assets.FirstOrDefault(i => i.uid == assetUid);

                    if (asset == null)
                    {
                        return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format(ActionApiMessages.AssetNotFound,assetUid.ToString()))).ConfigureAwait(false);
                    }
                    else if(asset.AssetType.Class == AssetTypeClass.Diagram)
                    {
                        return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ActionApiMessages.InvalidAssetUid)).ConfigureAwait(false);
                    }

                    if (assetTypeUidParam.Key != null && assetTypeUidParam.Value != null && !string.IsNullOrWhiteSpace(assetTypeUidParam.Value))
                    {
                        var assetTypeuUid = Guid.Parse(assetTypeUidParam.Value);
                        if (!Company.AssetTypes.Any(i => i.uid == assetTypeuUid && i.ID == asset.AssetTypeID))
                        {
                            return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.AssetValidateWithAssetType)).ConfigureAwait(false);
                        }
                    }

                }
                else
                {
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ActionApiMessages.InvalidAssetUid)).ConfigureAwait(false);
                }
            }

            var nameParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_name");

            if (nameParam.Key != null)
            {
                if (string.IsNullOrEmpty(nameParam.Value.Trim()))
                {
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest,ActionApiMessages.NameNotEmptyAndRequired)).ConfigureAwait(false);
                }

                if (nameParam.Value.Trim().Length > 250)
                {
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ActionApiMessages.NameMaxLength250Char)).ConfigureAwait(false);
                }
            }

            var resourceUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_resourceuid");

            if (resourceUidParam.Key != null && !string.IsNullOrWhiteSpace(resourceUidParam.Value))
            {
                if (Guid.TryParse(resourceUidParam.Value, out Guid resourceUid))
                {
                    var validUid = Company.GlobalReportingResources.Any(r => r.Uid == resourceUid);

                    if (!validUid)
                    {
                        return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.NotFound, ActionApiMessages.ResourceUidNotFound)).ConfigureAwait(false);
                    }
                }
                else
                {
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ActionApiMessages.ResourceUidNotValid)).ConfigureAwait(false);
                }
            }

            var limitToActiveWorkflowsParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_limittoactiveworkflows");

            if (limitToActiveWorkflowsParam.Key != null && !string.IsNullOrWhiteSpace(limitToActiveWorkflowsParam.Value))
            {
                if (!bool.TryParse(limitToActiveWorkflowsParam.Value, out _))
                {
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ActionApiMessages.InvalidLimitActiveWorkflow)).ConfigureAwait(false);
                }
            }

            #endregion

            var issueTypes = await issueRepository.GetIssueTypes(queryParams);

            return Request.CreateResponse(HttpStatusCode.OK, issueTypes);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
            ]
        public async Task<IHttpActionResult> GetAllocationByAssetTypeAsync(Guid AssetTypeUid)
        {
            var prefix = "Issues.GetAllocationByAssetTypeAsync => ";
            var errorMessage = "";

            try
            {
                AssetType assetType = this.assetRepository.GetAssetTypeByUID(AssetTypeUid);

                if (assetType == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetTypeNotFound, AssetTypeUid.ToString()))).ConfigureAwait(false);
                }

                var allocations = await this.issueRepository.GetAllocationByAssetType(AssetTypeUid);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, allocations))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix  }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> AddWorkflowActionType(AddWorkFlowAction model)
        {
            var prefix = "Issues.AddWorkflowActionType => ";
            AddIssueTypeApiModel result = new AddIssueTypeApiModel();
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage
)).ConfigureAwait(false);
                }

                if (model.Uid != null)
                {
                    var validUid = Company.IssueTypes.Any(i => i.uid == model.Uid);

                    if (validUid)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.UniqueUid)).ConfigureAwait(false);
                    }
                }

                if (string.IsNullOrEmpty(model.Name.Trim()))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.NameNotEmptyAndRequired)).ConfigureAwait(false);
                }

                if (model.Name.Trim().Length > 250)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.NameMaxLength250Char)).ConfigureAwait(false);
                }

                var validName = Company.IssueTypes.Any(i => i.Name.ToLower() == model.Name.Trim().ToLower());

                if (validName)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.UniqueNameWorkflowAction)).ConfigureAwait(false);
                }

                if (model.Uid == null || model.Uid == Guid.Empty)
                {
                    model.Uid = Guid.NewGuid();
                }

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

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage));
            }
        }


        /// <summary>
        /// Updates a workflow action type
        /// </summary>
        /// <param name="model">The information of the workflow action type to be updated</param>
        [
            Route("type"),
            HttpPut,
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Workflow Action Type successfully Updated.", typeof(AddIssueTypeApiModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "User is not an administrator.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> UpdateWorkflowActionType(AddWorkFlowAction model)
        {
            var prefix = "Issues.AddWorkflowActionType => ";
            AddIssueTypeApiModel result = new AddIssueTypeApiModel();
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage));

                if (model.Uid == null || model.Uid == Guid.Empty)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.UidNotEmptyAndRequired)).ConfigureAwait(false);
                }

                var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == model.Uid);

                if (issueType == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.UidNotValid)).ConfigureAwait(false);
                }


                if (model.Name == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.NameNotNull)).ConfigureAwait(false);
                }

                if (string.IsNullOrEmpty(model.Name.Trim()))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.NameNotEmptyAndRequired)).ConfigureAwait(false);
                }

                if (model.Name.Trim().Length > 250)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.NameMaxLength250Char)).ConfigureAwait(false);
                }

                var validName = Company.IssueTypes.Any(i => i.Name.ToLower() == model.Name.Trim().ToLower() && i.uid != model.Uid);

                if (validName)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest,ActionApiMessages.UniqueNameWorkflowAction)).ConfigureAwait(false);
                }

                if (model.Description == null)
                {
                    model.Description = issueType.Description;
                }

                var updateSQL = $@"Update [dbo].[IssueType]
                                        set [Name]= @name, [Description]=@desc, [UpdatedOn] = @date ,[UpdatedBy] = @user
                                   Where uid = @uid";


                var res = await Company.Database.Connection.ExecuteAsync(updateSQL,
                new { name = model.Name.Trim(), desc = model.Description, user = Company.CurrentResourceID, uid = model.Uid, date = DateTime.UtcNow });

                result.Uid = (Guid)model.Uid;
                result.Message = "Action Type updated";
                result.Success = true;

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage));
            }
        }

        /// <summary>
        /// Deletes a workflow action type
        /// </summary>
        /// <param name="actionTypeUid">Uid of the action type to be deleted</param>
        /// <param name="model">Request body containing cascade flag</param>
        [
            Route("type/{actionTypeUid:Guid}"),
            HttpDelete,
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Action Type was deleted.", typeof(AddIssueTypeApiModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteWorkflowActionType(Guid actionTypeUid, DeleteIssueTypeAPIModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage)).ConfigureAwait(false);
            }

            if (actionTypeUid == null || actionTypeUid == Guid.Empty)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.InvalidActionTypeUid)).ConfigureAwait(false);
            }


            var queryParams = Request.GetQueryNameValuePairs();
            bool IsFromUI = false;

            if ((queryParams.Any(p => p.Key.Trim().ToLower() == "_requestfromui")))
            {
                var val = queryParams.ToList().First(k => k.Key.ToLower() == "_requestfromui");

                if (!bool.TryParse(val.Value, out _))
                {
                    IsFromUI = false;
                }
                else
                {
                    IsFromUI = true;
                }

            }

            var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == actionTypeUid);

            if (issueType == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(ActionApiMessages.InvalidActionTypeUid, actionTypeUid.ToString()))).ConfigureAwait(false);
            }

            if (!model.cascade && (Company.Issues.Any(x => x.IssueTypeID == issueType.ID)))
            {
                if (IsFromUI)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(ActionApiMessages.ChildRecordExistsIssueType, issueType.Name))).ConfigureAwait(false);
                }
                else
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.CascadeDeleteActionType)).ConfigureAwait(false);
                }
            }

            var deleteSQL = $@" DELETE FROM IssueTypeRelation Where IssueTypeID = @issueTypeId
                                
                                DELETE FROM Issue Where IssueTypeID = @issueTypeId
                                
                                DELETE FROM IssueType Where uid = @uid";

            var res = await Company.Database.Connection.ExecuteAsync(deleteSQL,
                new { uid = actionTypeUid, issueTypeId = issueType.ID });

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new AddIssueTypeApiModel() { Uid = actionTypeUid, Message = "Action Type was deleted", Success = true }))).ConfigureAwait(false);
        }

        /// <summary>
        /// Create an action
        /// </summary>        
        /// <param name="actionTypeUid">The Uid of the action type</param>
        /// <param name="models">Collection of Issues/Actions</param>
        /// <param name="lookupFieldsPassedByValue">Optional query string parameter that allows you to pass list values numeric value instead of plain text value.  The default value for this is false.</param>
        /// <returns>Response with the uid of the action created.</returns>
        [
            HttpPost,
            Route("{ActionTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Response containing the uid of the action created", typeof(List<ApiStatusResponse>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid request parameters provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Invalid request parameters provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Insufficient permissions for this request.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> CreateAction(Guid actionTypeUid, List<ActionUpsertRequest> models, bool lookupFieldsPassedByValue = false)
        {
            try
            {
                bool isWriteActionDescriptionEnabled = IsWriteActionDescriptionEnabled();

                List<ApiStatusResponse> response = new List<ApiStatusResponse>();

                List<IssueInsertAPIModel> issueModels = new List<IssueInsertAPIModel>();

                if (actionTypeUid == null || actionTypeUid == Guid.Empty)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.InvalidActionTypeUid)).ConfigureAwait(false);
                }

                var issueType = Company.Filter<IssueType>(i => i.uid == actionTypeUid).SingleOrDefault();

                if (issueType == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.NotFound, string.Format(ActionApiMessages.ActionTypeUidIsNotValid, actionTypeUid.ToString()))).ConfigureAwait(false);
                }

                WorkHttpStatus validationStatus = PopulateRequest(models, ref issueModels, issueType, lookupFieldsPassedByValue);
                if (validationStatus.StatusCode != HttpStatusCode.OK)
                {
                    return await Task.FromResult(errorMessageResponse(validationStatus.StatusCode, validationStatus.Error, validationStatus.Message)).ConfigureAwait(false);
                }

                foreach (var issueModel in issueModels)
                {
                    var assetType = Company.AssetTypes.FirstOrDefault(i => i.Object == issueModel.Issue.ObjectType && i.ObjectID == issueModel.Issue.ObjectTypeID);

                    if (!Company.CurrentResourceIsAdmin && !Company.HasAssetTypePermission(assetType.Object, assetType.ID, Permission.ReadAsset))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, ActionApiMessages.AssetTypeAddActionPermissionsDenied)).ConfigureAwait(false);
                    }
                    if (isWriteActionDescriptionEnabled)
                    {
                        var actionAsset = assetRepository.GetAssetByObjectId(issueModel.Issue.Object, issueModel.Issue.ObjectID);

                        if (actionAsset != null)
                        {
                            var comment = new CommentApiPostModel
                            {
                                AssetUid = actionAsset.uid,
                                Body = issueModel.Comment ?? string.Format(ActionApiMessages.ActionAssetCommentBody, issueType.Name),
                                Tags = new List<Guid> { actionAsset.uid }       // Add relation to current artifact
                            };
                            var dtl = await commentRepository.AddComment(comment, CommentType.Issue);
                            issueModel.Issue.CommentID = dtl.ID;
                        }
                    }

                    var insertSQL = $@"INSERT INTO [dbo].[Issue]
                                                   ([IssueTypeID]
                                                   ,[Object]
                                                   ,[ObjectID]
                                                   ,[ObjectType]
                                                   ,[ObjectTypeID]
                                                   ,[CreatedOn]
                                                   ,[CreatedBy]
                                                   ,[UpdatedOn]
                                                   ,[UpdatedBy]
                                                   ,[CommentID])
                                            OUTPUT inserted.Uid, inserted.ID
                                               VALUES
                                                   (@issueTypeID
                                                   ,@object
                                                   ,@objectID
                                                   ,@objectType
                                                   ,@objectTypeID
                                                   ,GETDATE()
                                                   ,@userId
                                                   ,GETDATE()
                                                   ,@userId
                                                   ,@commentId)";

                    var res = await Company.Database.Connection.QueryAsync<(Guid uid, int id)>(insertSQL, new { issueTypeID = issueType.ID, @object = issueModel.Issue.Object, objectID = issueModel.Issue.ObjectID, objectType = issueModel.Issue.ObjectType, objectTypeID = issueModel.Issue.ObjectTypeID, userId = Company.CurrentResourceID, commentId = issueModel.Issue.CommentID });

                    issueModel.Issue.ID = res.FirstOrDefault().id;
                    issueModel.Issue.UID = res.FirstOrDefault().uid;

                    if (issueModel.fields != null && issueModel.fields.Count > 0)
                    {
                        issueModel.fields.ForEach(i =>
                        {
                            i.ObjectID = issueModel.Issue.ID;
                        });
                        Company.AddOrUpdateFields(issueModel.fields);
                    }

                    response.Add(new ApiStatusResponse { Uid = issueModel.Issue.UID.Value, Message = ActionApiMessages.ActionCreatedMsg, Success = true });
                }

                Company.CreateEventsForAddedActions(issueModels.Select(x => x.Issue).ToList());

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response));
            }
            catch (BaseException ex)
            {
                return await Task.FromResult(errorMessageResponse(ex.StatusCode, ApiMessages.BadRequest, ex.StatusDescription)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage));
            }
        }

        private WorkHttpStatus ValidateRequest(IssueType issueType, ActionUpsertRequest model, out Asset asset, out AssetType assetType, bool lookupFieldsPassedByValue = false)
        {
            asset = null;
            assetType = null;
            int assetTypeID = 0;
            string assetTypeName = "";

            if ((model.AssetTypeUid == null && model.AssetUid == null) || (model.AssetTypeUid != null && model.AssetUid != null))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.AssetTypeOrAssetRequired);
            }

            if (model.AssetUid != null)
            {

                if (model.AssetUid.Value == Guid.Empty)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.InvalidAssetUid);
                }

                asset = assetRepository.GetAssetByUID(model.AssetUid.Value);

                if (asset == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetNotFound, model.AssetUid.Value));
                }

                if (!Company.HasAssetPermission(asset.Object, asset.ObjectID, Permission.ReadAsset))
                {
                    return new WorkHttpStatus(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, ActionApiMessages.AssetAddActionPermissionsDenied);
                }

                if (asset.Object == SystemObjects.ReferenceItem.ToString())
                {
                    return new WorkHttpStatus(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetUidIsNotValid, model.AssetUid.Value));
                }

                assetTypeID = asset.AssetTypeID;

                assetTypeName = asset.AssetType.Name;
            }

            if (model.AssetTypeUid != null)
            {
                if (model.AssetTypeUid.Value == Guid.Empty)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.InvalidAssetTypeUid);
                }

                assetType = assetRepository.GetAssetTypeByUID(model.AssetTypeUid.Value);

                if (assetType == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetTypeNotFound, model.AssetTypeUid.Value));
                }

                if (assetType.Class == AssetTypeClass.Reference)
                {
                    return new WorkHttpStatus(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetTypeUidIsNotValid, model.AssetTypeUid.Value));
                }

                assetTypeID = assetType.ID;
                assetTypeName = assetType.Name;
            }

            var allocations = Company.Filter<IssueTypeRelation>(r => r.IssueTypeID == issueType.ID).ToList();

            if (allocations.Count > 0 && !allocations.Any(a => a.AssetTypeID == assetTypeID))
            {
                return new WorkHttpStatus(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.NoMatchingAllocation, assetTypeName, issueType.Name));
            }

            var fieldTypes = Company.Filter<FieldType>(ft => ft.Object == SystemObjects.IssueType.ToString() && ft.ObjectID == issueType.ID);

            var fieldTable = new DataTable();
            fieldTable.Columns.Add("ExecutionID", typeof(Guid));
            fieldTable.Columns.Add("ItemNumber", typeof(int));
            fieldTable.Columns.Add("FieldName", typeof(string));
            fieldTable.Columns.Add("FieldValue", typeof(string));
            fieldTable.Columns.Add("FieldTypeID", typeof(int));

            foreach (var type in fieldTypes.Where(ft => ft.Type == DataType.Link.ToString()))
            {
                if (model.Fields.ContainsKey(type.Name + "_Name"))
                {
                    if(!string.IsNullOrEmpty(model.Fields[type.Name + "_Name"]) || !string.IsNullOrEmpty(model.Fields[type.Name + "_Url"]))
                    {
                        model.Fields.Add(type.Name, $"{model.Fields[type.Name + "_Name"]}|{model.Fields[type.Name + "_Url"]}");
                    }                    
                    model.Fields.Remove(type.Name + "_Name");
                    model.Fields.Remove(type.Name + "_Url");
                }
            }

            Company.ValidateFields(SystemObjects.IssueType.ToString(), issueType.ID, true, fieldTypes.ToList(), fieldTypes.Where(f => f.IsRequired && string.IsNullOrEmpty(f.DefaultValue) && f.Type != DataType.Counter.ToString()).Select(f => f.Name).ToList(), model.Fields, Guid.Empty, 1, fieldTable, out bool success, out string errorMessage);

            if (!success)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, errorMessage);
            }
            
            if (!lookupFieldsPassedByValue)
            { 
                foreach (var ft in fieldTypes.Where(x => x.Type == DataType.Lookup.ToString()))
                {                   
                    var lookupSQL = @"Select 
                                    * 
                                    from 
                                    FieldLookupValue 
                                    where 
                                    fieldTypeId = @fieldTypeId 
                                    and 
                                    text in @lookupValues
                                    ";

                    string[] lookupValues = { };

                    if (model.Fields.ContainsKey(ft.Name))
                    {
                        lookupValues = model.Fields[ft.Name].Trim().Split(',');
                    }

                    if (lookupValues.Length > 0)
                    {
                        var fieldLookupValues = Company.Database.Connection.Query<FieldLookupValue>(lookupSQL, new { fieldTypeId = ft.ID, lookupValues });

                        List<string> fieldValues = new List<string>();
                        foreach (var lookupValue in lookupValues)
                        {
                            if (fieldLookupValues.Any(x => x.Text == lookupValue))
                            {
                                fieldValues.Add(fieldLookupValues.FirstOrDefault(x => x.Text == lookupValue).Value.ToString());
                            }
                            else
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, $"Lookup Value  '{lookupValue}' is not valid for lookup '{ft.Name}'.");
                            }                           
                        }

                        model.Fields[ft.Name] = string.Join(",", fieldValues.Distinct());
                    }
                }
            }
            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        private WorkHttpStatus PopulateRequest(List<ActionUpsertRequest> models, ref List<IssueInsertAPIModel> issues, IssueType issueType, bool lookupFieldsPassedByValue = false)
        {
            foreach (var model in models)
            {
                var validationStatus = ValidateRequest(issueType, model, out Asset asset, out AssetType assetType, lookupFieldsPassedByValue);

                if (validationStatus.StatusCode != HttpStatusCode.OK)
                {
                    return validationStatus;
                }

                var issue = new Issue
                {
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow,
                    IssueTypeID = issueType.ID,
                    CommentID = 0
                };

                if (asset != null)
                {
                    issue.Object = asset.Object;
                    issue.ObjectID = asset.ObjectID;
                    issue.ObjectType = asset.AssetType.Object;
                    issue.ObjectTypeID = asset.AssetType.ObjectID;
                }
                else if (assetType != null)
                {
                    issue.Object = assetType.Object;
                    issue.ObjectID = assetType.ObjectID;
                    issue.ObjectType = assetType.Object;
                    issue.ObjectTypeID = assetType.ObjectID;
                }

                var fields = PopulateActionFields(issueType.ID,issue.ID, model.Fields);

                issues.Add(new IssueInsertAPIModel { Issue = issue, fields = fields, Comment = model.Fields.ContainsKey("ProblemDesc") ? model.Fields["ProblemDesc"] : null });
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        private List<Field> PopulateActionFields(int issueTypeId, int issueId, Dictionary<string, string> fields)
        {
            var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.IssueType, issueTypeId).ToList();

            var fieldList = new List<Field>();

            foreach (var ft in fieldTypes)
            {
                if (ft.Type != DataType.ComplexRelationLookup.ToString())
                {
                    string value = "";

                    if (fields.ContainsKey(ft.Name))
                    {
                        switch (ft.Type)
                        {
                            case "Boolean":
                                value = fields[ft.Name];
                                value = (value == "on" || (value ?? "").ToUpper() == "TRUE").ToString();
                                break;
                            case "Html":
                                value = HttpUtility.HtmlDecode(fields[ft.Name]);
                                break;
                            case "Date":
                                var stringDate = fields[ft.Name];
                                DateTime dateVal = DateTime.MinValue;
                                //throw out any time piece sent in
                                if (DateTime.TryParse(stringDate, out dateVal))
                                {
                                    value = dateVal.ToShortDateString();
                                }
                                break;
                            case "DateTime":
                                var stringDateTime = fields[ft.Name];
                                DateTime dateTimeVal = DateTime.MinValue;
                                if (DateTime.TryParse(stringDateTime, out dateTimeVal))
                                {
                                    value = dateTimeVal.ToString("s");
                                }
                                break;
                            case "Relationship":
                                break;
                            default:
                                value = fields[ft.Name];
                                break;
                        }

                        if (!string.IsNullOrEmpty(value))
                        {
                            fieldList.Add(new Field { FieldTypeID = ft.ID, ObjectID = issueId, ObjectType = SystemObjects.Issue.ToString(), Value = value });
                        }
                    }
                }                
            }
            return fieldList;
        }

        private bool IsWriteActionDescriptionEnabled()
        {
            var setting = SettingsRepository.GetSettings().Single(s => s.ID == Setting.WriteActionDescription);
            return (setting.Value == "true");
        }        

        /// <summary>
        /// Adds allocations to a workflow action type
        /// </summary>
        /// <param name="actionTypeUid">Uid of the action type the allocations are to be added to</param>
        /// <param name="assetTypeUids">Collection of asset type Uids to be added</param>
        [
            Route("allocations/{actionTypeUid:Guid}"),
            HttpPost,
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Allocations Added Successfully.", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Uid(s) provided are not valid.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> AddActionTypeAllocations(Guid actionTypeUid, List<string> assetTypeUids)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage)).ConfigureAwait(false);
                }

                if (actionTypeUid == null || actionTypeUid == Guid.Empty)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ActionApiMessages.InvalidActionTypeUid));
                }

                var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == actionTypeUid);

                if (issueType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ActionApiMessages.InvalidActionTypeUid));

                if (assetTypeUids.Count == 0)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.EmptyAllocationRequest)).ConfigureAwait(false);
                }

                List<IssueTypeRelation> allocations = new List<IssueTypeRelation>();

                foreach (var assetTypeUid in assetTypeUids.Distinct())
                {
                    if (!Guid.TryParse(assetTypeUid, out Guid uid))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetTypeUidIsNotValid, assetTypeUid))).ConfigureAwait(false);
                    }

                    var assetType = Company.AssetTypes.FirstOrDefault(i => i.uid == uid);

                    if (assetType == null || assetType.Class == AssetTypeClass.Diagram || assetType.Class == AssetTypeClass.Reference)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetTypeUidIsNotValid, assetTypeUid)));
                    }

                    if (!Company.IssueTypeRelations.Any(itr => itr.AssetTypeID == assetType.ID && itr.IssueTypeID == issueType.ID))
                    {
                        var allocation = new IssueTypeRelation() { AssetTypeID = assetType.ID, IssueTypeID = issueType.ID };

                        allocations.Add(allocation);
                    }
                }

                string allocationsSQL = "INSERT INTO IssueTypeRelation (AssetTypeID, IssueTypeID) VALUES (@AssetTypeID, @IssueTypeID)";
                var res = await Company.Database.Connection.ExecuteAsync(allocationsSQL, allocations);

                return await Task.FromResult(successMessageResponse(HttpStatusCode.OK, ApiMessages.Success, allocations?.Count == 1 ? ActionApiMessages.AddSingleAllocationSuccessful : ActionApiMessages.AddAllocationsSuccessful)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage));
            }
        }

        /// <summary>
        /// Gets allocations for a workflow action type
        /// </summary>
        /// <param name="actionTypeUid">Uid of the action type</param>
        [
            Route("allocations/{actionTypeUid:Guid}"),
            HttpGet,
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "List of allocations.", typeof(List<IssueTypeAllocationsResponse>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Uid provided is not valid.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetActionTypeAllocations(Guid actionTypeUid)
        {
            try
            {
                if (actionTypeUid == null || actionTypeUid == Guid.Empty)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ActionApiMessages.InvalidActionTypeUid)).ConfigureAwait(false);                    
                }

                var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == actionTypeUid);

                if (issueType == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ActionApiMessages.InvalidActionTypeUid));                   
                }

                string allocationsSQL = @"
                                        SELECT 
                                            T.Uid as AssetTypeUid, 
		                                    T.Name, 
		                                    T.[Class], 
		                                    P.Path,
                                            Res.Value as ResponsibilitiesJson
                                        FROM 
                                            IssueTypeRelation R
                                            INNER JOIN AssetType T ON T.ID = R.AssetTypeID
                                            CROSS APPLY dbo.GetAssetTypeTextPathById(T.ID, ' / ') P
                                            OUTER APPLY (select [value] = (
                                                    select
			                                            rt.Name, rt.Uid
	                                                from 
		                                                IssueTypeRelationResponsibility ITRR 
                                                        INNER JOIN
                                                        ResponsibilityType rt on RT.ID = ITRR.ResponsibilityTypeId 
	                                                where 
		                                                ITRR.IssueTypeRelationID = R.ID
                                                    For Json Path   
                                                )
                                            ) Res
                                        WHERE 
                                            R.IssueTypeID = @issueTypeID";

                var allocations = await Company.QueryAsync<IssueTypeAllocationsResponse>(allocationsSQL, new { issueTypeID = issueType.ID });                

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, allocations))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }    

        /// <summary>
        /// Delete an allocation from a workflow action type
        /// </summary>
        /// <param name="actionTypeUid">Uid of the action type the allocation is to be deleted from</param>
        /// <param name="assetTypeUid">Uid of the asset type of the allocation to be deleted</param>
        [
            Route("allocations/{actionTypeUid:Guid}/{assetTypeUid:Guid}"),
            HttpDelete,
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Allocation Deleted Successfully.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Uid(s) provided are not valid.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteActionTypeAllocations(Guid actionTypeUid, Guid assetTypeUid)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage));

                if (actionTypeUid == null || actionTypeUid == Guid.Empty)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ActionApiMessages.InvalidActionTypeUid));
                }

                if (assetTypeUid == null || assetTypeUid == Guid.Empty)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ActionApiMessages.AssetTypeUidIsNotValid)).ConfigureAwait(false);
                }

                var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == actionTypeUid);

                if (issueType == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ActionApiMessages.InvalidActionTypeUid));
                }

                var assetType = Company.AssetTypes.FirstOrDefault(i => i.uid == assetTypeUid);

                if (assetType == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetTypeUidIsNotValid, assetTypeUid)));
                }                

                string allocationsSQL = @"DELETE FROM IssueTypeRelation WHERE AssetTypeID = @AssetTypeID and IssueTypeID = @IssueTypeID";
                var res = await Company.Database.Connection.ExecuteAsync(allocationsSQL, new { AssetTypeID = assetType.ID, IssueTypeID = issueType.ID});

                if(res == 0)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.NoMatchingAllocation, assetType.Name, issueType.Name))).ConfigureAwait(false);
                }

                return await Task.FromResult(successMessageResponse(HttpStatusCode.OK, ApiMessages.Success, ActionApiMessages.DeleteAllocationSuccessful)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage));
            }
        }

        /// <summary>
        /// Adds an allocation to a workflow action type with optional responsibilities
        /// </summary>
        /// <param name="actionTypeUid">Uid of the action type the allocations are to be added to</param>        
        [
            Route("allocation/{actionTypeUid:Guid}"),
            HttpPost,
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Allocations Added Successfully.", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Uid(s) provided are not valid.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> AddActionTypeAllocationWithResponsibility(Guid actionTypeUid, IssueTypeAllocationRequest model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage));

                if (actionTypeUid == null || actionTypeUid == Guid.Empty)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ActionApiMessages.InvalidActionTypeUid));
                }

                var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == actionTypeUid);

                if (issueType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ActionApiMessages.InvalidActionTypeUid));

                List<IssueTypeRelation> allocations = new List<IssueTypeRelation>();

                if (model.assetTypeUid == null || model.assetTypeUid == Guid.Empty)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ActionApiMessages.AssetTypeUidIsNotValid, model.assetTypeUid)));
                }

                var assetType = Company.AssetTypes.FirstOrDefault(i => i.uid == model.assetTypeUid);

                if (assetType == null || assetType.Class == AssetTypeClass.Diagram || assetType.Class == AssetTypeClass.Reference)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ActionApiMessages.AssetTypeUidIsNotValid, model.assetTypeUid)));
                }

                if (Company.IssueTypeRelations.Any(itr => itr.AssetTypeID == assetType.ID && itr.IssueTypeID == issueType.ID))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest,ActionApiMessages.UniqueAllocation)).ConfigureAwait(false);
                }                

                if (model.responsibilityTypeUid.Count() > 0)
                {
                    IEnumerable<ResponsibilityTypeViewModel> responsibilityTypes = await responsibilityRepository.GetResponsibilityTypesByAssetUid(model.assetTypeUid);

                    foreach(var uid in model.responsibilityTypeUid)
                    {
                        if(!responsibilityTypes.Any(rt => rt.uid == uid))
                        {
                            return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ActionApiMessages.InvalidReponsibilityTypeUid, uid.ToString(), assetType.Name))).ConfigureAwait(false);
                        }
                    }
                }

                string allocationSQL = $@"INSERT INTO IssueTypeRelation (AssetTypeID, IssueTypeID) 
                                            OUTPUT INSERTED.ID
                                            VALUES (@assetTypeID, @issueTypeID)";

                var allocationId = await Company.Database.Connection.QueryFirstAsync<int>(allocationSQL, new { assetTypeID = assetType.ID, issueTypeID = issueType.ID });

                foreach(var rUid in model.responsibilityTypeUid)
                {
                    var temp = new { allocationId, rUid };
                    string allocationResponsibilitySQL = $@"INSERT INTO IssueTypeRelationResponsibility (IssueTypeRelationID, ResponsibilityTypeId) 
                                                            SELECT @allocationId, ID FROM ResponsibilityType where Uid = @responsibilityTypeUid";

                    var res = await Company.Database.Connection.ExecuteAsync(allocationResponsibilitySQL, new { allocationId, responsibilityTypeUid = rUid });
                }

                return await Task.FromResult(successMessageResponse(HttpStatusCode.OK, ApiMessages.Success, ActionApiMessages.AddSingleAllocationSuccessful));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage));
            }
        }
    }    
}