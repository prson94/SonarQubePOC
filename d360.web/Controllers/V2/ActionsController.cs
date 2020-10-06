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
using System.Data;
using Resources;
using d360.core.enums;

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

            var count = await Company.QueryAsync<int>(countSql, dbArgs, ApiTimeout);
            var results = await Company.QueryAsync<dynamic>(resultsSql, dbArgs, ApiTimeout);
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
            SwaggerResponse(HttpStatusCode.BadRequest, "Request Parameters are invalid.", typeof(List<IssueTypeApiModel>)),
            SwaggerResponse(HttpStatusCode.NotFound, "No matching uid for the Action Type/Asset Type/Asset Uid Provided.", typeof(List<IssueTypeApiModel>)),
            SwaggerParameter("_actionTypeUid", "Filter by provided action type Uid.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetTypeUid", "Filter by provided asset type Uid.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetUid", "Filter by provided asset Uid.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_name", "Filter by provided name value.", DataType = "string", ParameterType = "query", Required = false),
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
                        return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Action Type Uid provided does not exist."));
                    }
                }
                else
                {
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Action Type Uid provided is invalid."));
                }
            }

            var assetTypeUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_assettypeuid");

            if (assetTypeUidParam.Key != null && assetTypeUidParam.Value != null && !string.IsNullOrWhiteSpace(assetTypeUidParam.Value))
            {
                if (Guid.TryParse(assetTypeUidParam.Value.Trim(), out Guid assetTypeUid))
                {
                    var validUid = Company.AssetTypes.Any(i => i.uid == assetTypeUid);

                    if (!validUid)
                    {
                        return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Asset Type Uid provided does not exist."));
                    }
                }
                else
                {
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Asset Type Uid provided is invalid."));
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
                        return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Asset Uid provided does not exist."));
                    }

                    if (assetTypeUidParam.Key != null && assetTypeUidParam.Value != null && !string.IsNullOrWhiteSpace(assetTypeUidParam.Value))
                    {
                        var assetTypeuUid = Guid.Parse(assetTypeUidParam.Value);
                        if (!Company.AssetTypes.Any(i => i.uid == assetTypeuUid && i.ID == asset.AssetTypeID))
                        {
                            return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Asset Uid does not match the Asset Type provided."));
                        }
                    }

                }
                else
                {
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Asset Type Uid provided is invalid."));
                }
            }

            var nameParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_name");

            if (nameParam.Key != null)
            {
                if (string.IsNullOrEmpty(nameParam.Value.Trim()))
                {
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Empty string provided for Name. Cannot be empty."));
                }

                if (nameParam.Value.Trim().Length > 250)
                    return await Task.FromResult(Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Name provided must be less then 250 characters in length."));
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

                var allocations = await this.issueRepository.GetAllocationByAssetType(AssetTypeUid);
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
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, "Forbidden", $"Forbidden user is not an administrator."));

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


        /// <summary>
        /// Updates a workflow action type
        /// </summary>
        /// <param name="model">The information of the workflow action type to be updated</param>
        [
            Route("type"),
            HttpPut,
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Workflow Action Type successfully Updated.", typeof(AddIssueTypeApiModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "User is not an administrator.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> UpdateWorkflowActionType(AddWorkFlowAction model)
        {
            var prefix = "Issues.AddWorkflowActionType => ";
            AddIssueTypeApiModel result = new AddIssueTypeApiModel();
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, "Forbidden", $"Forbidden user is not an administrator."));

                if (model.Uid == null || model.Uid == Guid.Empty)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"A valid Uid is required."));
                }

                var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == model.Uid);

                if (issueType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"The Uid provided is invalid."));


                if (model.Name == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"Name is a required field."));
                }

                if (string.IsNullOrEmpty(model.Name.Trim()))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"Empty string provided for Name. Cannot be empty."));

                if (model.Name.Trim().Length > 250)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"Name provided must be less then 250 characters in length."));

                var validName = Company.IssueTypes.Any(i => i.Name.ToLower() == model.Name.Trim().ToLower() && i.uid != model.Uid);

                if (validName)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"Name must be unique. Workflow action already exists with this name"));

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

        /// <summary>
        /// Deletes a workflow action type
        /// </summary>
        /// <param name="actionTypeUid">Uid of the action type to be deleted</param>
        [
            Route("type/{actionTypeUid:Guid}"),
            HttpDelete,
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Action Type was deleted.", typeof(AddIssueTypeApiModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteWorkflowActionType(Guid actionTypeUid, DeleteIssueTypeAPIModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, "Forbidden", $"Forbidden user is not an administrator."));

            if (actionTypeUid == null || actionTypeUid == Guid.Empty)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"A valid actionTypeUid must be provided."));
            }

            var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == actionTypeUid);

            if (issueType == null)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"No Action Type found matching the Uid Provided."));

            if (!model.cascade && (Company.Issues.Any(x => x.IssueTypeID == issueType.ID) || Company.IssueTypeRelations.Any(x => x.IssueTypeID == issueType.ID)))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad Request", $"Action Type has associated actions / allocations. Enable on cascade request to delete."));
            }

            var deleteSQL = $@" DELETE FROM IssueTypeRelation Where IssueTypeID = @issueTypeId
                                
                                DELETE FROM Issue Where IssueTypeID = @issueTypeId
                                
                                DELETE FROM IssueType Where uid = @uid";

            var res = await Company.Database.Connection.ExecuteAsync(deleteSQL,
                new { uid = actionTypeUid, issueTypeId = issueType.ID });

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new AddIssueTypeApiModel() { Uid = actionTypeUid, Message = "Action Type was deleted", Success = true })));
        }

        /// <summary>
        /// Create an action
        /// </summary>        
        /// <param name="actionTypeUid">The Uid of the action type</param>
        /// <param name="models">Collection of Issues/Actions</param>
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
        public async Task<IHttpActionResult> CreateAction(Guid actionTypeUid, List<ActionUpsertRequest> models)
        {
            bool isWriteActionDescriptionEnabled = IsWriteActionDescriptionEnabled();

            List<ApiStatusResponse> response = new List<ApiStatusResponse>();

            List<IssueInsertAPIModel> issueModels = new List<IssueInsertAPIModel>();

            if (actionTypeUid == null || actionTypeUid == Guid.Empty)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, "Invalid ActionTypeUid provided."));
            }

            var issueType = Company.Filter<IssueType>(i => i.uid == actionTypeUid).SingleOrDefault();

            if (issueType == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Not Found", $"Action Type with Uid {actionTypeUid} could not be found."));
            }

            WorkHttpStatus validationStatus = PopulateRequest(models, ref issueModels, issueType);
            if (validationStatus.StatusCode != HttpStatusCode.OK)
            {
                return await Task.FromResult(errorMessageResponse(validationStatus.StatusCode, validationStatus.Error, validationStatus.Message));
            }

            foreach (var issueModel in issueModels)
            {

                if (isWriteActionDescriptionEnabled)
                {
                    var relations = new List<CommentRelation>();
                    var comment = new Comment();

                    relations.Add(new CommentRelation { ObjectID = Company.CurrentResourceID, ObjectType = SystemObjects.Resource.ToString(), Date = DateTime.UtcNow });

                    comment.OwnerObjectType = SystemObjects.Resource.ToString();
                    comment.OwnerObjectID = Company.CurrentResourceID;
                    comment.CommentTypeID = CommentType.Issue;
                    comment.Body = issueModel.Comment ?? $"New {issueType.Name} Raised.";

                    //add relation to current artifact
                    relations.Add(new CommentRelation { ObjectType = issueModel.Issue.Object, ObjectID = issueModel.Issue.ObjectID, Date = DateTime.UtcNow });

                    var dtl = Company.AddComment(comment, relations).FirstOrDefault(i => i.ID == comment.ID);

                    issueModel.Issue.CommentID = dtl.ID;
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

                response.Add(new ApiStatusResponse { Uid = issueModel.Issue.UID.Value, Message = "Action Created", Success = true });
            }

            Company.CreateEventsForAddedActions(issueModels.Select(x => x.Issue).ToList());

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response));
        }

        private WorkHttpStatus ValidateRequest(IssueType issueType, ActionUpsertRequest model, out Asset asset, out AssetType assetType)
        {
            asset = null;
            assetType = null;
            int assetTypeID = 0;
            string assetTypeName = "";

            if ((model.AssetTypeUid == null && model.AssetUid == null) || (model.AssetTypeUid != null && model.AssetUid != null))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.AssetTypeOrAssetRequired);
            }            

            if(model.AssetUid != null)
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

                if (!Company.HasAssetDefaultReadPermission(asset.Object, asset.ObjectID, Permission.ReadAsset))
                {
                    return new WorkHttpStatus(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, ActionApiMessages.AssetAddActionPermissionsDenied);
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
                    
                if (!Company.HasAssetTypePermission(assetType.Object, assetType.ObjectID, Permission.ReadAsset))
                {
                    return new WorkHttpStatus(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, ActionApiMessages.AssetTypeAddActionPermissionsDenied);
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

            Company.ValidateFields(SystemObjects.IssueType.ToString(), issueType.ID, true, fieldTypes.ToList(), fieldTypes.Where(f => f.IsRequired && string.IsNullOrEmpty(f.DefaultValue)).Select(f => f.Name).ToList(), model.Fields, Guid.Empty, 1, fieldTable, out bool success, out string errorMessage);

            if (!success)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, errorMessage);
            }

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
                    var fieldLookupValues = Company.Database.Connection.Query<FieldLookupValue>(lookupSQL, new { fieldTypeId = ft.ID, lookupValues});

                    List<string> fieldValues = new List<string>();
                    foreach (var lookupValue in lookupValues)
                    {
                        if (!fieldLookupValues.Any(x => x.Value.ToString() == lookupValue))
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
                        else
                        {
                            fieldValues.Add(lookupValue);
                        }
                    }
                    
                    model.Fields[ft.Name] = string.Join(",", fieldValues.Distinct());
                }
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        private WorkHttpStatus PopulateRequest(List<ActionUpsertRequest> models, ref List<IssueInsertAPIModel> issues, IssueType issueType)
        {
            foreach (var model in models)
            {                           
                var validationStatus = ValidateRequest(issueType, model, out Asset asset, out AssetType assetType);

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
                else if(assetType != null){
                    issue.Object = assetType.Object;
                    issue.ObjectID = assetType.ObjectID;
                    issue.ObjectType = assetType.Object;
                    issue.ObjectTypeID = assetType.ObjectID;
                }                

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Issue, issue.ID, Company.GetFieldTypesByObject(SystemObjects.IssueType, issueType.ID).ToList(), model.Fields, null);

                issues.Add(new IssueInsertAPIModel { Issue = issue, fields = fields, Comment = model.Fields.ContainsKey("ProblemDesc") ? model.Fields["ProblemDesc"] : null });
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        private bool IsWriteActionDescriptionEnabled()
        {
            var setting = Community.Filter<CompanySetting>(i => i.CompanyID == Company.CurrentCompanyID && i.SettingID == 61).SingleOrDefault();
            if (setting == null)
                return true;
            else
                return bool.Parse(setting.Value);

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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Uid(s) provided are not valid.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> AddActionTypeAllocations(Guid actionTypeUid, List<string> assetTypeUids)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage));

                if (actionTypeUid == null || actionTypeUid == Guid.Empty)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ActionApiMessages.InvalidActionTypeUid));
                }

                var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == actionTypeUid);

                if (issueType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ActionApiMessages.InvalidActionTypeUid));

                if (assetTypeUids.Count == 0)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.EmptyAllocationRequest));
                }

                List<IssueTypeRelation> allocations = new List<IssueTypeRelation>();

                foreach (var assetTypeUid in assetTypeUids)
                {
                    if (!Guid.TryParse(assetTypeUid, out Guid uid))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetTypeUidIsNotValid, assetTypeUid)));
                    }

                    var assetType = Company.AssetTypes.FirstOrDefault(i => i.uid == uid);

                    if (assetType == null)
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

                return await Task.FromResult(successMessageResponse(HttpStatusCode.OK, ApiMessages.Success, ActionApiMessages.AddAllocationsSuccessful));
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Uid provided is not valid.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetActionTypeAllocations(Guid actionTypeUid)
        {
            try
            {
                if (actionTypeUid == null || actionTypeUid == Guid.Empty)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ActionApiMessages.InvalidActionTypeUid));                    
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
		                                    P.Path
                                        FROM 
                                            IssueTypeRelation R
                                            INNER JOIN AssetType T ON T.ID = R.AssetTypeID
                                            CROSS APPLY dbo.GetAssetTypeTextPathById(T.ID, ' / ') P
                                        WHERE 
                                            R.IssueTypeID = @issueTypeID";

                var allocations = await Company.QueryAsync<IssueTypeAllocationsResponse>(allocationsSQL, new { issueTypeID = issueType.ID });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, allocations)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage));
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse)),
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
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ActionApiMessages.AssetTypeUidIsNotValid));
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
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.NoMatchingAllocation, assetType.Name, issueType.Name)));
                }

                return await Task.FromResult(successMessageResponse(HttpStatusCode.OK, ApiMessages.Success, ActionApiMessages.DeleteAllocationSuccessful));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage));
            }
        }
    }    
}