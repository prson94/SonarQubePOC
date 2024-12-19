using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.resources;
using d360.core.entities.Views;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.model.helpers;
using d360.model.helpers.filters;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Services;
using d360.web.Services.Favorites;
using Dapper;
using MediatR;
using Microsoft.Web.Http;
using repositories;
using Resources;
using SpreadsheetLight;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using static d360.core.entities.Resource;
using static d360.web.UserIDCheckMiddleware;
using System.Text.RegularExpressions;

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
		private readonly IMediator Mediator;
		private readonly IAssetRepository Assets;
		//private readonly ISecurity Security;
		private IQueueSource Queue;
		private IStorageProvider Storage;

		public MembershipController(
			ICoreComponentSet set,
			IAssetRepository assets,
			IQueueSource queue,
			IStorageProvider storage,
			//ISecurity security,
			IMediator mediator) : base(set)
		{
			Mediator = mediator;
			Assets = assets;

			Queue = queue;
			//Security = security;
			Storage = storage;
		}

		void cleanIncomingUsers(List<UserApiModel> users, bool isNew)
		{
			users.ForEach(u => {
				u.IsNew = isNew;
				u.FirstName = u.FirstName.SanitizeHtml();
				u.LastName = u.LastName.SanitizeHtml();
				if (string.IsNullOrWhiteSpace(u.Username))
				{
					u.Username = u.Email;
				}
				if (string.IsNullOrWhiteSpace(u.Email))
				{
					u.Email = u.Username;
				}
			});
		}

		List<string> validateIncomingUsers(List<UserApiModel> users)
		{
			List<string> errors = new List<string>();

			foreach(var u in users)
			{
				if (!string.IsNullOrEmpty(u.Password))
				{
					if (u.Password.Length < 7 || u.Password.Length > 25
					|| !u.Password.Any(char.IsUpper) || !u.Password.Any(char.IsLower)
					|| !u.Password.Any(char.IsDigit))
					{
						errors.Add(MemberShipErrors.PasswordRule);
					}

					if (string.IsNullOrEmpty(u.FirstName))
					{
						errors.Add(MemberShipErrors.FirstNameMissing);
					}

					if (string.IsNullOrEmpty(u.LastName))
					{
						errors.Add(MemberShipErrors.LastNameMissing);
					}

					if (u.FirstName != null && u.FirstName.Length > 250)
					{
						errors.Add(MemberShipErrors.FirstNameTooLong);
					}

					if (u.LastName != null && u.LastName.Length > 250)
					{
						errors.Add(MemberShipErrors.LastNameTooLong);
					}

					if (string.IsNullOrEmpty(u.Username) || !Regex.IsMatch(u.Username + "", @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b"))
					{
						errors.Add(MemberShipErrors.InvalidEmail);
					}
				}
			}
			
			return errors;
		}

		/// <summary>
		/// Retrieves a list of users.
		/// </summary>
		/// <remarks>
		/// Advanced filtering is done using _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
		/// *  For comparison operators you can use eq (equal), ne (not equal), gt (greater than), ge (greater than or equal), lt (less than), le (less than or equal) and ct (contains) which allows usage of (*) symbol as wildcard
		///     
		///     Example :
		///     
		///     - **Comparison Operators**
		///         - Equals operator - {fieldname} eq 'Data'
		///         - Not equals operator - {fieldname} ne 'Data'
		///         - Contains operator - {fieldname} ct 'Data'  
		///         - Greater than operator - {fieldname} gt 99
		///         - Greater than or equal operator - {fieldname} ge 99
		///         - Less than operator - {fieldname} lt 99
		///         - Less than or equal operator - {fieldname} le 99
		///         - Not populated operator - {fieldname} eq null
		///         - populated operator - {fieldname} ne null
		///     
		///     - **Logical Operators**
		///         - Logical and - {fieldname} ge 00 and {fieldname} le 99
		///         - Logical or - {fieldname} eq 'Data' or {fieldname} eq 'Data1'
		/// </remarks>
		///
		/// <param name="Uid">The uid of the user.</param>
		/// <param name="ResourceID">The id of the user.</param>
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
			SwaggerConsumes("application/json"), SwaggerProduces("application/json", "application/octet-stream"),
			SwaggerResponse(HttpStatusCode.OK, "Gets a list of Users.", typeof(ResourceApiViewModel)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Invalid PageSize/PageNum value provided. Number is too large"),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> GetUsers(CancellationToken Cancellationtoken, Guid? Uid = null, int? ResourceID = null, string FirstName = null, string LastName = null, core.enums.CompanyResourceState? State = null, bool? IsAdministrator = null, string _pageSize = "5", string _pageNum = "1", string _order = "ResourceID", string _direction = "asc", string _filter = "", string _simpleFilter = "")
		{
			try
			{
				if (Cancellationtoken == null)
				{
					Cancellationtoken = CancellationToken.None;
				}

				var showResources = await GetCachedSettingValueById<bool>(Setting.ShowResources);
				bool IsCurrentUser = false;

				var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;

				if (ResourceID != null)
				{
					if (ResourceID == SecurityContext.ResourceID)
					{
						IsCurrentUser = true;
					}
				}
				else if (Uid != null)
				{
					if (Company.GlobalReportingResources.Any(r => r.Uid == Uid && r.ResourceID == SecurityContext.ResourceID))
					{
						IsCurrentUser = true;
					}
				}

				if (!SecurityContext.IsAdministrator && !showResources && IsCurrentUser == false)
				{
					return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.Forbidden, ApiMessages.AccessDenied)).ConfigureAwait(false);
				}

				string finalSql = "";
				string countSql;
				string orderBySQL;
				bool iscommunityuserresposibility = false;
				Guid? responsibilitytypeuid = null;

				DynamicParameters dbArgs = new DynamicParameters();
				List<string> queries = new List<string>();
				ResourceApiViewModel model = new ResourceApiViewModel();
				List<string> fieldColumns = new List<string>();
				List<string> fieldJoins = new List<string>();

				var queryParams = Request.GetQueryNameValuePairs();

				if (queryParams.Any(q => q.Key.ToLower() == "iscommunityuserresposibility"))
				{
					if (!bool.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "iscommunityuserresposibility").Value, out bool tempbool))
					{
						return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, ApiMessages.InvalidBoolean)).ConfigureAwait(false);
					}

					iscommunityuserresposibility = tempbool;
				}

				if (queryParams.Any(q => q.Key.ToLower() == "responsibilitytypeuid"))
				{
					if (!Guid.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "responsibilitytypeuid").Value, out Guid tempguid))
					{
						return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidParameter, string.Format(ApiMessages.InvalidGuid, queryParams.ToList().FirstOrDefault(q => q.Key == "responsibilitytypeuid").Value))).ConfigureAwait(false);
					}

					responsibilitytypeuid = tempguid;
				}

				var joinBulder = new StringBuilder();
				joinBulder.Append($@" outer apply (select object,objectid, ID from Asset A1 where A1.Object = 'Resource' and A1.ObjectID = gr.ResourceID) A");

				var whereBuilder = new StringBuilder();
				var selectBuilder = new StringBuilder();
				var countBuilder = new StringBuilder();

				if (iscommunityuserresposibility)
				{
					int responsibilityTypeID = Company.ResponsibilityTypes.Where(t => t.UID == responsibilitytypeuid).Select(t => t.ID).First();

					string sqlStmt = $@"			
						drop table if exists #temprsdata;

						select		OC.ResourceID,
									OC.ResponsibilityTypeID,
									sum(OC.[Count] * OC.AssetCount) as OwnedItemCount
						into #temprsdata
						from		(
									select		ResponsibilityTypeID,
												ResourceID,
												count(1) as [Count],
												C.Count as AssetCount
									from		ResponsibilityDetail R 
									cross apply (
										select 
												case when R.ApplyToType = 1 and R.AssetID = 0 then 
													(select count(1) from Asset a where a.AssetTypeID = R.AssetTypeID) 
												else 
													1
										end as [Count]
									) C
									where		R.IsVisible = 1
												and R.ResponsibilityTypeID = @responsibilityTypeID
									group by	ResponsibilityTypeID,
												ResourceID,
												C.Count
									) OC
						group by	OC.ResourceID,
									OC.ResponsibilityTypeID;";
					selectBuilder.Append(sqlStmt);
					countBuilder.Append(sqlStmt);

					dbArgs.Add("responsibilityTypeID", responsibilityTypeID);
				}

				if (!iscommunityuserresposibility)
				{
					selectBuilder.Append($@"select
					gr.uid,
					gr.ResourceID, 
					gr.FirstName, 
					gr.LastName,
					gr.Email,
					gr.IsAdministrator,
					gr.LastLoggedInOn, 
					case gr.State 
						 when 1 then 'Active'
						 when 2 then 'Inactive'
						 when 3 then 'Deleted' end as State,
					gr.CreatedOn");
				}
				else
				{
					selectBuilder.Append($@"select
					gr.ResourceID, 
					gr.FirstName + ' ' + gr.LastName FirstName, 
					OC.ResponsibilityTypeID,
					OC.OwnedItemCount,
					gr.FirstName FName, 
					gr.LastName LName,
					gr.uid");
				}

				if (iscommunityuserresposibility)
				{
					countBuilder.Append($@"select count(1) from #temprsdata OC
										   inner join [reporting].[Global_Resource] gr 
										   on gr.ResourceID = OC.ResourceID");
				}
				else
				{
					countBuilder.Append("select count(1) from [reporting].[Global_Resource] gr ");
				}

				Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", _pageSize }, { "_pageNum", _pageNum } };
				string isValid = isPageSizeAndNumValid(pageParams);

				if (!string.IsNullOrEmpty(isValid))
				{
					return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, isValid)).ConfigureAwait(false);
				}

				List<FieldType> fieldTypes;

				List<int> assetTypeIds = Company.AssetTypes.Where(a => a.Class == AssetTypeClass.User).Select(i => i.ID).ToList();

				if (iscommunityuserresposibility)
				{
					fieldTypes = Company.FieldTypes.Where(f => f.AssetTypeID.HasValue && assetTypeIds.Contains(f.AssetTypeID.Value) && f.IsListable == true).ToList();
				}
				else
				{
					fieldTypes = Company.FieldTypes.Where(f => f.AssetTypeID.HasValue && assetTypeIds.Contains(f.AssetTypeID.Value)).ToList();
				}

				getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns, isExport: isStreamResponse);

				if (Uid != null || ResourceID != null || FirstName != null || LastName != null || State != null || IsAdministrator != null)
				{
					if (Uid != null)
					{
						dbArgs.Add("uid", Uid);
						queries.Add(" gr.uid = @uid");
					}

					if (ResourceID != null)
					{
						dbArgs.Add("ResourceID", ResourceID);
						queries.Add(" gr.ResourceID = @ResourceID");
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
					selectBuilder.Append("," + col);
				}
				foreach (var join in fieldJoins)
				{
					joinBulder.Append(" " + join);
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
						FilterExpressionParser filterExpressionParser;
						var filterDataProvider = new FilterDataProvider(Company);

						if (!iscommunityuserresposibility)
						{
							filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.CustomFields, false, true);
						}
						else
						{
							filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.CommunityResposibilityResource, false, false);
						}

						filterExpressionParser.LoadFieldTypes(fieldTypes, fieldColumns);
						queries.Add("(" + filterExpressionParser.Parse(filterValue, out Dictionary<string, object> sqlParams, out List<int> filteredFieldIds) + ")");

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

					List<string> defaultFields;

					if (!iscommunityuserresposibility)
					{
						defaultFields = new List<string> { "FirstName", "LastName", "Email", "IsAdministrator", "LastLoggedInOn", "CreatedOn" };
					}
					else
					{
						defaultFields = new List<string> { "gr.FirstName + ' ' + gr.LastName", "OC.OwnedItemCount" };
					}

					defaultFields.ForEach(f =>
					{
						if (f == "CreatedOn" || f == "LastLoggedInOn")
						{
							simpleFilters.Add($"cast({f} as datetime2) like @simpleFilter");
						}
						else
						{
							simpleFilters.Add($"{f} like @simpleFilter");
						}
					});

					simpleFilters.Add(@"(case gr.State 
					 when 1 then 'Active'
					 when 2 then 'Inactive'
					 when 3 then 'Deleted' end) like @simpleFilter");
					queries.Add("(" + string.Join(" or ", simpleFilters) + ")");
				}

				var hide = await GetCachedSettingValueById<bool>(Setting.HideData3SixtyUsers);

				if (hide && !IsCurrentUser)
				{
					queries.Add("email not like '%@infogix.com'");
					queries.Add("email not like '%@precisely.com'");
				}

				if (queries.Count() > 0)
				{
					whereBuilder.Append("where ");
				}

				for (int i = 0; i < queries.Count(); i++)
				{
					whereBuilder.Append(queries[i]);

					if (i < queries.Count() - 1)
					{
						whereBuilder.Append(" and ");
					}
				}
				List<string> validCols;

				if (!iscommunityuserresposibility)
				{
					validCols = new List<string> { "uid", "ResourceID", "FirstName", "LastName", "Email", "IsAdministrator", "LastLoggedInOn", "State", "CreatedOn" };
				}
				else
				{
					validCols = new List<string> { "FirstName", "OwnedItemCount" };
				}

				validCols.AddRange(fieldTypes.Select(x => x.Name));

				if (validCols.All(x => x.ToLowerInvariant() != _order.ToLowerInvariant()))
				{
					return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.OrderByFieldNotFound)).ConfigureAwait(false);
				}

				if (!new[] { "asc", "desc" }.Contains(_direction.ToLowerInvariant()))
				{
					return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.InvalidDirection)).ConfigureAwait(false);
				}

				orderBySQL = $"order by {_order} {_direction}";

				long.TryParse(_pageSize, out long pageSize);
				long.TryParse(_pageNum, out long pageNum);
				model.pageNum = pageNum;
				model.pageSize = pageSize;

				string offsetSql = $" {orderBySQL} offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
				
				if (iscommunityuserresposibility)
				{
					selectBuilder.Append($@" from #temprsdata OC inner join [reporting].[Global_Resource] gr on gr.ResourceID = OC.ResourceID");
				}
				else
				{
					selectBuilder.Append(" from [reporting].[Global_Resource] gr ");

				}

				finalSql = $"{selectBuilder} {joinBulder} {whereBuilder} {offsetSql}";
				countSql = $"{countBuilder} {joinBulder } {whereBuilder}";

				var results = await Company.Database.Connection.QueryAsync(
							 new CommandDefinition(finalSql,
							cancellationToken: Cancellationtoken,
							parameters: dbArgs,
							commandTimeout: ApiTimeout));

				int countResults = await Company.Database.Connection.QueryFirstOrDefaultAsync<int>(
							 new CommandDefinition(countSql,
							cancellationToken: Cancellationtoken,
							parameters: dbArgs,
							commandTimeout: ApiTimeout));				

				if (isStreamResponse)
				{
					byte[] xlsResult = GetUsersExcelFromResults(results, fieldTypes, iscommunityuserresposibility);

					var response = createFileResponseMessage(HttpStatusCode.OK, $"Users {DateTime.Now.ToShortDateString()}.xlsx", xlsResult);
					
					return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);
				}
				else
				{
					model.items = results;
					model.total = countResults;
					var response = Request.CreateResponse(HttpStatusCode.OK, model);

					return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);
				}
			}
			catch (FilterExpressionParserException ex)
			{
				string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

				return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.ErrorFilterExpressionParse, errorMessage)).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

				return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
		   SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)), 
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> AddMembers(Guid groupUid, List<InsertUserToGroup> users)
		{
			if (groupUid == Guid.Empty)
			{
				return errorMessageArgumentResponse(ActionApiMessages.UidNotEmptyAndRequired);
			}

			if (users.Count == 0)
			{
				return errorMessageArgumentResponse(ApiMessages.NoUserUIDProvided);
			}

			bool duplicates = users.GroupBy(u => u.Uid).Any(g => g.Count() > 1);
			if (duplicates)
			{
				return errorMessageArgumentResponse(ApiMessages.DuplicateUserUidProvided);
			}

			var response = await Workspace.AddMembersToGroupAsync(groupUid, users.Select(u => u.Uid).ToList());

			return response.IsSuccess ? 
				Ok(users) :
				errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
		}

		/// <summary>
		/// Retrieves members of a group for a given group unique identifier.
		/// </summary>
		/// <param name="groupUid">The unique identifier of the Group.</param>
		[
		   HttpGet,
		   MapToApiVersion("2.0"),
		   Route("groups/{groupUid:Guid}/members"),
		   SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
		   SwaggerResponse(HttpStatusCode.OK, "Gets Members of a Group.", typeof(ResourceApiViewModel)),
		   SwaggerResponse(HttpStatusCode.BadRequest, "Invalid PageSize/PageNum value provided. Number is too large"),
		   SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		   SwaggerParameter("_firstName", "The First Name of the user.", DataType = "string", ParameterType = "query", Required = false),
		   SwaggerParameter("_lastName", "The last name of the user.", DataType = "string", ParameterType = "query", Required = false),
		   SwaggerParameter("_pageSize", "The number of results to return per page. The default is 5 users per page and max value is 250.", DataType = "integer", ParameterType = "query", Required = false),
		   SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
		]
		public async Task<IHttpActionResult> GetMembers(Guid groupUid)
		{
			string finalSql;
			string countSql;
			var joinBuilder = new StringBuilder();
			joinBuilder.Append(" left join Asset A on A.Object = 'Resource' and A.ObjectID = gr.ResourceID ");
			var whereBuilder = new StringBuilder();

			List<string> fieldColumns = new List<string>();
			List<string> fieldJoins = new List<string>();

			var selectBuilder = new StringBuilder();

			joinBuilder.Append($@" outer apply (select case 
									when g.PrimaryOwnerResourceID = gr.ResourceID then 'Primary' 
									when g.SecondaryOwnerResourceID = gr.ResourceID then 'Secondary' 
									else null end 
								as [Owner])Ownership(Owner) ");

			selectBuilder.Append($@"
							select gr.uid, 
								gr.ResourceID, gr.FirstName, gr.LastName, gr.Email, 
								gr.IsAdministrator, gr.LastLoggedInOn, 
								Ownership.Owner,
								case gr.State 
									when 1 then 'Active' 
									when 2 then 'Inactive'
									when 3 then 'Deleted' end 
								as State ");
			var countBuilder = new StringBuilder();
			countBuilder.Append(@"
						   select count(*)
								   from[reporting].[Global_Resource] as gr
									   inner join [dbo].[ResourceGroup] rg on rg.ResourceID = gr.ResourceID
									   inner join [dbo].[Group] g on g.ID = rg.GroupID
									   inner join [dbo].[Asset] AB on AB.uid = '"
									+ groupUid + "'");

			string pageSize = "5";
			string pageNum = "1";
			DynamicParameters dbArgs = new DynamicParameters();
			ResourceApiViewModel model = new ResourceApiViewModel();
			var queryParams = Request.GetQueryNameValuePairs();

			foreach (var q in queryParams)
			{
				var key = q.Key.ToLower();

				if (key.StartsWith("_"))
				{
					switch (key)
					{
						case "_firstname":
							dbArgs.Add("firstName", q.Value);
							whereBuilder.Append(" and gr.FirstName = @firstName");
							countBuilder.Append(" and gr.FirstName = @firstName");
							break;
						case "_lastname":
							dbArgs.Add("lastName", q.Value);
							whereBuilder.Append(" and gr.lastName = @lastName");
							countBuilder.Append(" and gr.LastName = @lastName");
							break;
						case "_pagesize":
							pageSize = q.Value;
							break;
						case "_pagenum":
							pageNum = q.Value;
							break;
						default:
							continue;
					}
				}
			}

			Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };
			string isValid = isPageSizeAndNumValid(pageParams);

			if (!string.IsNullOrEmpty(isValid))
			{
				return errorMessageArgumentResponse(isValid);
			}

			var resourceTypeIds = Company.AssetTypes.Where(a => a.Class == AssetTypeClass.User).Select(i => i.ID).ToList();

			var fieldTypes = Company.FieldTypes.Where(f => f.AssetTypeID.HasValue && resourceTypeIds.Contains(f.AssetTypeID.Value)).ToList();
			getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);

			foreach (var col in fieldColumns)
			{
				selectBuilder.AppendLine("," + col);
			}

			foreach (var join in fieldJoins)
			{
				joinBuilder.AppendLine(join);
			}

			foreach (FieldType customField in fieldTypes)
			{
				if (queryParams.Any(x => x.Key == customField.Name))
				{
					var paramval = queryParams.FirstOrDefault(x => x.Key == customField.Name).Value;
					whereBuilder.Append($" and F{customField.ID}.FormattedValue = @field{customField.ID}");
					dbArgs.Add($"@field{customField.ID}", paramval);
				}
			}

			if (queryParams.ToList().Any(x => x.Key.ToLower() == "_simplefilter"))
			{
				var simpleFilter = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_simplefilter").Value.Trim();

				if (!string.IsNullOrEmpty(simpleFilter))
				{
					simpleFilter = Company.GetEscapedFilterString(simpleFilter);

					dbArgs.Add("@simpleFilter", simpleFilter);

					whereBuilder.Append($" and (concat(gr.LastName,', ',gr.FirstName) like @simpleFilter or Ownership.Owner like @simpleFilter)");
				}
			}


			long.TryParse(pageSize, out long _pageSize);
			long.TryParse(pageNum, out long _pageNum);
			model.pageNum = _pageNum;
			model.pageSize = _pageSize;

			string offsetSql = $" Order by gr.ResourceID offset {_pageSize * (_pageNum - 1)} rows fetch next {_pageSize} rows only";
			countSql = $"{countBuilder} {joinBuilder} where g.ID = AB.ObjectID {whereBuilder}";
			finalSql = $@"{selectBuilder} from[reporting].[Global_Resource] gr inner join [dbo].[ResourceGroup] rg on rg.ResourceID = gr.ResourceID 
									  inner join[dbo].[Group] g on g.ID = rg.GroupID
									  inner join[dbo].[Asset] AB on AB.uid = '{groupUid}' {joinBuilder} where g.ID = AB.ObjectID {whereBuilder} {offsetSql}";


			var results = await Company.QueryAsync<dynamic>(finalSql, dbArgs, ApiTimeout);
			var count = await Company.QueryAsync<int>(countSql, dbArgs, ApiTimeout);
			model.items = results;
			model.total = count.FirstOrDefault();

			return Ok(model);
		}

		[
		   HttpGet,
		   MapToApiVersion("2.0"),
		   Route("groups/{groupId:int}"),
		   ApiExplorerSettings(IgnoreApi = true)
	   ]
		public async Task<IHttpActionResult> GetGroupUid(int groupId)
		{
			string sql = $"SELECT uid FROM [dbo].[Group] where ID =" + groupId;
			var results = await Company.QueryAsync<dynamic>(sql, ApiTimeout);
			return Ok(results);
		}

		/// <summary>
		/// Retrieves a list of groups
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("groups"),
			SwaggerResponse(HttpStatusCode.OK, "", typeof(GroupApiModels)),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerParameter("Uid", "Uid of the group.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("Name", "Name of the group", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("ResourceUid", "Uid of user", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_simpleFilter", "The text or phrase you want to find within the listable fields of an asset. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_order", "The name of the field to order results by. The acceptable fields are Name or field defined on groups field definitions. By default the results are ordered by Name ascending.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false), 
			SwaggerParameter("_pageSize", "The number of results to return per page. The default is 10 groups per page", DataType = "integer", ParameterType = "query", Required = false),
			SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false)
		]
		public async Task<IHttpActionResult> GetGroups()
		{
			var queryParams = Request.GetQueryNameValuePairs();
			var results = await Workspace.ReadGroupsAsync(queryParams);
			return results.IsSuccess ? 
				Ok(results.Data) : 
				errorMessageResponse((HttpStatusCode)results.StatusCode, results.Message);
		}

		/// <summary>
		/// Deletes the specified user from the specified group.
		/// </summary>
		/// <param name="groupUid">The unique identifier of the Group.</param>
		/// <param name="resourceUid">The unique identifier of the resource.</param>
		[
			HttpDelete,
			Route("groups/{groupUid:Guid}/{resourceUid:Guid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource / Group doesn't exist.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - Provided group could not be updated", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> DeleteGroupMember(Guid groupUid, Guid resourceUid)
		{
			var response = await Workspace.RemoveMemberFromGroupAsync(groupUid, resourceUid);
			return response ?
				successMessageResponse(HttpStatusCode.OK, ApiMessages.Userremoved, ApiMessages.UserremovedMessage) :
				errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ApiMessages.ResourceGroupNotExists);
		}

		/// <summary>
		/// Deletes the specified users from Govern.
		/// </summary>
		/// <param name="users">A list of uids for users to delete.</param>
		[
			HttpDelete,
			Route("users"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerRequestExample(typeof(DeleteUserModel), typeof(DeleteUserExample)),
			SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource doesn't exist.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)), 
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> DeleteUsers(List<DeleteUserModel> users)
		{
			if (users == null || users.Count() == 0)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.NoUserRequest);
			}

			var uids = users.Select(u => u.Uid).ToList();
			var execution = getApiExecution(uids.Count, action: ApiExecutionAction.DeleteUsers);
			Company.Add(execution);
			var tenantResponse = await Workspace.RemoveUsersAsync(execution.Id, uids);
			
			var communityResponse = new RepositoryResponse<int>(400, "");
			string errormessage = "";
			bool isSuccess = false;
			
			if (tenantResponse.StatusCode == 200)
			{
				if (tenantResponse.IsSuccess)
				{
					communityResponse = await Community.RemoveUsersFromTenantAsync(SecurityContext.CompanyID, uids);
				}

				isSuccess = (tenantResponse.IsSuccess && communityResponse.IsSuccess);
				if (isSuccess)
				{
					Queue.CreateMessage(constants.Queue.Search, new ReindexModel
					{
						CompanyID = SecurityContext.CompanyID,
						Category = "Resource",
						To = QueueAction.RemoveFromIndex,
						BatchOperation = ReindexBatchOperation.Delete,
						BatchUids = uids
					});
				}
				else
				{
					errormessage = tenantResponse.Message;
				}
			}
			else
			{
				communityResponse.StatusCode = tenantResponse.StatusCode;
				errormessage = tenantResponse.Message;
			}
			return isSuccess ?
				successMessageResponse((HttpStatusCode)communityResponse.StatusCode, "Success", "Users removed from environment.") :
				errorMessageResponse((HttpStatusCode)communityResponse.StatusCode, "Error", errormessage);
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
		/// <param name="lookupFieldsPassedByValue">Optional query string parameter that allows you to pass list values numeric value instead of plain text value.  The default value for this is false.</param>
		[
			HttpPost,
			Route("users"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			//SwaggerRequestExample(typeof(UserApiModel), typeof(UserPostExample)),
			SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource doesn't exist.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)), 
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> PostUsers(List<UserApiModel> users, bool lookupFieldsPassedByValue = false)
		{
			if (users == null || users.Count == 0)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.NoUserRequest);
			}

			cleanIncomingUsers(users, true);
			var communityResponse = await Community.CreateUsersInTenantAsync(SecurityContext.CompanyID, users);
			
			var execution = getApiExecution(users.Count, action: ApiExecutionAction.UpsertUsers);
			Company.Add(execution);
			var tenantResponse = await Workspace.UpsertUsersAsync(execution.Id, users, lookupFieldsPassedByValue);
			return sendRepositoryOkResponse(tenantResponse);
		}

		/// <summary>
		/// Adds the specified users. This endpoint is meant for a greater number of users as it stores the user list for asynchronous or batch processing.
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
		/// <param name="lookupFieldsPassedByValue">Optional query string parameter that allows you to pass list values numeric value instead of plain text value.  The default value for this is false.</param>
		[
			HttpPost,
			Route("batch/users"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			//SwaggerRequestExample(typeof(UserApiModel), typeof(UserPostExample)),
			SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution's unique identifier to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource doesn't exist.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)), 
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> PostBulkUsers(List<UserApiModel> users, bool lookupFieldsPassedByValue = false)
		{
			if (users == null || users.Count == 0)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.NoUserRequest);
			}

			cleanIncomingUsers(users, true);

			UserUpsertModel model = new UserUpsertModel
			{
				Users = users.Select(u => new UserApiModel
				{
					Username = u.Username,
					Email = u.Email,
					FirstName = u.FirstName,
					LastName = u.LastName,
					Password = u.Password,
					IsAdministrator = u.IsAdministrator,
					Fields = u.Fields,
					IsNew = true,
					ItemNumber = u.ItemNumber,
					ExecutionItemUid = u.ExecutionItemUid
				}),
				LookupFieldsPassedByValue = lookupFieldsPassedByValue,
				IsInsert = true
			};

			var execution = getApiExecution(users.Count, action: ApiExecutionAction.UpsertUsers);
			var executionInfo = saveExecution(execution);

			await Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, model.AsJson());

			await Queue.CreateMessageAsync(constants.Queue.Execution, executionInfo);

			return await sendExecutionProcessingResponse(executionInfo);
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
		/// <param name="lookupFieldsPassedByValue">Optional query string parameter that allows you to pass list values numeric value instead of plain text value.  The default value for this is false.</param>
		/// <param name="IsChangePasswordReqeust">Optional query string parameter that allows you to password changed request.  The default value for this is false.</param>

		[
			HttpPut,
			RequireAdminPermissions,
			Route("users"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			//SwaggerRequestExample(typeof(UserApiModel), typeof(UserPutExample)),
			SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource doesn't exist.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> PutUsers(List<UserApiModel> users, bool lookupFieldsPassedByValue = false)
		{
			if (users == null || users.Count == 0)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.NoUserRequest);
			}

			cleanIncomingUsers(users, false);

			await Community.CreateUsersInTenantAsync(SecurityContext.CompanyID, users);

			var execution = getApiExecution(users.Count, action: ApiExecutionAction.UpsertUsers);
			Company.Add(execution);
			var tenantResponse = await Workspace.UpsertUsersAsync(execution.Id, users, lookupFieldsPassedByValue);
			return sendRepositoryOkResponse(tenantResponse);
		}

		[
			HttpPut,
			Route("users/me/password-reset"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			//SwaggerRequestExample(typeof(UserApiModel), typeof(UserApiModel)),
			SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse))
		]
		public async Task<IHttpActionResult> ResetUserPassword(UserApiModel user, bool lookupFieldsPassedByValue = false)
		{
			if (user == null)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.NoUserRequest);
			}

			cleanIncomingUsers(new List<UserApiModel> { user }, false);

			if (SecurityContext.AuthenticationType != AuthenticationType.Forms)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.IsChangePwdReqAuthOtherThanForm);
			}

			var newPassword = user.Fields.Where(z => z.Key == "NewPassword").Select(z => z.Value).FirstOrDefault();
			var currentPassword = user.Fields.Where(z => z.Key == "CurrentPassword").Select(z => z.Value).FirstOrDefault();
			var response = await Community.ResetUserPassword(SecurityContext.ResourceID, currentPassword, newPassword);

			return response.IsSuccess ? 
				Ok(new ConfirmResponse { message = "Password successfully reset.", title = "Reset Success" }) : 
				errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
		}


		/// <summary>
		/// Updates the specified users. This endpoint is meant for a greater number of users as it stores the user list for asynchronous or batch processing.
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
		/// <param name="lookupFieldsPassedByValue">Optional query string parameter that allows you to pass list values numeric value instead of plain text value.  The default value for this is false.</param>

		[
			HttpPut,
			Route("batch/users"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			//SwaggerRequestExample(typeof(UserApiModel), typeof(UserPutExample)),
			SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution's unique identifier to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Not found - Resource doesn't exist.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Unauthorized, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)), 
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> PutBulkUsers(List<UserApiModel> users, bool lookupFieldsPassedByValue = false)
		{
			if (users == null || users.Count == 0)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.NoUserRequest);
			}

			cleanIncomingUsers(users, false);

			UserUpsertModel model = new UserUpsertModel
			{
				Users = users.ToList(),
				LookupFieldsPassedByValue = lookupFieldsPassedByValue,
				IsInsert = false
			};

			var execution = getApiExecution(users.Count, action: ApiExecutionAction.UpsertUsers);
			var executionInfo = saveExecution(execution);

			await Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, model.AsJson());

			await Queue.CreateMessageAsync(constants.Queue.Execution, executionInfo);
			return await sendExecutionProcessingResponse(executionInfo);
		}

		/// <summary>
		/// Retrieves a list of favorite items for the current user
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("users/me/favorites"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "", typeof(List<FavoriteApiViewModel>)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> GetFavorites()
		{
			var favorites = await Mediator.Send(new GetFavoritesQuery.Request { ResourceId = SecurityContext.ResourceID });
			return Ok(favorites);
		}

		/// <summary>
		/// Retrieves the Home Page the current user
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("users/me/getHomePage"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "", typeof(FavoriteApiViewModel)),
			SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = true)
		]
		public async Task<IHttpActionResult> GetHomePage()
		{
			var favorites = await Mediator.Send(new GetFavoritesQuery.Request { ResourceId = SecurityContext.ResourceID, HomePageOnly = true });
			var homePage = favorites.SingleOrDefault();
			return Ok(homePage);
		}

		/// <summary>
		/// Removes a given set of favorites for the current user based on their Id. 
		/// </summary>
		/// <param name="favoriteIds">List of Ids corresponding to favorites</param>
		/// <returns>Status</returns>
		[
			HttpDelete,
			Route("users/me/favorites/bulk"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, ""),
			SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> DeleteFavorites(List<int> favoriteIds)
		{
			await Workspace.RemoveFavoritesAsync(SecurityContext.ResourceID, favoriteIds);
			return successMessageResponse(HttpStatusCode.OK, ApiMessages.Success, ApiMessages.FavoritesSuccessfullyDeleted);
		}

		/// <summary>
		/// Given a route, toggles the favorite status on/off for the current user
		/// </summary>
		/// <returns></returns>
		[
			HttpPut,
			Route("users/me/favorites"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerRequestExample(typeof(FavoriteApiModel), typeof(FavoriteApiModelExample)),
			SwaggerResponse(HttpStatusCode.Created, "Favorite status toggled.", typeof(IdResponse<int?>)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> ToggleFavorite(FavoriteApiModel favorite)
		{
			if (string.IsNullOrWhiteSpace(favorite.Route))
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.FavoritesEmptyRoute);
			}

			await Mediator.Send(new ToggleFavoriteOrHomePageCommand.Argument
			{
				ResourceId = SecurityContext.ResourceID,
				Route = favorite.Route,
				IsHomePage = false
			});

			var favoriteId = await Mediator.Send(new GetFavoriteId.Argument
			{
				ResourceId = SecurityContext.ResourceID,
				Route = favorite.Route,
			});

			return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, new IdResponse<int?>(favoriteId)));
		}

		/// <summary>
		/// Given a route, toggles the homepage status on/off for the current user
		/// </summary>
		/// <returns></returns>
		[
			HttpPut,
			Route("users/me/homepage"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerRequestExample(typeof(FavoriteApiModel), typeof(FavoriteApiModelExample)),
			SwaggerResponse(HttpStatusCode.Created, "Homepage status toggled.", typeof(IdResponse<int?>)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Bad Request - the format or contents of this request are not valid.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> ToggleHomepage(FavoriteApiModel favorite)
		{
			if (string.IsNullOrWhiteSpace(favorite.Route))
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.FavoritesEmptyRoute);
			}

			await Mediator.Send(new ToggleFavoriteOrHomePageCommand.Argument
			{
				ResourceId = SecurityContext.ResourceID,
				Route = favorite.Route,
				IsHomePage = true
			});

			var favoriteId = await Mediator.Send(new GetFavoriteId.Argument
			{
				ResourceId = SecurityContext.ResourceID,
				Route = favorite.Route,
			});

			return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, new IdResponse<int?>(favoriteId)));
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
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)), 
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> DeleteGroupAsync(List<DeleteGroupModel> groups)
		{
			if (groups.Count() < 1)
			{
				return errorMessageArgumentResponse(ApiMessages.NoGroupRequest);
			}
			
			var execution = getApiExecution(groups.Count, action: ApiExecutionAction.DeleteGroups);
			Company.Add(execution);
			var result = await Workspace.RemoveGroupsAsync(execution.Id, groups.Select(g => g.Uid).ToList());
			
			return result.IsSuccess ?
				Ok(result.Data) :
				errorMessageResponse((HttpStatusCode)result.StatusCode, result.Message);
		}

		private bool IsValidGuid(IEnumerable<KeyValuePair<string, string>> queryParams, string paramName)
		{
			bool isValid = true;

			if (queryParams.ToList().Any(q => q.Key.ToLower() == paramName.ToLowerInvariant()))
			{
				var uidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLowerInvariant() == paramName.ToLowerInvariant()).Value;
				
				if (Guid.TryParse(uidString, out Guid uid))
				{
					isValid = true;
				}
				else
				{
					isValid = false;
				}
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
			SwaggerRequestExample(typeof(UpdateGroupModel), typeof(UpdateGroupModelExample)),
			SwaggerResponse(HttpStatusCode.OK, "Success", typeof(List<GroupResponseResult>)),
			SwaggerResponse(HttpStatusCode.BadRequest, "There are no groups in this request.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)), 
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> UpdateGroupAsync(List<UpdateGroupModel> groups, bool lookupFieldsPassedByValue = false)
		{
			if (groups.Count < 1)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.NoGroupRequest);
			}

			var isValid = groups.All(x => x.Uid.HasValue);

			if (!isValid)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ActionApiMessages.UidNotEmptyAndRequired);
			}

			var execution = getApiExecution(groups.Count, action: ApiExecutionAction.PutGroups);
			Company.Add(execution);
			var result = await Workspace.UpsertGroupsAsync(execution.Id, groups, false, lookupFieldsPassedByValue);

			return result.IsSuccess ? 
				Ok(result.Data) : 
				errorMessageResponse((HttpStatusCode)result.StatusCode, result.Message);
		}

		/// <summary>
		/// Add a group based on the data provided in request.
		/// </summary>
		/// <param name="groups">The groups that will be added</param>
		[
			HttpPost,
			Route("groups"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerRequestExample(typeof(UpdateGroupModel), typeof(UpdateGroupModelExample)),
			SwaggerResponse(HttpStatusCode.OK, "Success", typeof(List<GroupResponseResult>)),
			SwaggerResponse(HttpStatusCode.BadRequest, "There are no groups in this request.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)), 
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> AddGroupAsync(List<UpdateGroupModel> groups, bool lookupFieldsPassedByValue = false)
		{
			if (groups.Count < 1)
			{
				return errorMessageArgumentResponse(ApiMessages.NoGroupRequest);
			}

			if (groups.Any(x => string.IsNullOrEmpty(x.Name)))
			{
				return errorMessageArgumentResponse(ApiMessages.NameMissingInGroupPayload);
			}

			var execution = getApiExecution(groups.Count, action: ApiExecutionAction.PostGroups);
			Company.Add(execution);
			var result = await Workspace.UpsertGroupsAsync(execution.Id, groups, true, lookupFieldsPassedByValue);

			return result.IsSuccess ?
				Ok(result.Data) :
				errorMessageResponse((HttpStatusCode)result.StatusCode, result.Message);
		}

		private byte[] GetUsersExcelFromResults(IEnumerable<dynamic> results, List<FieldType> fieldTypes, bool iscommunityuserresposibility)
		{
			List<Tuple<string, string, string>> fieldMap = new List<Tuple<string, string, string>>();
			
			if (!iscommunityuserresposibility)
			{
				fieldMap.Add(new Tuple<string, string, string>("First name", "FirstName", "Text"));
				fieldMap.Add(new Tuple<string, string, string>("Last name", "LastName", "Text"));
				fieldMap.Add(new Tuple<string, string, string>("Email", "Email", "Text"));
				fieldTypes.Where(x => x.IsListable == true).ToList().ForEach(ft =>
				{
					fieldMap.Add(new Tuple<string, string, string>(ft.FriendlyName, ft.Name, ft.Type));
				});
				fieldMap.Add(new Tuple<string, string, string>("Created on", "CreatedOn", "Date"));
				fieldMap.Add(new Tuple<string, string, string>("Last logged in on", "LastLoggedInOn", "Date"));
				fieldMap.Add(new Tuple<string, string, string>("Administrator?", "IsAdministrator", "Boolean"));
				fieldMap.Add(new Tuple<string, string, string>("Status", "State", "Text"));
				fieldMap.Add(new Tuple<string, string, string>("User UID", "uid", "Text"));
			}
			else
			{
				fieldMap.Add(new Tuple<string, string, string>("First Name", "FName", "Text"));
				fieldMap.Add(new Tuple<string, string, string>("Last Name", "LName", "Text"));
				fieldMap.Add(new Tuple<string, string, string>("Items Owned", "OwnedItemCount", "Text"));
				fieldTypes.Where(x => x.IsListable == true).ToList().ForEach(ft =>
				{
					fieldMap.Add(new Tuple<string, string, string>(ft.FriendlyName, ft.Name, ft.Type));
				});
				fieldMap.Add(new Tuple<string, string, string>("User UID", "uid", "Text"));
			}

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

		/// <summary>
		/// Retrieve the current users API credentials
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("users/me/apikey"),
			SwaggerResponse(HttpStatusCode.OK, "", typeof(List<ApiKeyDetailModel>)),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> GetApikey()
		{
			var showAllUsersAPIKey = await GetCachedSettingValueById<bool>(Setting.ShowAllUsersAPIKey);

			if (!SecurityContext.IsAdministrator && !showAllUsersAPIKey)
			{
				return errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.Forbidden, ApiMessages.AccessDenied);
			}

			var resource = await Community.ReadUserByIdAsync(SecurityContext.ResourceID);
			if (!resource.IsSuccess)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.InvalidUser);
			}

			var apikeydetail = new ApiKeyDetailModel
			{
				apiKey = resource.Data.APIPublicKey,
				apiSecret = resource.Data.APIPrivateKey
			};

			if (apikeydetail.apiKey == null || apikeydetail.apiSecret == null)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.InvalidUser);
			}

			return Ok(apikeydetail);
		}

		/// <summary>
		/// Retrieve the roles of current user (Administrator/User)
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("users/me/roles"),
			SwaggerResponse(HttpStatusCode.OK, "", typeof(List<string>)),
			SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = true)
		]
		public IHttpActionResult GetUserRoles()
		{
			List<string> roles = new List<string>();

			if (SecurityContext.IsAdministrator)
			{
				roles.Add("Administrator");
			}
			else
			{
				roles.Add("User");
			}

			return Ok(roles);
		}

		/// <summary>
		/// Updates the watch status of an Asset/Asset Type for the requesting user.
		/// </summary>
		/// <param name="model">Request model containing the Asset/Asset Type to be watched/unwatched</param>
		[
			HttpPut,
			Route("users/me/watches"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Invalid request model parameters provided.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> UpdateWatches(UpdateUserWatchModel model)
		{
			int id = -1;
			long AssetID = 0;
			int AssetTypeID = 0;
			string name = "";
			string parentName = "";
			bool includeChildren = false;
			FollowDetail followDetail = null;

			if ((model.assetTypeUid == null && model.assetUid == null) || (model.assetTypeUid != null && model.assetUid != null))
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ActionApiMessages.AssetTypeOrAssetRequired)).ConfigureAwait(false);
			}

			if (model.assetTypeUid != null)
			{
				if ((model.assetTypeUid.Value == Guid.Empty) || !Company.Any<AssetType>(x => x.uid == model.assetTypeUid))
				{
					return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ActionApiMessages.InvalidAssetTypeUid)).ConfigureAwait(false);
				}
				else
				{
					var assetType = Assets.GetAssetTypeByUID(model.assetTypeUid.Value);
					AssetTypeID = assetType.ID; 
					name = assetType.Name;
					includeChildren = true;
					followDetail = Company.Filter<FollowDetail>(i => i.AssetTypeID == AssetTypeID && i.AssetID == null && i.ResourceID == SecurityContext.ResourceID).FirstOrDefault();
				}
			}

			if (model.assetUid != null)
			{

				if ((model.assetUid.Value == Guid.Empty) || !Company.Any<Asset>(x => x.uid == model.assetUid.Value))
				{
					return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidAssetUid)).ConfigureAwait(false);
				}
				else
				{
					var asset = Company.Filter<AssetDetail>(x => x.uid == model.assetUid.Value).FirstOrDefault();
					AssetID = asset.id; 
					name = asset.DisplayValue;
					parentName = asset.TypeName;
					followDetail = Company.Filter<FollowDetail>(i => i.AssetID == AssetID && i.ResourceID == SecurityContext.ResourceID).FirstOrDefault();
				}
			}

			if (model.watches && followDetail != null)
			{

				if (followDetail.HardFollow)
				{
					return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ApiMessages.AlreadyWatch, (model.assetTypeUid != null) ? $"{ApiMessages.Type} '{name}'" : $"'{name}'"))).ConfigureAwait(false);
				}
				else
				{
					if (followDetail != null && !followDetail.HardFollow)
					{
						return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ApiMessages.CurrentWatch, name, parentName))).ConfigureAwait(false);
					}
				}
			}

			if (!model.watches)
			{
				if (followDetail == null)
				{
					return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ApiMessages.NotCurrentWatch, (model.assetTypeUid != null) ? $"{ApiMessages.Type} '{name}'" : $"'{name}'"))).ConfigureAwait(false);
				}

				if (followDetail != null && !followDetail.HardFollow)
				{
					return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ApiMessages.CurrentWatchNotUnwatchIndividually, parentName))).ConfigureAwait(false);
				}
			}

			bool success = Company.UpdateFollowStatus(AssetTypeID, AssetID, null, includeChildren);

			return await Task.FromResult(successMessageResponse(HttpStatusCode.OK, ApiMessages.Success, string.Format(success ? ApiMessages.YouAreNowWatching : ApiMessages.YouAreNoLongerWatching,
																													  model.assetTypeUid != null ? $"{ApiMessages.Type} '{name}'" : $"'{name}'"))).ConfigureAwait(false);
		}

		/// <summary>
		/// Retrieve a users logo
		/// </summary>
		/// <param name="uid">Uid of the user</param>
		/// <param name="size">Size of the image to be returned in pixels.</param>
		[
			HttpGet,
			Route("users/{uid:Guid}/image"),
			SwaggerConsumes("application/json"),
			SwaggerProduces("application/octet-stream"),
			SwaggerResponse(HttpStatusCode.OK, "Success"),
			SwaggerResponse(HttpStatusCode.BadRequest, "Invalid size specified for image, value greater than max or less than or equal to 0.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.NotFound, "Invalid Resource Uid provided.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> MyImage(Guid uid, int size = 150)
		{
			var user = await Community.ReadUserByUidAsync(uid);

			if (!user.IsSuccess || user.Data != null)
			{
				return errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ActionApiMessages.ResourceUidNotValid);
			}

			if (size < 1 || size > 2048)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.ImageSize2048);
			}

			MD5 md5Hasher = MD5.Create();

			// Convert the input string to a byte array and compute the hash. 
			// 1.  Trim leading and trailing whitespace from an email address
			// 2.  Force all characters to lower-case
			// 3.  md5 hash the final string
			byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes((user.Data.Email ?? "").Trim().ToLower()));

			// Create a new Stringbuilder to collect the bytes  
			// and create a string.  
			StringBuilder sBuilder = new StringBuilder();

			// Loop through each byte of the hashed data  
			// and format each one as a hexadecimal string.  
			for (int i = 0; i < data.Length; i++)
			{
				sBuilder.Append(data[i].ToString("x2"));
			}

			string url = "http://www.gravatar.com/avatar/" + sBuilder + "?s=" + size + "&d=mm";
			var uri = new Uri(url);

			using (var client = new HttpClient())
			{
				var res = client.GetAsync(uri).Result;
				byte[] content = await res.Content.ReadAsByteArrayAsync();
				var response = createFileResponseMessage(HttpStatusCode.OK, $"user.png", content);

				return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Checks the watch status of an Asset for the requesting user.
		/// </summary>
		/// <param name="assetTypeUid">Uid of the asset type</param>
		/// <param name="assetUid">Uid of the asset</param>
		[
			HttpGet,
			Route("users/me/watches/{assetTypeUid:Guid}/{assetUid:Guid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "true/false based on whether the user is watching the given asset.", typeof(bool)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Invalid parameters provided.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> GetWatchStatusOfAsset(Guid assetTypeUid, Guid? assetUid)
		{
			return await Task.FromResult(GetWatchStatusForUser(assetTypeUid, assetUid)).ConfigureAwait(false);
		}

		/// <summary>
		/// Checks the watch status of an Asset Type for the requesting user.
		/// </summary>
		/// <param name="assetTypeUid">Uid of the asset type</param>
		[
			HttpGet,
			Route("users/me/watches/{assetTypeUid:Guid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "true/false based on whether the user is watching the given asset type.", typeof(bool)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Invalid parameters provided.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> GetWatchStatusOfAssetType(Guid assetTypeUid)
		{
			return await Task.FromResult(GetWatchStatusForUser(assetTypeUid, null)).ConfigureAwait(false);
		}

		private IHttpActionResult GetWatchStatusForUser(Guid assetTypeUid, Guid? assetUid)
		{
			bool response = false;

			if ((assetTypeUid == Guid.Empty) || !Company.Any<AssetType>(x => x.uid == assetTypeUid))
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ActionApiMessages.InvalidAssetTypeUid);
			}

			var assetType = Assets.GetAssetTypeByUID(assetTypeUid);

			if (assetUid != null)
			{

				if ((assetUid.Value == Guid.Empty) || !Company.Any<Asset>(x => x.uid == assetUid.Value))
				{
					return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ActionApiMessages.InvalidAssetUid);
				}
				else
				{
					var asset = Company.Filter<AssetDetail>(x => x.uid == assetUid.Value).FirstOrDefault();

					if (asset.AssetTypeUid != assetTypeUid)
					{
						return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.AssetValidateWithAssetType);
					}

					response = Company.Any<Follow>(F => F.AssetID  == asset.ID && F.ResourceID == SecurityContext.ResourceID);
				}
			}

			if (!response)
			{
				response = Company.Any<Follow>(F => F.AssetTypeID == assetType.ID && F.ResourceID == SecurityContext.ResourceID);
			}

			return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response));
		}

		/// <summary>
		/// Generates a new API key / secret for the current user
		/// </summary>
		/// <param name="model">Request model containing the current API key and API secret</param>
		[
			HttpPost,
			Route("users/me/apikey"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Success", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.BadRequest, "Invalid request model parameters provided.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
		]
		public async Task<IHttpActionResult> UpdateApiKey(ApiKeyDetailModel model)
		{
			var resource = await Community.ReadUserByIdAsync(SecurityContext.ResourceID);

			if (model is null)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.ErrorInvalidDatasetMessage);
			}

			if (string.IsNullOrEmpty(model?.apiKey))
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ApiMessages.RequiredFieldError, "apikey"));
			}

			if (string.IsNullOrEmpty(model?.apiSecret))
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(ApiMessages.RequiredFieldError, "apiSecret"));
			}

			if (resource.IsSuccess)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidUser);
			}

			if (resource.Data.APIPublicKey != model.apiKey || resource.Data.APIPrivateKey != model.apiSecret)
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.InvalidApiKeyOrApiSecret);
			}

			var updatedResource = await Community.UpdateUserApiCredentialsAsync(SecurityContext.ResourceID);
				
			ApiKeyDetailModel newKeys = null;

			if (updatedResource.IsSuccess)
			{
				newKeys = new ApiKeyDetailModel { apiKey = updatedResource.Data.APIPublicKey, apiSecret = updatedResource.Data.APIPrivateKey };
			}

			var users = Cache.GetItem<ConcurrentBag<usercompany>>("Users");

			if (users != null)
			{
				var cachedUser = users.FirstOrDefault(uc => uc.ResourceID == SecurityContext.ResourceID);
				cachedUser.APIPublicKey = newKeys.apiKey;
				cachedUser.APIPrivateKey = newKeys.apiSecret;
			}

			return updatedResource.IsSuccess ? 
				Ok(newKeys) : 
				errorMessageResponse((HttpStatusCode)updatedResource.StatusCode, "Error updated API credentials.");
		}

		/// <summary>
		/// Creates a new claim.
		/// </summary>
		/// <remarks>
		/// NOTE: Only claims with a location of Idp or Environment can be added
		/// 
		/// Only certain claim types are valid based on the claim action:
		/// 
		///  - For the Lookup action the following claim types are allowed: Tenant, NameIdentifier, Username
		///  - For the Replace action the following claim types are allowed: Email, FirstName, LastName, Group
		///  - For the Append action the following claim types are allowed: Group
		/// </remarks>
		[
			HttpPost,
			MapToApiVersion("2.0"),
			Route("claims"),
			RequireAdminPermissions,
			SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.Created, "Claim was created successfully."),
			SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation."),
			SwaggerResponse(HttpStatusCode.BadRequest, "Claim could not be created due to an invalid path length or action."),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> PostClaimAsync(ClaimPostApiModel claim)
		{
			var claimType = claim.ClaimType.GetType().GetMember(claim.ClaimType.ToString()).First();
			var allowedActions = ((AllowedActionsAttribute)claimType.GetCustomAttribute(typeof(AllowedActionsAttribute))).Actions;

			if (!allowedActions.Contains(claim.Action))
			{
				return errorMessageArgumentResponse("");
			}

			if (claim.Location == ClaimLocation.Default || claim.Location == ClaimLocation.Client)
			{
				return errorMessageForbiddenResponse("");
			}

			if (claim.Path?.Length > 250)
			{
				return errorMessageArgumentResponse(ApiMessages.ValueNotExpectedRange);
			}

			var newClaim = new ClaimMapping();

			if (claim.Location == ClaimLocation.Environment)
			{
				newClaim.ClientId = SecurityContext.ClientID;
				newClaim.CompanyId = SecurityContext.CompanyID;
				newClaim.DomainSettingId = 0;
			}
			else if (claim.Location == ClaimLocation.Idp)
			{
				newClaim.ClientId = SecurityContext.ClientID;
				newClaim.CompanyId = SecurityContext.CompanyID;
				newClaim.DomainSettingId = SecurityContext.DomainSettingID;
			}
			else if (claim.Location == ClaimLocation.Client)
			{
				newClaim.ClientId = SecurityContext.ClientID;
				newClaim.CompanyId = 0;
				newClaim.DomainSettingId = 0;
			}
			else
			{
				newClaim.ClientId = 0;
				newClaim.CompanyId = 0;
				newClaim.DomainSettingId = 0;
			}

			newClaim.ClaimType = claim.ClaimType;
			newClaim.AuthenticationType = SecurityContext.AuthenticationType;
			newClaim.Action = claim.Action;
			newClaim.Path = claim.Path;
			newClaim.IsArray = claim.IsArray;

			await Community.CreateClaimAsync(newClaim);

			return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created));
		}

		/// <summary>
		/// Updates a claim for the given id.
		/// </summary>
		/// <param name="id">The id of the claim to be updated</param>
		/// <remarks>
		/// NOTE: Only claims with a location of Idp or Environment can be updated
		/// 
		/// Only certain claim types are valid based on the claim action:
		/// 
		///  - For the Lookup action the following claim types are allowed: Tenant, NameIdentifier, Username
		///  - For the Replace action the following claim types are allowed: Email, FirstName, LastName, Group
		///  - For the Append action the following claim types are allowed: Group
		/// </remarks>
		[
			HttpPut,
			MapToApiVersion("2.0"),
			Route("claims/{id:int}"),
			RequireAdminPermissions,
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Claim was updated successfully."),
			SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation."),
			SwaggerResponse(HttpStatusCode.NotFound, "Claim with the given id was not found."),
			SwaggerResponse(HttpStatusCode.BadRequest, "Claim could not be updated due to an invalid path length or action."),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> PutClaimAsync(int id, ClaimPutApiModel claim)
		{
			var existingClaim = (await Community.ReadClaimMappingById(id)).Data;

			if (existingClaim == null)
			{
				return errorMessageNotFoundResponse("");
			}

			var claimType = existingClaim.ClaimType.GetType().GetMember(existingClaim.ClaimType.ToString()).First();
			var allowedActions = ((AllowedActionsAttribute)claimType.GetCustomAttribute(typeof(AllowedActionsAttribute))).Actions;

			if (!allowedActions.Contains(claim.Action))
			{
				return errorMessageArgumentResponse("");
			}

			if (existingClaim.Location == ClaimLocation.Default || existingClaim.Location == ClaimLocation.Client)
			{
				return errorMessageForbiddenResponse("");
			}

			if (claim.Path?.Length > 250)
			{
				return errorMessageArgumentResponse("");
			}

			await Community.UpdateClaimAsync(id, claim.Action, claim.Path, claim.IsArray);

			return Ok();
		}

		/// <summary>
		/// Deletes a claim for the given id.
		/// </summary>
		/// <param name="id">The id of the claim to delete</param>
		/// <remarks>
		/// NOTE: Only claims with a location of Idp or Environment can be deleted
		/// </remarks>
		[
			HttpDelete,
			MapToApiVersion("2.0"),
			Route("claims/{id:int}"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Claim was deleted successfully."),
			SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation."),
			SwaggerResponse(HttpStatusCode.NotFound, "Claim with the given id was not found."),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> DeleteClaimAsync(int id)
		{
			if (!SecurityContext.IsAdministrator)
			{
				return errorMessageForbiddenResponse("Not allowed to remove claim.");
			}

			var existingClaim = await Community.ReadClaimMappingById(id);

			if (!existingClaim.IsSuccess)
			{
				return errorMessageNotFoundResponse("Claim not found.");
			}

			if (existingClaim.Data.Location == ClaimLocation.Default || existingClaim.Data.Location == ClaimLocation.Client)
			{
				return errorMessageForbiddenResponse("Not allowed to remove default or client claim.");
			}

			var response = await Community.RemoveClaimAsync(id, SecurityContext.ClientID, SecurityContext.CompanyID, SecurityContext.DomainSettingID);

			return response.IsSuccess ? 
				Ok() : 
				errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
		}

		/// <summary>
		/// Retrieves a list of claims.
		/// </summary>
		[
			HttpGet,
			MapToApiVersion("2.0"),
			Route("claims"),
			RequireAdminPermissions,
			SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Gets a list of claims.", typeof(ClaimApiViewModel)),
			SwaggerResponse(HttpStatusCode.Forbidden, "Access denied / you are not an admin and dont have access to perform this operation."),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> GetClaimsAsync()
		{
			var response = await Community.ReadClaimsByTenantAsync(SecurityContext.ClientID, SecurityContext.CompanyID, SecurityContext.DomainSettingID);
			List<ClaimApiViewModel> models = null;
			if (response.IsSuccess)
			{
				models = response.Data.Select(c => new ClaimApiViewModel { Action = c.Action, ClaimType = c.ClaimType, Id = c.Id, IsArray = c.IsArray, Location = c.Location, Path = c.Path }).ToList();
			}
			return Ok(models);
		}
	}
}
