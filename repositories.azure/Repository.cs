using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using Dapper;
using Newtonsoft.Json.Linq;

namespace repositories.azure
{
	public abstract class Repository
	{
		public bool CurrentUserIsAdmin { get; set; }
		public int CurrentUserId { get; set; }
		public Platform Platform { get { return Platform.Azure; } }	
		public DapperConnectionProvider ConnectionProvider { get; set; }
		
		// Commonly used sql expressions in thr repositories.
		internal readonly string FIELD_VALIDATION_COLUMNS = "f.ID, f.Name, f.Type, f.AllowMultipleValues, f.MinimumLength, f.MaximumLength, f.Length, f.Pattern, f.IsRequired";
		internal readonly int MAX_PERMISSIONS_MASK = 15854;

		protected Repository(DapperConnectionProvider provider)
		{
			ConnectionProvider = provider;
		}

		protected int CommandTimeout
		{
			get
			{
				int commandTimeout;
				if (!int.TryParse(ConnectionProvider.CommandTimeOut, out commandTimeout))
				{
					commandTimeout = 90;
				}
				return commandTimeout;
			}
		}

		private string GetFullExceptionData(Exception ex, bool includeStacktrace = true, int characterLimit = 2000)
		{
			StringBuilder sb = new StringBuilder();
			bool isSqlException = (ex.InnerException != null && ex.InnerException.InnerException != null && ex.InnerException.InnerException.GetType() == typeof(SqlException));

			if (isSqlException)
			{
				SqlException sqlException = (SqlException)ex.InnerException.InnerException;

				foreach (SqlError sqlError in sqlException.Errors)
				{
					if (sb.Length > 0)
					{
						sb.Append(" ");
					}

					sb.Append(sqlError.Message);
				}
			}
			else
			{
				if (!ex.Message.Contains("inner exception for details"))
				{
					sb.Append(ex.Message);
				}

				var iex = ex.InnerException;
				while (iex != null)
				{
					sb.Append("; ");
					sb.Append(iex.Message);
					if (includeStacktrace)
					{
						sb.Append("-----");
						sb.Append(iex.StackTrace);
					}
					iex = iex.InnerException;
				}
			}

			if (characterLimit == -1)
			{
				return sb.ToString();
			}
			else
			{
				string message = sb.ToString().Substring(0, Math.Min(characterLimit, sb.Length));
				return message;
			}
		}

		protected async Task<GlobalReportingResource> GetUser(int? resId)
		{
			try
			{
				var parameters = new
				{
					ResourceId = resId
				};
				using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
				{
					var user = await connection.QueryFirstOrDefaultAsync<GlobalReportingResource>(
					 @"SELECT 
						g.ResourceID, g.uid as Uid,
						g.LastLoggedInOn,g.State, g.,
						g.FirstName, g.LastName, g.Email,
						g.CreatedOn, g.UpdatedOn
						from reporting.Global_Resource g
						where g.ResourceID = @ResourceId",
					 parameters,
					 commandTimeout: CommandTimeout
						);

					return user;
				}
			}
			catch (Exception)
			{

				throw;
			}
		}

		public bool PermissionInMask(Permission p, int mask)
		{
			Permission pMask = (Permission)mask;
			return (pMask & p) == p;
		}

		public async Task<int> ReadCombinedPermissionByAssetId(long id)
		{
			int permissions = 0;

			if (CurrentUserIsAdmin)
			{
				permissions = MAX_PERMISSIONS_MASK;
			}
			else
			{
				using (var connection = ConnectionProvider.Connect(true))
				{
					permissions = await connection.QueryFirstAsync<int>(@"select dbo.GetCombinedPermissionsForUserByAssetId(@id, @currentUserId)", new { id, CurrentUserId });
				}
			}
			return permissions;
		}
		public async Task<int> ReadCombinedPermissionByAssetLegacy(string @object, int id)
		{
			int permissions = 0;
			if (CurrentUserIsAdmin)
			{
				permissions = MAX_PERMISSIONS_MASK;
			}
			else
			{
				using (var connection = ConnectionProvider.Connect(true))
				{
					permissions = await connection.QueryFirstAsync<int>(@"select dbo.GetCombinedPermissionsForUserByAssetUid(@object, @id, @currentUserId)", new { @object, id, CurrentUserId });
				}
			}
			return permissions;
		}
		public async Task<int> ReadCombinedPermissionByAssetUid(Guid uid)
		{
			int permissions = 0;
			if (CurrentUserIsAdmin)
			{
				permissions = MAX_PERMISSIONS_MASK;
			}
			else
			{
				using (var connection = ConnectionProvider.Connect(true))
				{
					permissions = await connection.QueryFirstAsync<int>(@"select dbo.GetCombinedPermissionsForUserByAssetUid(@uid, @currentUserId)", new { uid, CurrentUserId });
				}
			}
			return permissions;
		}

		public async Task<int> ReadCombinedPermissionByAssetTypeId(int id)
		{
			int permissions = 0;
			if (CurrentUserIsAdmin)
			{
				permissions = MAX_PERMISSIONS_MASK;
			}
			else
			{
				using (var connection = ConnectionProvider.Connect(true))
				{
					permissions = await connection.QueryFirstAsync<int>(@"select dbo.GetCombinedPermissionsForUserByAssetTypeId(@id, @currentUserId)", new { id, CurrentUserId });
				}
			}
			return permissions;
		}
		public async Task<int> ReadCombinedPermissionByAssetTypeLegacy(string @object, int id)
		{
			int permissions = 0;
			if (CurrentUserIsAdmin)
			{
				permissions = MAX_PERMISSIONS_MASK;
			}
			else
			{
				using (var connection = ConnectionProvider.Connect(true))
				{
					permissions = await connection.QueryFirstAsync<int>(@"select dbo.GetCombinedPermissionsForUserByAssetTypeUid(@object, @id, @currentUserId)", new { @object, id, CurrentUserId });
				}
			}
			return permissions;
		}
		public async Task<int> ReadCombinedPermissionByAssetTypeUid(Guid uid)
		{
			int permissions = 0;
			if (CurrentUserIsAdmin)
			{
				permissions = MAX_PERMISSIONS_MASK;
			}
			else
			{
				using (var connection = ConnectionProvider.Connect(true))
				{
					permissions = await connection.QueryFirstAsync<int>(@"select dbo.GetCombinedPermissionsForUserByAssetTypeUid(@uid, @currentUserId)", new { uid, CurrentUserId });
				}
			}
			return permissions;
		}

		internal FieldValidationResult isFieldValid(FieldTypeValidation ft, string value)
		{
			FieldValidationResult result;
			DataType type = (DataType)Enum.Parse(typeof(DataType), ft.Type);

			result = type.ValidateRestricted(ft.Name, ft.Type);
			if (!result.IsValid)
			{
				return result;
			}
			result = type.ValidateRequirement(ft.Name, ft.IsRequired, value);
			if (!result.IsValid)
			{
				return result;
			}

			switch (type)
			{
				case DataType.Boolean:
					result = type.ValidateBoolean(ft.Name, value);
					break;
				case DataType.Date:
					result = type.ValidateDate(ft.Name, value);
					break;
				case DataType.DateTime:
					result = type.ValidateDateTime(ft.Name, value);
					break;
				case DataType.Decimal:
					result = type.ValidateDecimal(ft.Name, ft.Length, ft.MinimumLength, ft.MaximumLength, value);
					break;
				case DataType.Html:
					result = type.ValidateText(ft.Name, ft.Length, ft.MinimumLength, ft.MaximumLength, ft.Pattern, value);
					break;
				case DataType.Lookup:
					result = type.ValidateList(ft.Name, ft.AllowMultipleValues, value);
					break;
				case DataType.Number:
					result = type.ValidateNumber(ft.Name, ft.Length, ft.MinimumLength, ft.MaximumLength, value);
					break;
				default:
					result = type.ValidateText(ft.Name, ft.Length, ft.MinimumLength, ft.MaximumLength, ft.Pattern, value);
					break;
			}

			if (result.IsValid && string.IsNullOrEmpty(result.CorrectedValue))
			{
				result.CorrectedValue = value;
			}

			return result;
		}

		internal (bool, List<string>) parseFieldAndAddToRow(DataRow row, List<FieldTypeValidation> fieldTypes, Dictionary<string, string> fields)
		{
			var jsonArray = JArray.Parse("[]");
			bool fieldsAreValid = true;
			List<string> validationMessages = [];
			foreach (var key in fields.Keys)
			{
				var ft = fieldTypes.FirstOrDefault(o => o.Name == key.Trim());
				if (ft != null)
				{
					var validationResult = isFieldValid(ft, (fields[key] ?? "").Trim());
					if (validationResult.IsValid)
					{
						var jsonObject = JObject.Parse("{}");

						jsonObject.Add("FieldName", key.Trim());
						jsonObject.Add("FieldValue", validationResult.CorrectedValue);
						jsonObject.Add("FieldTypeID", ft.ID);

						jsonArray.Add(jsonObject);
					}
					else
					{
						fieldsAreValid = false;
						validationMessages.Add(validationResult.Message);
					}
				}
			}
			row["CustomProperties"] = jsonArray.ToString();

			return (fieldsAreValid, validationMessages);
		}

		protected async Task UpdateExecutionWithErrorFromException(ApiExecution execution, Exception ex)
		{
			try
			{
				string message = GetFullExceptionData(ex, false);
				execution.ErrorMessage = message;
				execution.CompletedOn = DateTime.UtcNow;
				using (var connection = ConnectionProvider.Connect())
				{
					await connection.ExecuteAsync($@"
					update api.execution
					set ErrorMessage = @message, CompletedOn = @date
					where executionid = @ExecutionID", new { execution.ExecutionID, message, date = DateTime.UtcNow });
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		protected async Task UpsertApiExecution(ApiExecution execution)
		{
			try
			{
				if (execution.ExecutionID == Guid.Empty)
				{
					execution.ExecutionID = Guid.NewGuid();
				}

				string sql = $@"
				if (@id = 0)
					begin
						insert into api.execution (executionid,resourceid,total,Processed,
						Error,StartedOn,CompletedOn,Fields,ErrorMessage,Method,Route,
						ProcessingStartedOn,State,ApplicationID,RetryCount,Action)
						values (@executionid,@resourceid,@total,@Processed,
						@Error,@StartedOn,@CompletedOn,@Fields,@ErrorMessage,@Method,@Route,
						@ProcessingStartedOn,@State,@ApplicationID,@RetryCount,@Action);
					end
				else
					begin
						update e
						set executionid = @executionid,
						resourceid = @resourceid,
						total = @total,
						Processed = @Processed,
						Error = @Error,
						StartedOn = @StartedOn,
						CompletedOn = @CompletedOn,
						Fields = @Fields,
						ErrorMessage = @ErrorMessage,
						Method = @Method,
						Route = @Route,
						ProcessingStartedOn = @ProcessingStartedOn,
						State = @State,
						ApplicationID = @ApplicationID,
						RetryCount = @RetryCount,
						Action = @Action
						from api.execution e
						where e.id = @id;
					end
				";

				using (var connection = ConnectionProvider.Connect())
				{
					await connection.ExecuteAsync(sql, new
					{
						execution.Id,
						execution.ExecutionID,
						execution.ResourceID,
						execution.Total,
						execution.Processed,
						execution.Error,
						execution.StartedOn,
						execution.CompletedOn,
						execution.Fields,
						execution.ErrorMessage,
						execution.Method,
						execution.Route,
						execution.ProcessingStartedOn,
						execution.State,
						execution.ApplicationId,
						execution.RetryCount,
						execution.Action
					});
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		private bool HasAssetDefaultReadPermission(string type, int id)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{
				bool hasPermission = CurrentUserIsAdmin;
				if (!hasPermission)
				{
					int assetTypeID = connection.Query<int>("select AssetTypeID from Asset where Object = @type and ObjectID = @id", new { type, id }).FirstOrDefault();


					if (assetTypeID <= 0)
					{
						return true; // objects not in asset table we grant permission               
					}

					hasPermission = HasUserReadPermission(type, id, assetTypeID, CurrentUserId);
				}

				return hasPermission;
			}
		}

		public bool HasUserReadPermission(string type, int objectId, int assetTypeId, int resourceId)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{
				Permission permission = Permission.ReadAsset;

				return connection.QuerySingle<bool>($@"	if exists(select 1 
																		 from asset a
																		 cross apply UserAssetPermissionsByAssetID(@r, @t, a.id) ua
																		 where a.Object = @type and a.ObjectID = @id 
																		 and ua.PermissionsBitMask & {(int)permission} = 0)
																	begin
																		select 0;
																		end
																	else
																	begin
																		select 1;
																	end", new { type, id = objectId, t = assetTypeId, r = resourceId });
			}
		}

		private bool HasPermission(int objectId, int assetTypeId, Permission permission)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{
				return connection.QuerySingle<bool>($@"	if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = {(int)permission} and ua.AssetTypeID = @t");
			}
		}

		private bool HasAssetTypeReadPermission(int assetTypeId)
		{
			using (var connection = ConnectionProvider.Connect())
			{
				Permission permission = Permission.ReadAsset;

				return connection.QuerySingle<bool>($@"	if exists(select 1 from UserAssetPermissionsByAssetID(@r,@t,0) ua where ua.PermissionsBitMask & {(int)permission} = 0)
																						begin
																							select 0;
																						end				                                                                        
																						else
																						begin
																							select 1;
																						end", new { t = assetTypeId, r = CurrentUserId });
			}
		}

		public bool HasAssetPermission(string type, int id, Permission permission)
		{
			bool hasPermission = CurrentUserIsAdmin;

			if (!hasPermission)
			{

				if (permission == Permission.ReadAsset)
				{
					hasPermission = HasAssetDefaultReadPermission(type, id);
				}
				else
				{
					int? assetTypeID = null;
					using (var connection = ConnectionProvider.Connect(true))
					{
						if (type.EndsWith("Type"))
						{
							assetTypeID = connection.Query<int>("select ID from AssetType where Object = @type and ObjectID = @id", new { type, id }).SingleOrDefault();

						}
						else
						{
							assetTypeID = connection.Query<int?>("select AssetTypeID from Asset where Object = @type and ObjectID = @id", new { type, id }).SingleOrDefault();
						}

						if (assetTypeID.HasValue)
						{
							hasPermission = HasPermission(id, assetTypeID.Value, permission);
						}
					}
				}
			}

			return hasPermission;
		}

		public bool HasAssetTypePermission(string type, int id, Permission permission)
		{
			using (var connection = ConnectionProvider.Connect())
			{
				bool hasPermission = CurrentUserIsAdmin;
				bool isReadPermission = new List<Permission> { Permission.ReadAsset, Permission.ReadRelationships, Permission.ReadResponsibilities }.Contains(permission);


				if (!hasPermission)
				{
					if (isReadPermission)
					{
						hasPermission = HasAssetTypeReadPermission(id);
					}
					else
					{
						hasPermission = connection.QuerySingle<bool>($@"
																			declare @t int;
																			select @t = ID from AssetType where [Object] = @type and [ObjectID] = @id;

																			if exists(select 1 from UserAssetPermissions(@r,@t) ua where ua.PermissionsBitMask & {(int)permission} = {(int)permission} and ua.AssetTypeID = @t)
																				begin
																					select 1;
																				end				                                                                        
																			else
																				begin
																					select 0;
																				end", new { id, type, r = CurrentUserId });
					}
				}

				return hasPermission;
			}

		}
	}
}

