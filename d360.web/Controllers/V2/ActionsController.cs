using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Services;

using Dapper;

using Microsoft.Web.Http;

using Newtonsoft.Json;

using Resources;

using Swashbuckle.Swagger.Annotations;

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
		private readonly IAssetRepository assetRepository;
		private readonly ICommentRepository commentRepository;
		private readonly IIssueRepository issueRepository;
		private readonly IResponsibilityRepository responsibilityRepository;

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
		   SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json"),
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
				"left join AssetDetail A on A.ID = I.AssetID",
				"left join [reporting].[Global_Resource] CR on CR.ResourceID = I.CreatedBy",
				"left join [reporting].[Global_Resource] UR on UR.ResourceID = I.UpdatedBy"
			};

			DynamicParameters dbArgs = new DynamicParameters();
			ResourceApiViewModel model = new ResourceApiViewModel();
			bool isOrderByFieldValid = false;

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
				throw new ArgumentException(isValid);
			}

			long.TryParse(_pageSize, out long pageSize);
			long.TryParse(_pageNum, out long pageNum);

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
					throw new ArgumentException(ApiMessages.InvalidDirection);
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
					IssueType issueType = issueRepository.GetIssueTypeByUID(atGuid);

					if (issueType == null)
					{
						throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.ActionTypeUidIsNotValid, actionTypeUid));
					}
					else
					{
						queries.Add("IT.[Uid] = @actionTypeUid");
						dbArgs.Add("actionTypeUid", actionTypeUid);

						var fieldTypes = Company.Filter<FieldType>(f => f.IssueTypeID == issueType.ID).ToList();
						getFieldSql(fieldTypes, dbArgs, fieldJoins, selectColumns, "Issue", "I.ID");

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
					throw new NotFoundBusinessLayerException(string.Format(ApiMessages.InvalidGuid, actionTypeUid));
				}
			}

			if (!isOrderByFieldValid)
			{
				throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.OrderByFieldNotFoundMessage, _order));
			}

			if (!string.IsNullOrEmpty(assetUid) && !string.IsNullOrWhiteSpace(assetUid))
			{
				if (Guid.TryParse(assetUid, out Guid aGuid))
				{
					Asset asset = assetRepository.GetAssetByUID(aGuid);

					if (asset == null)
					{
						throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.AssetUidIsNotValid, assetUid));
					}

					queries.Add("A.[Uid] = @assetUid");
					dbArgs.Add("assetUid", assetUid);
				}
				else
				{
					throw new NotFoundBusinessLayerException(string.Format(ApiMessages.InvalidGuid, assetUid));
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

			return Ok(model);
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
			var sql = @"
							select	I.uid as 'uid', 
									A.uid as 'asset.uid',
									coalesce(A.icon, 'fa-book') as 'asset.icon',
									A.TypeName + ' > ' + A.DisplayValue as 'asset.displayValue',
									IT.name as 'action.name', 
									reporting.StripHTML(F.FormattedValue) as 'action.description'
							from	AssetDetail A
									inner join @uids U on U.Uid = A.Uid
									inner join Issue I on I.AssetId = A.Id
									left join IssueType IT on IT.ID = I.IssueTypeID
									left join FieldType FT on FT.IssueTypeID = IT.ID and (FT.Name = 'Description' or FT.Name = 'ProblemDesc')
									left join Field F on F.FieldTypeID = FT.ID and F.IssueID = I.ID
							where	I.CompletedOn is null
									and exists (select 1 from workflow.Item where Object = 'Issue' and ObjectID = I.ID)
							for json path";

			if (model == null)
			{
				throw new ArgumentException(ApiMessages.EmptyInvalidParameterSet);
			}

			if (model.assets.Count == 0)
			{
				AssetBrowserAlert[] alerts = new AssetBrowserAlert[0];

				return ResponseMessage(Request.CreateResponse(HttpStatusCode.NoContent, alerts));
			}

			var reader = await Company.QueryAsync<string>(sql, new
				{
					uids = model.assets.Select(i => i.uid).Distinct().AsTableValuedParameter(
						"dbo.UidTable",
						new List<string>() { "Uid" }
						)
				}, timeout: 100);
			var json = string.Join("", reader);

			var returnModel = JsonConvert.DeserializeObject<AssetBrowserAlert[]>(json);

			return Ok(returnModel);
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
		public async Task<IHttpActionResult> GetIssueTypes()
		{
			var queryParams = Request.GetQueryNameValuePairs();

			#region validate Parameters

			var actionTypeUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_actiontypeuid");

			if (actionTypeUidParam.Key != null)
			{
				if (!Guid.TryParse(actionTypeUidParam.Value, out Guid actionTypeUid) || actionTypeUid == Guid.Empty)
				{
					throw new ArgumentException(ActionApiMessages.InvalidActionTypeUid);
				}

				var validUid = Company.IssueTypes.Any(i => i.uid == actionTypeUid);
				if (!validUid)
				{
					throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.ActionTypeUidIsNotValid, actionTypeUid.ToString()));
				}
			}

			var assetTypeUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_assettypeuid");

			if (assetTypeUidParam.Key != null && assetTypeUidParam.Value != null && !string.IsNullOrWhiteSpace(assetTypeUidParam.Value))
			{
				if (!Guid.TryParse(assetTypeUidParam.Value.Trim(), out Guid assetTypeUid) || assetTypeUid == Guid.Empty)
				{
					throw new ArgumentException(ActionApiMessages.InvalidAssetTypeUid);
				}

				var assetType = Company.AssetTypes.FirstOrDefault(i => i.uid == assetTypeUid);
				if (assetType == null)
				{
					throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString()));
				}
				else if (assetType.Class == AssetTypeClass.Diagram)
				{
					throw new ArgumentException(ActionApiMessages.InvalidAssetTypeUid);
				}
			}

			var assetUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_assetuid");

			if (assetUidParam.Key != null && assetUidParam.Value != null && !string.IsNullOrWhiteSpace(assetUidParam.Value))
			{
				if (!Guid.TryParse(assetUidParam.Value.Trim(), out Guid assetUid) || assetUid == Guid.Empty)
				{
					throw new ArgumentException(ActionApiMessages.InvalidAssetUid);
				}

				var asset = Company.Assets.FirstOrDefault(i => i.uid == assetUid);
				if (asset == null)
				{
					throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.AssetNotFound, assetUid.ToString()));
				}
				else if (asset.AssetType.Class == AssetTypeClass.Diagram)
				{
					throw new ArgumentException(ActionApiMessages.InvalidAssetUid);
				}

				if (assetTypeUidParam.Key != null && assetTypeUidParam.Value != null && !string.IsNullOrWhiteSpace(assetTypeUidParam.Value))
					{
					var assetTypeuUid = Guid.Parse(assetTypeUidParam.Value);

					if (!Company.AssetTypes.Any(i => i.uid == assetTypeuUid && i.ID == asset.AssetTypeID))
					{
						throw new ArgumentException(ApiMessages.AssetValidateWithAssetType);
					}
				}
			}

			var nameParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_name");

			if (nameParam.Key != null)
			{
				if (string.IsNullOrEmpty(nameParam.Value.Trim()))
				{
					throw new ArgumentException(ActionApiMessages.NameNotEmptyAndRequired);
				}

				if (nameParam.Value.Trim().Length > 250)
				{
					throw new ArgumentException(ActionApiMessages.NameMaxLength250Char);
				}
			}

			var resourceUidParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_resourceuid");

			if (resourceUidParam.Key != null && !string.IsNullOrWhiteSpace(resourceUidParam.Value))
			{
				if (Guid.TryParse(resourceUidParam.Value, out Guid resourceUid) || resourceUid == Guid.Empty)
				{
					throw new ArgumentException(ActionApiMessages.ResourceUidNotValid);
				}

				var validUid = Company.GlobalReportingResources.Any(r => r.Uid == resourceUid);
				if (!validUid)
				{
					throw new NotFoundBusinessLayerException(ActionApiMessages.ResourceUidNotFound);
				}
			}

			var limitToActiveWorkflowsParam = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_limittoactiveworkflows");

			if (limitToActiveWorkflowsParam.Key != null && !string.IsNullOrWhiteSpace(limitToActiveWorkflowsParam.Value))
			{
				if (!bool.TryParse(limitToActiveWorkflowsParam.Value, out _))
				{
					throw new ArgumentException(ActionApiMessages.InvalidLimitActiveWorkflow);
				}
			}

			#endregion

			var issueTypes = await issueRepository.GetIssueTypes(queryParams);

			return Ok(issueTypes);
		}

		/// <summary>
		/// Returns actions types that are associated with a particular asset type
		/// </summary>
		/// <param name="AssetTypeUid">Asset Type Uid</param>
		/// <returns>A list of actions types</returns>
		[
			HttpGet,
			Route("types/{AssetTypeUid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "", typeof(IssueTypeApiModel)),
			SwaggerResponse(HttpStatusCode.NotFound, "Asset Type with Uid {uid} not found."),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> GetAllocationByAssetTypeAsync(Guid AssetTypeUid)
		{
			AssetType assetType = assetRepository.GetAssetTypeByUID(AssetTypeUid);
			if (assetType == null)
			{
				throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.AssetTypeNotFound, AssetTypeUid.ToString()));
			}

			var allocations = await issueRepository.GetAllocationByAssetType(AssetTypeUid);

			return Ok(allocations);
		}

		/// <summary>
		/// Creates a workflow action type
		/// </summary>
		/// <param name="model">The information of the workflow action type to be created</param>
		[
			HttpPost,
			Route("type"),
			RequireAdminPermissions,
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Workflow Action Type successfully created.", typeof(AddIssueTypeApiModel)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> AddWorkflowActionType(AddWorkFlowAction model)
		{
			var prefix = "Issues.AddWorkflowActionType => ";
			AddIssueTypeApiModel result = new AddIssueTypeApiModel();

			if (model.Uid != null)
			{
				var validUid = Company.IssueTypes.Any(i => i.uid == model.Uid);

				if (validUid)
				{
					throw new ArgumentException(ActionApiMessages.UniqueUid);
				}
			}

			if (string.IsNullOrEmpty(model.Name.Trim()))
			{
				throw new ArgumentException(ActionApiMessages.NameNotEmptyAndRequired);
			}

			if (model.Name.Trim().Length > 250)
			{
				throw new ArgumentException(ActionApiMessages.NameMaxLength250Char);
			}

			var validName = Company.IssueTypes.Any(i => i.Name.ToLower() == model.Name.Trim().ToLower());

			if (validName)
			{
				throw new ArgumentException(ActionApiMessages.UniqueNameWorkflowAction);
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
				var issueType = Company.IssueTypes.FirstOrDefault(i => i.Name.ToLower() == model.Name.ToLower());
				Company.Add(new FieldType
				{
					IssueTypeID = issueType.ID,
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

			return Ok(result);
		}

		/// <summary>
		/// Updates a workflow action type
		/// </summary>
		/// <param name="model">The information of the workflow action type to be updated</param>
		[
			Route("type"),
			HttpPut,
			RequireAdminPermissions,
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Workflow Action Type successfully Updated.", typeof(AddIssueTypeApiModel)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "User is not an administrator.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> UpdateWorkflowActionType(AddWorkFlowAction model)
		{
			if (model.Uid == null || model.Uid == Guid.Empty)
			{
				throw new ArgumentException(ActionApiMessages.UidNotEmptyAndRequired);
			}

			var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == model.Uid);

			if (issueType == null)
			{
				throw new ArgumentException(ActionApiMessages.UidNotValid);
			}

			if (model.Name == null)
			{
				throw new ArgumentException(ActionApiMessages.NameNotNull);
			}

			if (string.IsNullOrEmpty(model.Name.Trim()))
			{
				throw new ArgumentException(ActionApiMessages.NameNotEmptyAndRequired);
			}

			if (model.Name.Trim().Length > 250)
			{
				throw new ArgumentException(ActionApiMessages.NameMaxLength250Char);
			}

			var validName = Company.IssueTypes.Any(i => i.Name.ToLower() == model.Name.Trim().ToLower() && i.uid != model.Uid);

			if (validName)
			{
				throw new ArgumentException(ActionApiMessages.UniqueNameWorkflowAction);
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

			AddIssueTypeApiModel result = new AddIssueTypeApiModel()
			{
				Uid = (Guid)model.Uid,
				Message = ActionApiMessages.ActionTypeUpdated,
				Success = true
			};

			return Ok(result);
		}

		/// <summary>
		/// Deletes a workflow action type
		/// </summary>
		/// <param name="actionTypeUid">Uid of the action type to be deleted</param>
		/// <param name="model">Request body containing cascade flag</param>
		[
			Route("type/{actionTypeUid:Guid}"),
			HttpDelete,
			RequireAdminPermissions,
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Action Type was deleted.", typeof(AddIssueTypeApiModel)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Action Type Not Found", typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> DeleteWorkflowActionType(Guid actionTypeUid, DeleteIssueTypeAPIModel model)
		{
			if (actionTypeUid == null || actionTypeUid == Guid.Empty)
			{
				throw new ArgumentException(ActionApiMessages.InvalidActionTypeUid);
			}

			var queryParams = Request.GetQueryNameValuePairs();
			bool IsFromUI = false;

			if (queryParams.Any(p => p.Key.Trim().ToLower() == "_requestfromui"))
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
				throw new NotFoundBusinessLayerException(ActionApiMessages.ActionTypeNotFound);
			}

			if (!model.cascade && Company.Issues.Any(x => x.IssueTypeID == issueType.ID))
			{
				if (IsFromUI)
				{
					throw new ArgumentException(string.Format(ActionApiMessages.ChildRecordExistsIssueType, issueType.Name));
				}
				else
				{
					throw new ArgumentException(ActionApiMessages.CascadeDeleteActionType);
				}
			}

			var deleteSQL = $@" DELETE FROM IssueTypeRelation Where IssueTypeID = @issueTypeId
								
								DELETE FROM Issue Where IssueTypeID = @issueTypeId
								
								DELETE FROM FieldType WHERE IssueTypeID = @issueTypeId;

								DELETE FROM IssueType Where uid = @uid";

			var res = await Company.Database.Connection.ExecuteAsync(deleteSQL,
				new { uid = actionTypeUid, issueTypeId = issueType.ID });

			return Ok(new AddIssueTypeApiModel()
			{
				Uid = actionTypeUid,
				Message = ActionApiMessages.ActionTypeDeleted,
				Success = true
			});
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
			bool isWriteActionDescriptionEnabled = IsWriteActionDescriptionEnabled();

			List<ApiStatusResponse> response = new List<ApiStatusResponse>();

			List<IssueInsertAPIModel> issueModels = new List<IssueInsertAPIModel>();

			if (actionTypeUid == null || actionTypeUid == Guid.Empty)
			{
				throw new ArgumentException(ActionApiMessages.InvalidActionTypeUid);
			}

			var issueType = Company.Filter<IssueType>(i => i.uid == actionTypeUid).SingleOrDefault();

			if (issueType == null)
			{
				throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.ActionTypeUidIsNotValid, actionTypeUid.ToString()));
			}

			WorkHttpStatus validationStatus = PopulateRequest(models, ref issueModels, issueType, lookupFieldsPassedByValue);

			if (validationStatus.StatusCode != HttpStatusCode.OK)
			{
				throw new RestApiException(validationStatus.StatusCode, validationStatus.Error, validationStatus.Message);
			}

			foreach (var issueModel in issueModels)
			{
				if (!Company.CurrentResourceIsAdmin && !Company.HasAssetTypePermission(issueModel.Issue.AssetTypeID, Permission.ReadAsset))
				{
					throw new ForbiddenBusinessLayerException(ActionApiMessages.AssetTypeAddActionPermissionsDenied);
				}

				if (isWriteActionDescriptionEnabled && issueModel.Issue.AssetID != null)
				{
					var comment = new CommentApiPostModel
					{
						AssetUid = issueModel.AssetUid,
						Body = issueModel.Comment ?? string.Format(ActionApiMessages.ActionAssetCommentBody, issueType.Name),
						Tags = new List<Guid> { issueModel.AssetUid }       // Add relation to current artifact
					};
					var dtl = await commentRepository.AddComment(comment, CommentType.Issue);
					issueModel.Issue.CommentID = dtl.ID;
				}

				var insertSQL = $@"INSERT INTO [dbo].[Issue]
												   ([IssueTypeID]
												   ,[AssetID]
												   ,[AssetTypeID]
												   ,[CreatedOn]
												   ,[CreatedBy]
												   ,[UpdatedOn]
												   ,[UpdatedBy]
												   ,[CommentID])
											OUTPUT inserted.Uid, inserted.ID
											   VALUES
												   (@issueTypeID
												   ,@assetID
												   ,@assetTypeID
												   ,GETDATE()
												   ,@userId
												   ,GETDATE()
												   ,@userId
												   ,@commentId)";

				var res = await Company.Database.Connection.QueryAsync<(Guid uid, int id)>(insertSQL, new { issueTypeID = issueType.ID, assetID = issueModel.Issue.AssetID, assetTypeID = issueModel.Issue.AssetTypeID, userId = Company.CurrentResourceID, commentId = issueModel.Issue.CommentID });

				issueModel.Issue.ID = res.FirstOrDefault().id;
				issueModel.Issue.UID = res.FirstOrDefault().uid;

				if (issueModel.fields != null && issueModel.fields.Count > 0)
				{
					issueModel.fields.ForEach(i =>
					{
						i.IssueID = issueModel.Issue.ID;
					});
					Company.AddOrUpdateFields(issueModel.fields);
				}

				response.Add(new ApiStatusResponse { Uid = issueModel.Issue.UID.Value, Message = ActionApiMessages.ActionCreatedMsg, Success = true });
			}

			Company.CreateEventsForAddedActions(issueModels.Select(x => x.Issue).ToList());

			return Ok(response);
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

			var fieldTypes = Company.GetAssetTypeFieldTypesCore(SystemObjects.IssueType.ToString(), issueType.ID);

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
					if (!string.IsNullOrEmpty(model.Fields[type.Name + "_Name"]) || !string.IsNullOrEmpty(model.Fields[type.Name + "_Url"]))
					{
						model.Fields.Add(type.Name, $"{model.Fields[type.Name + "_Name"]}|{model.Fields[type.Name + "_Url"]}");
					}

					model.Fields.Remove(type.Name + "_Name");
					model.Fields.Remove(type.Name + "_Url");
				}
			}

			Company.ValidateFields(SystemObjects.IssueType.ToString(), issueType.ID, true, fieldTypes.ToList(), fieldTypes.Where(f => f.IsRequired && !f.HasDefaultValue && f.Type != DataType.Counter.ToString()).Select(f => f.Name).ToList(), model.Fields, Guid.Empty, 1, fieldTable, out bool success, out string errorMessage);

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
									text in @lookupValues";

					string[] lookupValues = { };

					if (model.Fields.ContainsKey(ft.Name))
					{
						lookupValues = ft.AllowMultipleValues ? model.Fields[ft.Name].Split(',').ToList().Select(v => v.Trim()).ToArray() : new[] { model.Fields[ft.Name].Trim() };
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

				if (assetType != null)
				{
					issue.AssetTypeID = assetType.ID;
				}
				else if (asset != null)
				{
					issue.AssetID = asset.ID;
					issue.AssetTypeID = asset.AssetTypeID;
				}

				var fields = PopulateActionFields(issueType.ID, issue.ID, model.Fields);

				issues.Add(new IssueInsertAPIModel
				{
					Issue = issue,
					fields = fields,
					Comment = model.Fields.ContainsKey("ProblemDesc") ? model.Fields["ProblemDesc"] : null,
					AssetUid = (asset != null) ? asset.uid : Guid.Empty
				});
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
								DateTime dateVal;
								//throw out any time piece sent in
								if (DateTime.TryParse(stringDate, out dateVal))
								{
									value = dateVal.ToShortDateString();
								}
								break;
							case "DateTime":
								var stringDateTime = fields[ft.Name];
								DateTime dateTimeVal;
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
							fieldList.Add(new Field { FieldTypeID = ft.ID, IssueID = issueId, Value = value, FormattedValue = value });
						}
					}
				}
			}

			return fieldList;
		}

		private bool IsWriteActionDescriptionEnabled()
		{
			var setting = SettingsRepository.GetSettings().Single(s => s.ID == Setting.WriteActionDescription);

			return setting.Value == "true";
		}

		/// <summary>
		/// Adds allocations to a workflow action type
		/// </summary>
		/// <param name="actionTypeUid">Uid of the action type the allocations are to be added to</param>
		/// <param name="assetTypeUids">Collection of asset type Uids to be added</param>
		[
			Route("allocations/{actionTypeUid:Guid}"),
			HttpPost,
			RequireAdminPermissions,
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Allocations Added Successfully.", typeof(ApiStatusResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Uid(s) provided are not valid.", typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> AddActionTypeAllocations(Guid actionTypeUid, List<string> assetTypeUids)
		{
			if (actionTypeUid == null || actionTypeUid == Guid.Empty)
			{
				throw new ArgumentException(ActionApiMessages.InvalidActionTypeUid);
			}

			var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == actionTypeUid);

			if (issueType == null)
			{
				throw new NotFoundBusinessLayerException(ActionApiMessages.InvalidActionTypeUid);
			}

			if (assetTypeUids.Count == 0)
			{
				throw new ArgumentException(ActionApiMessages.EmptyAllocationRequest);
			}

			List<IssueTypeRelation> allocations = new List<IssueTypeRelation>();

			foreach (var assetTypeUid in assetTypeUids.Distinct())
			{
				if (!Guid.TryParse(assetTypeUid, out Guid uid))
				{
					throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.AssetTypeUidIsNotValid, assetTypeUid));
				}

				var assetType = Company.AssetTypes.FirstOrDefault(i => i.uid == uid);

				if (assetType == null || assetType.Class == AssetTypeClass.Diagram || assetType.Class == AssetTypeClass.Reference)
				{
					throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.AssetTypeUidIsNotValid, assetTypeUid));
				}

				if (!Company.IssueTypeRelations.Any(itr => itr.AssetTypeID == assetType.ID && itr.IssueTypeID == issueType.ID))
				{
					var allocation = new IssueTypeRelation() { AssetTypeID = assetType.ID, IssueTypeID = issueType.ID };

					allocations.Add(allocation);
				}
			}

			string allocationsSQL = "INSERT INTO IssueTypeRelation (AssetTypeID, IssueTypeID) VALUES (@AssetTypeID, @IssueTypeID)";
			var res = await Company.Database.Connection.ExecuteAsync(allocationsSQL, allocations);

			var resultMessage = allocations?.Count == 1 ? ActionApiMessages.AddSingleAllocationSuccessful : ActionApiMessages.AddAllocationsSuccessful;

			return successMessageResponse(HttpStatusCode.OK, ApiMessages.Success, resultMessage);
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
			SwaggerResponse(HttpStatusCode.NotFound, "Uid provided is not valid.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> GetActionTypeAllocations(Guid actionTypeUid)
		{
			if (actionTypeUid == null || actionTypeUid == Guid.Empty)
			{
				throw new ArgumentException(ActionApiMessages.InvalidActionTypeUid);
			}

			var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == actionTypeUid);

			if (issueType == null)
			{
				throw new NotFoundBusinessLayerException(ActionApiMessages.InvalidActionTypeUid);
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

			return Ok(allocations);
		}

		/// <summary>
		/// Delete an allocation from a workflow action type
		/// </summary>
		/// <param name="actionTypeUid">Uid of the action type the allocation is to be deleted from</param>
		/// <param name="assetTypeUid">Uid of the asset type of the allocation to be deleted</param>
		[
			Route("allocations/{actionTypeUid:Guid}/{assetTypeUid:Guid}"),
			HttpDelete,
			RequireAdminPermissions,
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Allocation Deleted Successfully.", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Uid(s) provided are not valid.", typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> DeleteActionTypeAllocations(Guid actionTypeUid, Guid assetTypeUid)
		{
			if (actionTypeUid == null || actionTypeUid == Guid.Empty)
			{
				throw new ArgumentException(ActionApiMessages.InvalidActionTypeUid);
			}

			if (assetTypeUid == null || assetTypeUid == Guid.Empty)
			{
				throw new ArgumentException(string.Format(ActionApiMessages.AssetTypeUidIsNotValid, assetTypeUid));
			}

			var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == actionTypeUid);

			if (issueType == null)
			{
				throw new NotFoundBusinessLayerException(ActionApiMessages.InvalidActionTypeUid);
			}

			var assetType = Company.AssetTypes.FirstOrDefault(i => i.uid == assetTypeUid);

			if (assetType == null)
			{
				throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.AssetTypeUidIsNotValid, assetTypeUid));
			}

			string allocationsSQL = @"DELETE FROM IssueTypeRelation WHERE AssetTypeID = @AssetTypeID and IssueTypeID = @IssueTypeID";
			var res = await Company.Database.Connection.ExecuteAsync(allocationsSQL, new { AssetTypeID = assetType.ID, IssueTypeID = issueType.ID });

			if (res == 0)
			{
				throw new NotFoundBusinessLayerException(string.Format(ActionApiMessages.NoMatchingAllocation, assetType.Name, issueType.Name));
			}

			return successMessageResponse(HttpStatusCode.OK, ApiMessages.Success, ActionApiMessages.DeleteAllocationSuccessful);
		}

		/// <summary>
		/// Adds an allocation to a workflow action type with optional responsibilities
		/// </summary>
		/// <param name="actionTypeUid">Uid of the action type the allocations are to be added to</param>        
		[
			Route("allocation/{actionTypeUid:Guid}"),
			HttpPost,
			RequireAdminPermissions,
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Allocations Added Successfully.", typeof(ApiStatusResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Uid(s) provided are not valid.", typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> AddActionTypeAllocationWithResponsibility(Guid actionTypeUid, IssueTypeAllocationRequest model)
		{
			if (actionTypeUid == null || actionTypeUid == Guid.Empty)
			{
				throw new ArgumentException(ActionApiMessages.InvalidActionTypeUid);
			}

			var issueType = Company.IssueTypes.FirstOrDefault(i => i.uid == actionTypeUid);

			if (issueType == null)
			{
				throw new NotFoundBusinessLayerException(ActionApiMessages.ActionTypeNotFound);
			}

			List<IssueTypeRelation> allocations = new List<IssueTypeRelation>();

			if (model.assetTypeUid == null || model.assetTypeUid == Guid.Empty)
			{
				throw new ArgumentException(string.Format(ActionApiMessages.AssetTypeUidIsNotValid, model.assetTypeUid));
			}

			var assetType = Company.AssetTypes.FirstOrDefault(i => i.uid == model.assetTypeUid);

			if (assetType == null || assetType.Class == AssetTypeClass.Diagram || assetType.Class == AssetTypeClass.Reference)
			{
				throw new ArgumentException(string.Format(ActionApiMessages.AssetTypeUidIsNotValid, model.assetTypeUid));
			}

			if (Company.IssueTypeRelations.Any(itr => itr.AssetTypeID == assetType.ID && itr.IssueTypeID == issueType.ID))
			{
				throw new ArgumentException(ActionApiMessages.UniqueAllocation);
			}

			if (model.responsibilityTypeUid.Count() > 0)
			{
				IEnumerable<ResponsibilityTypeViewModel> responsibilityTypes = await responsibilityRepository.GetResponsibilityTypesByAssetUid(model.assetTypeUid);

				foreach (var uid in model.responsibilityTypeUid)
				{
					if (!responsibilityTypes.Any(rt => rt.uid == uid))
					{
						throw new ArgumentException(string.Format(ActionApiMessages.InvalidReponsibilityTypeUid, uid.ToString(), assetType.Name));
					}
				}
			}

			string allocationSQL = $@"INSERT INTO IssueTypeRelation (AssetTypeID, IssueTypeID) 
											OUTPUT INSERTED.ID
											VALUES (@assetTypeID, @issueTypeID)";

			var allocationId = await Company.Database.Connection.QueryFirstAsync<int>(allocationSQL, new { assetTypeID = assetType.ID, issueTypeID = issueType.ID });

			foreach (var rUid in model.responsibilityTypeUid)
			{
				string allocationResponsibilitySQL = $@"INSERT INTO IssueTypeRelationResponsibility (IssueTypeRelationID, ResponsibilityTypeId) 
															SELECT @allocationId, ID FROM ResponsibilityType where Uid = @responsibilityTypeUid";

				var res = await Company.Database.Connection.ExecuteAsync(allocationResponsibilitySQL, new { allocationId, responsibilityTypeUid = rUid });
			}

			return successMessageResponse(HttpStatusCode.OK, ApiMessages.Success, ActionApiMessages.AddSingleAllocationSuccessful);
		}
	}
}
