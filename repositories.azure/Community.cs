using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.enums;
using d360.core.helpers;
using d360.core.resources;
using Dapper;
using Dapper.Contrib.Extensions;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class Community: ICommunity
	{
		public string ReadWriteConnectionString { get; set; }
		public string ReadOnlyConnectionString { get; set; }

		public IDbConnection Connect(bool isReadOnly = false)
			=> new SqlConnection(isReadOnly ? ReadOnlyConnectionString : ReadWriteConnectionString);

		public Community(string readWriteConnectionString, string readOnlyConnectionString)
		{
			ReadOnlyConnectionString = readOnlyConnectionString;
			ReadWriteConnectionString = readWriteConnectionString;
		}

		public async Task<RepositoryResponse<bool>> ChangePasswordAsync(int resourceId, string newPassword)
		{
			RepositoryResponse<bool> response = new(400);

			var dbArgs = new DynamicParameters();
			dbArgs.Add("@userId", resourceId);
			dbArgs.Add("@newPassword", PasswordHelper.HashPassword(newPassword));
			using (var connection = Connect())
			{
				int recordsCount = await connection.ExecuteAsync("update Resource set [Password] = @newPassword, [UpdatedOn] = getutcdate() where Id = @userId", dbArgs);
				response.IsSuccess = recordsCount > 0;
				response.Data = response.IsSuccess;
			}
			
			return response;
		}

		public async Task<RepositoryResponse<int>> CreateClaimAsync(ClaimMapping claim)
		{
			RepositoryResponse<int> response = new(400);

			using (var connection = Connect())
			{
				int id = await connection.InsertAsync(claim);
				response.Data = id;
				response.IsSuccess = id > 0;
			}

			return response;
		}

		public async Task<bool> CreateOpenIdRequestAsync(OpenIdRequest request)
		{
			bool success = false;
			using (var connection = Connect())
			{
				int recordsCount = await connection.InsertAsync(request);
				success = recordsCount > 0;
			}
			return success;
		}

		public async Task<RepositoryResponse<int>> CreateUserAsync(Resource user)
		{
			RepositoryResponse<int> response = new(400);
			user.Password = PasswordHelper.HashPassword(user.Password);
			user.UpdatedOn = DateTime.UtcNow;
			using (var connection = Connect())
			{
				int recordsCount = await connection.InsertAsync(user);
				response.Data = user.ID;
				response.IsSuccess = recordsCount > 0;
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> CreateUserInTenantAsync(int companyId, int resourceId, bool isAdministrator, DateTime loggedInOn, AuthenticationMethod authMethod)
		{
			RepositoryResponse<bool> response = new(400);
			var sql = 
				"insert into CompanyResource (CompanyID, ResourceID, IsAdministrator, LastLoggedInOn, [State]) " +
				"values (@companyId, @resourceId, @isAdministrator, @loggedInOn, @state); " +
				"insert into CompanyResourceState (CompanyID, ResourceID, AuthenticationMethod, [Date]) " +
				"values (@companyId, @resourceId, @authMethod, @loggedInOn)";
			using (var connection = Connect())
			{
				int recordsCount = await connection.ExecuteAsync(
					sql, 
					new { 
						companyId, 
						resourceId, 
						isAdministrator, 
						loggedInOn, 
						authMethod = (int)authMethod, 
						state = (int)CompanyResourceState.Active 
					});
				response.Data = recordsCount > 0;
				response.IsSuccess = response.Data;
			}

			return response;
		}

		public async Task<List<UserApiModel>> CreateUsersInTenantAsync(int companyId, List<UserApiModel> users)
		{
			var tbl = new DataTable();
			tbl.Columns.Add("ItemNumber", typeof(int));
			tbl.Columns.Add("ResourceID", typeof(int));
			tbl.Columns.Add("Username", typeof(string));
			tbl.Columns.Add("Email", typeof(string));
			tbl.Columns.Add("FirstName", typeof(string));
			tbl.Columns.Add("LastName", typeof(string));
			tbl.Columns.Add("uid", typeof(Guid));
			tbl.Columns.Add("State", typeof(int));
			tbl.Columns.Add("IsAdministrator", typeof(bool));

			int itemNumber = 0;
			foreach (var user in users)
			{
				itemNumber++;
				user.ItemNumber = itemNumber;

				var row = tbl.NewRow();

				row["ItemNumber"] = itemNumber;
				row["Username"] = user.Username;
				row["Email"] = user.Email;
				row["FirstName"] = user.FirstName;
				row["LastName"] = user.LastName;
				row["IsAdministrator"] = user.IsAdministrator;
				row["State"] = user.State ?? CompanyResourceState.Active;

				if (!user.uid.HasValue || (user.uid.HasValue && user.uid == Guid.Empty))
				{
					user.uid = Guid.NewGuid();
				}

				row["uid"] = user.uid;

				tbl.Rows.Add(row);
			}

			using (var connection = Connect())
			{
				connection.Open();
				using (SqlTransaction trans = ((SqlConnection)connection).BeginTransaction())
				{
					try
					{
						await connection.ExecuteAsync(@"
						create table #Users
						(
							ItemNumber int,
							Username nvarchar(500),
							Email nvarchar(500),
							FirstName nvarchar(250),
							LastName nvarchar(250),

							ResourceID int,
							[uid] uniqueidentifier,
							IsAdministrator bit,
							[State] int
						)", transaction: trans);

						SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)connection, SqlBulkCopyOptions.Default, trans)
						{
							DestinationTableName = "#Users"
						};

						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("Username", "Username");
						bulkCopy.ColumnMappings.Add("Email", "Email");
						bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
						bulkCopy.ColumnMappings.Add("LastName", "LastName");
						bulkCopy.ColumnMappings.Add("IsAdministrator", "IsAdministrator");
						bulkCopy.ColumnMappings.Add("State", "State");
						bulkCopy.ColumnMappings.Add("uid", "uid");

						await bulkCopy.WriteToServerAsync(tbl);

						// NOTE: We need to ensure we are not auto-joining any users we should not be, and resetting their data. Make sure to check if user is already part of client we are on.
						// NOTE: Check to ensure we are not trying to insert duplicate usernames.

						await connection.ExecuteAsync(@"
update  U
set     U.ResourceID = coalesce(R2.ID, R.ID, R3.ID)
from    #Users U
		left join [Resource] R on R.Username = U.Username
		left join [Resource] R2 on R2.[uid] = U.[uid] and U.[uid] != '00000000-0000-0000-0000-000000000000'
		left join [Resource] R3 on R.Email = U.Email;

update  U
set     U.[uid] = R.[uid]
from    #Users U
inner join [Resource] R on R.ID = U.ResourceID
where U.[uid] = '00000000-0000-0000-0000-000000000000';

update  U
set     U.[uid] = newid()
from    #Users U
where U.[uid] = '00000000-0000-0000-0000-000000000000';

update	T
set		T.FirstName = S.FirstName,
		T.LastName = S.LastName,
		T.Email = S.Email
from	[Resource] T
		inner join #Users S on S.ResourceID = T.ID;

insert into [Resource] (Username, [Password], LastName, FirstName, Email, Uid)
	select	Username, '{None}', LastName, FirstName, Email, Uid
	from	#Users
	where	ResourceID is null;

update  U
set     U.ResourceID = R.ID
from    #Users U
		inner join [Resource] R on R.Username = U.Username and U.ResourceID is null;

merge	CompanyResource as T
using	(select * from #Users) as S
on		(T.CompanyID = @companyId and T.ResourceID = S.ResourceID)
when	matched then
update  set
		T.IsAdministrator = S.IsAdministrator,
		T.State = S.State
when	not matched by target then
insert	(CompanyID, ResourceID, IsAdministrator, State)
values	(@companyId, S.ResourceID, S.IsAdministrator, S.State);", 
						new { companyId}, transaction: trans);

						var results = await connection.QueryAsync<dynamic>(@"select * from #Users", transaction: trans);

						foreach (var result in results)
						{
							var user = users.SingleOrDefault(u => u.ItemNumber == result.ItemNumber);
							if (user != null)
							{
								user.ResourceID = result.ResourceID;
								if (user.IsNew)
								{
									user.uid = result.uid;
								}
								else
								{
									user.uid = user.uid == Guid.Empty ? result.uid : user.uid;
								}
								user.CompanyResourceState = CompanyResourceState.Active;
							}
						}

						trans.Commit();
					}
					catch
					{
						try
						{
							if (trans != null)
							{
								trans.Rollback();
							}
						}
						catch
						{
						}

						throw;
					}
				}
			}

			return users;
		}

		public string GenerateOpenIdRequestValue(int length = 5)
		{
			var builder = new StringBuilder(length);

			// Unicode/ASCII Letters are divided into two blocks (Letters 65–90 / 97–122):
			// The first group containing the uppercase letters and the second group containing the lowercase.
			char offset = 'a';
			const int lettersOffset = 26; // A...Z or a..z: length=26

			Random _random = new Random();

			for (var i = 0; i < length; i++)
			{
				var @char = (char)_random.Next(offset, offset + lettersOffset);
				builder.Append(@char);
			}

			return builder.ToString().ToLower();
		}

		public string GetConnectionStringForTenant(int companyId)
		{
			string connectionString = "";
			var dbArgs = new DynamicParameters();
			dbArgs.Add("@companyId", companyId);
			using (var connection = Connect(true))
			{
				var server = connection.QueryFirstOrDefault<DatabaseServer>(
					"select d.* from DatabaseServer d inner join Company c on c.DatabaseServerId = d.Id and c.Id = @companyId", dbArgs, commandTimeout: 10);
				if (server != null)
				{
					connectionString = $"server={server.Server};Database=D3S_{companyId};User ID={server.Username};Password={server.Password};MultipleActiveResultSets=True;ConnectRetryCount=5;ConnectRetryInterval=10;Connection Timeout=180;";
				}
			}

			return connectionString;
		}

		public async Task<OpenIdRequest> GetOpenIdRequestAsync(string state)
		{
			OpenIdRequest model = null;

			var dbArgs = new DynamicParameters();
			dbArgs.Add("@state", state);
			using (var connection = Connect(true))
			{
				model = await connection.QuerySingleAsync<OpenIdRequest>("select * from OpenIdRequest where State = @state", dbArgs);
			}

			return model;
		}

		public async Task<RepositoryResponse<ClaimMapping>> ReadClaimMappingById(int id)
		{
			RepositoryResponse<ClaimMapping> response = new(null, 200, true);

			using (var connection = Connect(true))
			{
				response.Data = await connection.QuerySingleOrDefaultAsync<ClaimMapping>(
					"select * from ClaimMapping where Id = @id",
					new { id }
				);
				if (response.Data == null)
				{
					response.IsSuccess = false;
					response.StatusCode = 404;
					response.Message = "Claim not found based on Id.";
				}
			}

			return response;
		}

		public async Task<OidcAuthenticationSettings> ReadIdpOidcSettingsByTenantPrefix(string prefix)
		{
			OidcAuthenticationSettings response;

			using (var connection = Connect(true))
			{
				response = await connection.QuerySingleOrDefaultAsync<OidcAuthenticationSettings>($@"
select	oidc.*
from	CompanyDomainSetting u
		inner join DomainSetting d on d.ID = u.DomainSettingID 
		cross apply openjson(d.AuthenticationSettings) with (
			baseUri nvarchar(500), 
			discoveryUri nvarchar(500), 
			jwtAuthorityUri nvarchar(500),
			clientId nvarchar(500), 
			clientSecret nvarchar(500), 
			audience nvarchar(500), 
			nameClaimType nvarchar(500), 
			scopesJson nvarchar(max) as json,
			extraParametersJson nvarchar(max) as json
		) oidc
where	u.UrlPrefix = @prefix",
					new { prefix }
				);
			}

			return response;
		}

		public async Task<SamlAuthenticationSettings> ReadIdpSamlSettingsByTenantPrefix(string prefix)
		{
			SamlAuthenticationSettings response;

			using (var connection = Connect(true))
			{
				response = await connection.QuerySingleOrDefaultAsync<SamlAuthenticationSettings>($@"
select	d.HashAlgorithmType,
		d.SignInitialSSORequest,
		d.IdpSsoEndpoint,
		d.IdpSloEndpoint,
		idp.[File] as IdpCertificateFile,
		idp.[Password] as IdpCertificatePassword,
		sp.[File] as SpCertificateFile,
		sp.[Password] as SpCertificatePassword
from	CompanyDomainSetting u
		inner join DomainSetting d on d.ID = u.DomainSettingID 
		left join DomainCertificate idp on idp.ID = d.IdpDomainCertificateID
		left join DomainCertificate sp on sp.ID = d.SpDomainCertificateID
where	u.UrlPrefix = @prefix",
					new { prefix }
				);
			}

			return response;
		}

		public async Task<RepositoryResponse<AuthenticationType>> ReadAuthenticationTypeByTenantUrlAsync(int companyId, string urlPrefix)
		{
			var response = new RepositoryResponse<AuthenticationType>(AuthenticationType.Forms, 200, true, "");

			using (var connection = Connect(true))
			{
				response.Data = await connection.QueryFirstAsync<AuthenticationType>(
						$"select AuthenticationType from CompanyDomainSetting where CompanyID = @companyId and UrlPrefix = @urlPrefix",
						new { companyId, urlPrefix }
					);
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<ClaimMapping>>> ReadClaimsByTenantAsync(int clientId, int companyId, int domainSettingId)
		{
			var response = new RepositoryResponse<IEnumerable<ClaimMapping>>(null, 404, false, "");

			using (var connection = Connect(true))
			{
				response.Data = await connection.QueryAsync<ClaimMapping>(
					$"select * from ClaimMapping where ClientID = 0 and CompanyID = 0 and DomainSettingID = 0 " +
					$"union all " +
					$"select * from ClaimMapping where ClientID = @clientId and CompanyID = 0 and DomainSettingID = 0 " +
					$"union all " +
					$"select * from ClaimMapping where ClientID = @clientId and CompanyID = @companyId and DomainSettingID = 0 " +
					$"union all " +
					$"select * from ClaimMapping where ClientID = @clientId and CompanyID = @companyId and DomainSettingID = @domainSettingId",
					new { clientId, companyId, domainSettingId }
				);
				response.Message = "";
				response.IsSuccess = true;
				response.StatusCode = 200;
			}

			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<CompanyDomainSetting>>> ReadDomainSettingsByTenantAsync(int companyId)
		{
			var response = new RepositoryResponse<IEnumerable<CompanyDomainSetting>>(null, 404, false, "");

			using (var connection = Connect(true))
			{
				response.Data = await connection.QueryAsync<CompanyDomainSetting>(
						$"select * from CompanyDomainSetting where CompanyID = @companyId",
						new { companyId }
					);

				response.Message = "";
				response.IsSuccess = true;
				response.StatusCode = 200;
			}

			return response;
		}

		public async Task<IEnumerable<CompanyDigestExecution>> ReadMostRecentWorkflowDigestStatusBySlotAsync(EnvironmentLevel slot, string region = null)
		{
			IEnumerable<CompanyDigestExecution> response = null;

			string regionJoin = string.IsNullOrEmpty(region) ? "" : "inner join DatabaseServer d on d.ID = e.DatabaseServerID and d.RegionCode = @region";
			string sql = $@"
select	* 
from	CompanyDigestExecution e
		inner join	(
					select	ex.CompanyID,
							max(ex.LastExecuted) as LastExecuted
					from	CompanyDigestExecution ex
							inner join Company e on e.ID = ex.CompanyID and e.EnvironmentLevel = @slot {regionJoin}
					group by ex.CompanyID
					) g on g.CompanyID = e.CompanyID and g.LastExecuted= e.LastExecuted";

			using (var connection = Connect(true))
			{
				response = await connection.QueryAsync<CompanyDigestExecution>(sql, new { slot = (int)slot, region });
			}

			return response;
		}

		public async Task<bool> ReadShouldUserBeAutoAdminByGroupMembershipAsync(int companyId, int domainSettingId, List<string> groups)
		{
			bool response = false;
			
			using (var connection = Connect(true))
			{
				response = await connection.QuerySingleAsync<bool>(
						$"select cast(iif(exists(select 1 from CompanyDomainGroup where CompanyID = @companyId and DomainSettingID = @domainSettingId and GroupName in @groups and IsAdministrator = 1), 1, 0) as bit)",
						new { companyId, domainSettingId, groups }
					);
			}

			return response;
		}

		public async Task<IEnumerable<CompanyWithDatabaseServerSettings>> ReadTenantConnectionSettingsByCurrentSlotAsync(EnvironmentLevel slot, string region = null)
		{
			IEnumerable<CompanyWithDatabaseServerSettings> response = null;

			string sql = @"
select  c.ID as CompanyID, 
        c.ClientID,
        ds.Server, 
        ds.Username, 
        ds.Password,                             
        ds.SearchServer, 
        c.EnvironmentLevel,
        CDS.UrlPrefix,
        c.Priority,
        ds.RegionCode,
		ds.[Region]
from    company c 
        inner join databaseserver ds on c.databaseserverid = ds.id and c.Status = 'Active' 
        inner join CompanyDomainSetting CDS on CDS.CompanyID = c.ID and CDS.IsPrimary = 1
where	c.EnvironmentLevel = @slot" + (string.IsNullOrEmpty(region) ? "" : " and ds.RegionCode = @region");

			using (var connection = Connect(true))
			{
				response = await connection.QueryAsync<CompanyWithDatabaseServerSettings>(sql, new { slot = (int)slot, region });
			}

			return response;
		}

		public async Task<CompanyWithDatabaseServerSettings> ReadTenantConnectionSettingsByIdAsync(int companyId)
		{
			CompanyWithDatabaseServerSettings response = null;

			string sql = @"
select  c.ID as CompanyID, 
        c.ClientID,
        ds.Server, 
        ds.Username, 
        ds.Password,                             
        ds.SearchServer, 
        c.EnvironmentLevel,
        CDS.UrlPrefix,
        c.Priority,
        ds.RegionCode,
		ds.[Region]
from    company c 
        inner join databaseserver ds on c.databaseserverid = ds.id and c.Status = 'Active' 
        inner join CompanyDomainSetting CDS on CDS.CompanyID = c.ID and CDS.IsPrimary = 1
where	c.ID = @companyId";

			using (var connection = Connect(true))
			{
				response = await connection.QueryFirstAsync<CompanyWithDatabaseServerSettings>(sql, new { companyId });
			}

			return response;
		}

		public async Task<CompanyResource> ReadTenantUserAsync(int companyId, int resourceId)
		{
			CompanyResource response = null;

			using (var connection = Connect(true))
			{
				response = await connection.QuerySingleOrDefaultAsync<CompanyResource>(
					$"select * from CompanyResource where CompanyID = @companyId and ResourceID = @resourceId",
					new { companyId, resourceId }
				);
			}

			return response;
		}

		private async Task<RepositoryResponse<Resource>> readUserByIdentifierAsync(string identifierName, string sql, object parameters)
		{
			var response = new RepositoryResponse<Resource>(null, 404, false, $"User not found based on {identifierName} provided.");

			using (var connection = Connect(true))
			{
				response.Data = await connection.QuerySingleOrDefaultAsync<Resource>(sql, parameters);
				if (response.Data != null)
				{
					response.Message = "";
					response.IsSuccess = true;
					response.StatusCode = 200;
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<Resource>> ReadUserByEmailAsync(string email)
		{
			return await readUserByIdentifierAsync("email", $"select * from [Resource] where lower(Email) = @email", new { email });
		}

		public async Task<RepositoryResponse<Resource>> ReadUserByIdAsync(int userId)
		{
			return await readUserByIdentifierAsync("Id", $"select * from [Resource] where ID = @userId", new { userId });
		}

		public async Task<RepositoryResponse<Resource>> ReadUserByUidAsync(Guid userId)
		{
			return await readUserByIdentifierAsync("Uid", $"select * from [Resource] where Uid = @userId", new { userId });
		}

		public async Task<RepositoryResponse<Resource>> ReadUserByUsernameAsync(string username)
		{
			return await readUserByIdentifierAsync("username", $"select * from [Resource] where lower(Username) = @username", new { username });
		}

		public async Task<RepositoryResponse<IEnumerable<Resource>>> ReadUsersByTenantAsync(int companyId, List<int> userIds = null)
		{
			var response = new RepositoryResponse<IEnumerable<Resource>>(null, 404, false, "");

			using (var connection = Connect(true))
			{
				if (userIds == null)
				{
					response.Data = await connection.QueryAsync<Resource>(
						$"select r.* from [Resource] r inner join CompanyResource c on c.ResourceID = r.ID and c.CompanyID = @companyId",
						new { companyId }
					);
				}
				else 
				{
					response.Data = await connection.QueryAsync<Resource>(
						$"select r.* from [Resource] r inner join CompanyResource c on c.ResourceID = r.ID and c.CompanyID = @companyId and c.ResourceID in @userIds",
						new { companyId, userIds }
					);
				}
				response.Message = "";
				response.IsSuccess = true;
				response.StatusCode = 200;
			}

			return response;
		}

		public async Task<ClientUserModel> ReadUserFeatureFlagContext(int companyId, int userId) 
		{
			ClientUserModel model = null;

			using (var connection = Connect(true))
			{
				if (userId == 0)
				{
					model = await connection.QueryFirstAsync<ClientUserModel>(@"
select	C.ID as ClientId,
		C.PublicID as TenantId,
		C.Name as TenantName,
		E.Id as CompanyId,
		0 as ResourceId,
		'no-reply@data3sixty.com' as Email,
		'Govern' as FirstName,
		'Service' as LastName,
		cast(0 as bit) as IsAdministrator
from	Company E
		inner join Client C on C.ID = E.ClientID and E.ID = @companyId",
						new { companyId, userId }
					);
				}
				else 
				{
					model = await connection.QueryFirstAsync<ClientUserModel>(
						@"
select	C.ID as ClientId,
		C.PublicID as TenantId,
		C.Name as TenantName,
		CR.CompanyId,
		CR.ResourceId,
		R.Email,
		R.FirstName,
		R.LastName,
		R.uid as UserId,
		CR.IsAdministrator
from	CompanyResource CR
		inner join [Resource] R on R.ID = CR.ResourceID and CR.CompanyID = @companyId and CR.ResourceID = @userId
		inner join Company E on E.ID = CR.CompanyID
		inner join Client C on C.ID = E.ClientID",
						new { companyId, userId }
					);
				}
			}

			return model;
		}

		public async Task<RepositoryResponse<bool>> RemoveClaimAsync(int claimId, int clientId, int companyId, int domainSettingId)
		{
			var response = new RepositoryResponse<bool>(404);

			using (var connection = Connect())
			{
				var claim = await connection.QuerySingleOrDefaultAsync<ClaimMapping>("select * from ClaimMapping where ID = @claimId;", new { claimId });
				if (claim != null)
				{
					if (claim.ClientId == clientId)
					{
						int rowsAffected = await connection.ExecuteAsync("delete ClaimMapping where ID = @claimId;", new { claimId });
						if (rowsAffected > 0)
						{
							response.StatusCode = 200;
							response.IsSuccess = true;
							response.Message = "Claim removed.";
						}
						else 
						{
							response.StatusCode = 400;
							response.Message = "Not able to remove this claim.";
						}
					}
					else
					{
						response.StatusCode = 403;
						response.Message = "Not allowed to alter this claim.";
					}
				}
				else 
				{
					response.StatusCode = 404;
					response.Message = "Claim not found.";
				}
			}

			return response;
		}

		public async Task<bool> RemoveOpenIdRequestAsync(OpenIdRequest request)
		{
			bool success = false;

			var dbArgs = new DynamicParameters();
			dbArgs.Add("@state", request.State);
			using (var connection = Connect())
			{
				int recordsCount = await connection.ExecuteAsync("delete OpenIdRequest where State = @state", dbArgs);
				success = recordsCount > 0;
			}

			return success;
		}

		public async Task<RepositoryResponse<int>> RemoveUsersFromTenantAsync(int companyId, List<Guid> resourceUids)
		{
			RepositoryResponse<int> response;

			using (var connection = Connect())
			{
				var recordsImpacted = await connection.ExecuteAsync(
					"update	t " +
					"set	t.State = @state " +
					"from	CompanyResource t " +
					"		inner join [Resource] s on s.ID = t.ResourceID and t.CompanyID = @companyId and s.[Uid] in @resourceUids;", 
					new { companyId, resourceUids, state = (int)CompanyResourceState.Deleted }
				);

				response = new(recordsImpacted, 200, true);
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> ResetUserPassword(int resourceId, string currentPassword, string newPassword)
		{
			RepositoryResponse<bool> response = new(200, "");

			if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrWhiteSpace(currentPassword))
			{
				response = new(400, Error.MissingCurrentPasswordParameter);
				return response;
			}

			if (string.IsNullOrEmpty(newPassword) || string.IsNullOrWhiteSpace(newPassword))
			{
				response = new(400, Error.PasswordRule);
				return response;
			}

			currentPassword = currentPassword.Trim();
			newPassword = newPassword.Trim();
			
			if (currentPassword.Equals(newPassword))
			{
				response = new(400, Error.PasswordRule);
				return response;
			}

			var newPasswordHash = PasswordHelper.HashPassword(newPassword);
			var currentPasswordHash =  PasswordHelper.HashPassword(currentPassword);

			using (var connection = Connect())
			{
				var user = await connection.QueryFirstAsync<Resource>("select * from [Resource] where ID = @resourceId;", new { resourceId });

				if (user.Password != currentPasswordHash)
				{
					response = new(400, Error.PasswordRule);
				}
				else 
				{
					await connection.ExecuteAsync("update [Resource] set [Password] = @newPasswordHash where ID = @resourceId", new { resourceId, newPasswordHash });
					response = new(true, 200, true);
				}
			}

			return response;
		}

		public async Task<bool> UpdateClaimAsync(int claimId, ClaimAction action, string path, bool isArray)
		{
			bool response = false;

			using (var connection = Connect())
			{
				response = (await connection.ExecuteAsync(
					"update ClaimMapping set [Action] = @action, [IsArray] = @isArray, [Path] = @path where ID = @claimId", 
					new { claimId, action = (int)action, path, isArray })) > 0;
			}

			return response;
		}

		public async Task<RepositoryResponse<Resource>> UpdateUserApiCredentialsAsync(int userId)
		{
			RepositoryResponse<Resource> response = new(400);
			var sql = @"
declare @apikey nvarchar(25) = '',
		@apisecret nvarchar(50) = ''

select	@apiKey = [dbo].[GenerateAPIKeyWrapper](25),
		@apisecret = [dbo].[GenerateAPIKeyWrapper](50);

update	[Resource]
set		APIPublicKey = @apikey,
		APIPrivateKey = @apisecret
where	ID = @userId

select * from [Resource] where ID = @userId";

			using (var connection = Connect())
			{
				response.Data = await connection.QuerySingleOrDefaultAsync<Resource>(sql, new { userId });
				response.IsSuccess = response.Data != null;
			}

			return response;
		}

		public async Task<RepositoryResponse<int>> UpdateUserAsync(Resource user)
		{
			RepositoryResponse<int> response = new(400);
			user.Password = PasswordHelper.HashPassword(user.Password);
			user.UpdatedOn = DateTime.UtcNow;
			using (var connection = Connect())
			{
				response.IsSuccess = await connection.UpdateAsync(user);
				response.Data = user.ID;
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> UpdateUserInTenantAsync(int companyId, int resourceId, bool isAdministrator, DateTime loggedInOn, AuthenticationMethod authMethod)
		{
			RepositoryResponse<bool> response = new(400);
			var sql =
				"update CompanyResource " +
				"set	IsAdministrator = @isAdministrator, " +
				"		LastLoggedInOn = @loggedInOn " +
				"where	CompanyID = @companyId and ResourceID = @resourceId; ";
			using (var connection = Connect())
			{
				int recordsCount = await connection.ExecuteAsync(
					sql,
					new
					{
						companyId,
						resourceId,
						isAdministrator,
						loggedInOn,
						authMethod = (int)authMethod
					});
				response.Data = recordsCount > 0;
				response.IsSuccess = response.Data;
			}

			return response;
		}

		public async Task UpsertWorkflowDigestStatusAsync(int companyId, Guid invocationId, int? existingId)
		{
			using (var connection = Connect())
			{
				if (existingId.HasValue)
				{
					await connection.ExecuteAsync(
						"update CompanyDigestExecution set InstanceID = @invocationId, LastExecuted = getutcdate() where ID = @id", 
						new { invocationId, id = existingId.Value });
				}
				else
				{
					await connection.ExecuteAsync(
						"insert into CompanyDigestExecution (CompanyID, InvocationID, LastExecuted) values (@companyId, @invocationId, getutcdate())", 
						new { companyId, invocationId });
				}
			}
		}

		public async Task<Resource> ValidateResourceAsync(string username, string password, int? companyId)
		{
			Resource model = null;

			var dbArgs = new DynamicParameters();
			dbArgs.Add("@username", username);
			dbArgs.Add("@password", PasswordHelper.HashPassword(password));
			using (var connection = Connect())
			{
				model = await connection.QuerySingleAsync<Resource>("select * from [Resource] where Username = @username and [password] = @password", dbArgs);
				if (model != null && companyId.HasValue)
				{
					dbArgs = new DynamicParameters();
					dbArgs.Add("@companyId", companyId.Value);
					dbArgs.Add("@resourceId", model.ID);
					CompanyResource companyResource = await connection.QuerySingleAsync<CompanyResource>("select * from CompanyResource where CompanyId = @companyId and ResourceId = @resourceId", dbArgs);
					if (companyResource != null)
					{
						if (companyResource.State == d360.core.enums.CompanyResourceState.Active)
						{
							await connection.ExecuteAsync("update CompanyResource set LastLoggedInOn = getutcdate() where ResourceID = @resourceId and CompanyID = @companyId", dbArgs);
						}
						else //User no longer active in company.
						{
							model = null;
						}
					}
					else // Resource Not assigned to this company.
					{
						model = null;
					}
				}
			}

			return model;
		}
	}
}
