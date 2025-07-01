using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.enums;
using d360.core.helpers;
using d360.core.resources;
using Dapper;
using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace repositories.azure
{
	public partial class Community: ICommunity
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

		public async Task<RepositoryResponse<int>> CreateUserAsync(Resource user)
		{
			RepositoryResponse<int> response = new(400);
			user.APIPublicKey = GenerateRandomString(25);
			user.APIPrivateKey = GenerateRandomString(50);
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
			var sql = @"
if exists(select 1 from CompanyResource where CompanyId = @companyId and ResourceId = @resourceId)
begin
	update	CompanyResource
	set		IsAdministrator = @isAdministrator,
			LastLoggedInOn = @loggedInOn,
			State = @state
	where	CompanyID = @companyId and ResourceID = @resourceId;
end
else
begin
	insert into CompanyResource (CompanyID, ResourceID, IsAdministrator, LastLoggedInOn, [State])
	values (@companyId, @resourceId, @isAdministrator, @loggedInOn, @state);
end
insert into CompanyResourceLog (CompanyID, ResourceID, AuthenticationMethod, [Date])
values (@companyId, @resourceId, @authMethod, @loggedInOn);";

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

		public string GenerateRandomString(int length = 5)
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

		public async Task<List<UserApiModel>> GetUsersInTenantAsync(int companyId, List<UserApiModel> users)
		{
			var tbl = new DataTable();
			tbl.Columns.Add("ItemNumber", typeof(int));
			tbl.Columns.Add("ResourceID", typeof(int));
			tbl.Columns.Add("Username", typeof(string));
			tbl.Columns.Add("Email", typeof(string));
			tbl.Columns.Add("uid", typeof(Guid));

			int itemNumber = 0;
			foreach (var user in users)
			{
				var row = tbl.NewRow();
				itemNumber++;
				user.ItemNumber = itemNumber;
				row["ItemNumber"] = itemNumber;
				row["Username"] = user.Username;
				row["Email"] = user.Email;
				row["uid"] = (user.uid != null) ? user.uid : Guid.Empty;

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
							ResourceID int,
							[uid] uniqueidentifier
						)", transaction: trans);

						SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)connection, SqlBulkCopyOptions.Default, trans)
						{
							DestinationTableName = "#Users"
						};

						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("Username", "Username");
						bulkCopy.ColumnMappings.Add("Email", "Email");
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
",
						new { companyId }, transaction: trans);

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

		public async Task<List<UserUpsertValidateModel>> CreateUsersInTenantAsync(int companyId, List<UserUpsertValidateModel> users)
		{
			var tbl = new DataTable();
			tbl.Columns.Add("ItemNumber", typeof(int));
			tbl.Columns.Add("ResourceID", typeof(int));
			tbl.Columns.Add("Username", typeof(string));
			tbl.Columns.Add("Email", typeof(string));
			tbl.Columns.Add("FirstName", typeof(string));
			tbl.Columns.Add("LastName", typeof(string));
			tbl.Columns.Add("Password", typeof(string));
			tbl.Columns.Add("uid", typeof(Guid));
			tbl.Columns.Add("State", typeof(int));
			tbl.Columns.Add("IsAdministrator", typeof(bool));
			tbl.Columns.Add("Success", typeof(bool));
			tbl.Columns.Add("Message", typeof(string));

			foreach (var user in users)
			{
				string password = user.users.Password ?? string.Empty;

				if (!string.IsNullOrEmpty(password))
				{
					password = PasswordHelper.HashPassword(password);
				}

				if (user.Success ?? true)
				{
					var row = tbl.NewRow();
					row["ItemNumber"] = user.users.ItemNumber;
					row["Username"] = user.users.Username ?? (object)DBNull.Value;
					row["Email"] = user.users.Email ?? (object)DBNull.Value;
					row["FirstName"] = user.users.FirstName ?? (object)DBNull.Value;
					row["LastName"] = user.users.LastName ?? (object)DBNull.Value;
					row["Password"] = password ?? (object)DBNull.Value;
					row["ResourceID"] = user.users.ResourceID ?? (object)DBNull.Value;
					row["IsAdministrator"] = user.users.IsAdministrator;
					row["State"] = user.users.State ?? CompanyResourceState.Active;
					row["uid"] = user.users.uid ?? (object)DBNull.Value;
					row["Success"] = user.Success ?? (object)DBNull.Value;
					row["Message"] = user.Message;

					tbl.Rows.Add(row);
				}
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
							Password nvarchar(250),
							ResourceID int,
							[uid] uniqueidentifier,
							IsAdministrator bit,
							[State] int,
							[Success] bit,
							[Message] varchar(4000)
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
						bulkCopy.ColumnMappings.Add("Password", "Password");
						bulkCopy.ColumnMappings.Add("ResourceID", "ResourceID");
						bulkCopy.ColumnMappings.Add("IsAdministrator", "IsAdministrator");
						bulkCopy.ColumnMappings.Add("State", "State");
						bulkCopy.ColumnMappings.Add("uid", "uid");
						bulkCopy.ColumnMappings.Add("Success", "Success");
						bulkCopy.ColumnMappings.Add("Message", "Message");

						await bulkCopy.WriteToServerAsync(tbl);

						// NOTE: We need to ensure we are not auto-joining any users we should not be, and resetting their data. Make sure to check if user is already part of client we are on.
						// NOTE: Check to ensure we are not trying to insert duplicate usernames.

						await connection.ExecuteAsync(@"

update	S
set		S.Success = 0,
		S.Message = 'Email already exists for different user'
from	#Users S
		inner join [Resource] T on S.Email = T.Email
where coalesce(S.Success,1) = 1 and coalesce(S.ResourceID,0) != T.ID;

update	S
set		S.Success = 0,
		S.Message = 'Username already exists for different user'
from	#Users S
		inner join [Resource] T on S.Username = T.Username
where coalesce(S.Success,1) = 1 and coalesce(S.ResourceID,0) != T.ID;

update	T
set		T.FirstName = S.FirstName,
		T.LastName = S.LastName,
		T.Email = S.Email
from	[Resource] T
		inner join #Users S on S.ResourceID = T.ID
where coalesce(S.Success,1) = 1;

insert into [Resource] (Username, [Password], LastName, FirstName, Email, Uid)
	select	Username, [Password], LastName, FirstName, Email, Uid
	from	#Users
	where	ResourceID is null and coalesce(Success,1) = 1;

update  U
set     U.ResourceID = R.ID
from    #Users U
		inner join [Resource] R on R.Username = U.Username and U.ResourceID is null
where  coalesce(U.Success,1) = 1;

if exists(select 1 
		 from [Resource] r 
		 inner join #Users S on s.ResourceID = r.ID
		 left join CompanyResource cr on r.ID = cr.ResourceID and cr.CompanyID = @companyId 
		 where coalesce(cr.state,3) = 3 and S.State = 1 and coalesce(s.Password,'') != '')
begin
		update r
		set r.Password = s.Password
		from [Resource] r 
		inner join #Users S on s.ResourceID = r.ID
		left join CompanyResource cr on r.ID = cr.ResourceID and cr.CompanyID = @companyId 
		where coalesce(cr.state,3) = 3 and S.State = 1 and coalesce(s.Password,'') != '';
end

if exists(select 1 from #Users T group by T.ResourceID having count(1)>1)
begin
	update	T
	set		T.IsAdministrator = S.IsAdministrator,
			T.State = S.State
	from CompanyResource T
	inner join #Users S on T.ResourceID = S.ResourceID 
	where T.CompanyID = @companyId and coalesce(S.Success,1) = 1;

	insert	into CompanyResource(CompanyID, ResourceID, IsAdministrator, State)
	select distinct @companyId, S.ResourceID, S.IsAdministrator, S.State
	from #Users S
	left join CompanyResource T on T.CompanyID = @companyId and T.ResourceID = S.ResourceID 
	where T.CompanyID is null and coalesce(S.Success,1) = 1;
end
else
begin
	merge	CompanyResource as T
	using	(select * from #Users where coalesce(Success,1) = 1) as S
	on		(T.CompanyID = @companyId and T.ResourceID = S.ResourceID)
	when	matched then
	update  set
			T.IsAdministrator = S.IsAdministrator,
			T.State = S.State
	when	not matched by target then
	insert	(CompanyID, ResourceID, IsAdministrator, State)
	values	(@companyId, S.ResourceID, S.IsAdministrator, S.State);
end

",
						new { companyId }, transaction: trans);

						var results = await connection.QueryAsync<dynamic>(@"select * from #Users", transaction: trans);

						foreach (var result in results)
						{
							var user = users.SingleOrDefault(u => u.users.ItemNumber == result.ItemNumber);
							if (user != null)
							{
								user.users.ResourceID = result.ResourceID;
								if (user.users.IsNew)
								{
									user.users.uid = result.uid;
								}
								else
								{
									user.users.uid = user.users.uid == Guid.Empty ? result.uid : user.users.uid;
								}
								user.users.CompanyResourceState = CompanyResourceState.Active;
								if (user.Success ?? true)
								{
									bool isSuccess = result.Success ?? true;
									if (isSuccess == false)
									{
										user.Success = false;
										user.Message = result?.Message ?? "User record not validate";
									}
								}
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

		public async Task<RepositoryResponse<IEnumerable<dynamic>>> ReadLoginsByTenantAsync(int companyId, long startId = 0)
		{
			RepositoryResponse<IEnumerable<dynamic>> response = new(200);

			string sql = $@"
select	top 250
		l.Id,
		l.[Date],
		l.AuthenticationMethod,
		r.Uid as ResourceUid,
		r.FirstName + ' ' + r.LastName as ResourceFullName
from	CompanyResourceLog l
		inner join [Resource] r on r.ID = l.ResourceId and l.CompanyID = @companyId and l.Id > @startId
order by l.Id asc";

			using (var connection = Connect(true))
			{
				response.Data = await connection.QueryAsync<dynamic>(sql, new { companyId, startId });
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

		private async Task<RepositoryResponse<Resource>> readUserByIdentifierAsync(string identifierName, string sql, object parameters, bool fromSecondary)
		{
			var response = new RepositoryResponse<Resource>(null, 404, false, $"User not found based on {identifierName} provided.");

			using (var connection = Connect(fromSecondary))
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

		public async Task<RepositoryResponse<Resource>> ReadUserByEmailAsync(string email, bool fromSecondary = true)
		{
			return await readUserByIdentifierAsync("email", $"select * from [Resource] where lower(Email) = @email", new { email }, fromSecondary);
		}

		public async Task<RepositoryResponse<Resource>> ReadUserByIdAsync(int userId, bool fromSecondary = true)
		{
			return await readUserByIdentifierAsync("Id", $"select * from [Resource] where ID = @userId", new { userId }, fromSecondary);
		}

		public async Task<RepositoryResponse<Resource>> ReadUserByUidAsync(Guid userId, bool fromSecondary = true)
		{
			return await readUserByIdentifierAsync("Uid", $"select * from [Resource] where Uid = @userId", new { userId }, fromSecondary);
		}

		public async Task<RepositoryResponse<Resource>> ReadUserByUsernameAsync(string username,  bool fromSecondary = true )
		{
			return await readUserByIdentifierAsync("username", $"select * from [Resource] where lower(Username) = @username", new { username }, fromSecondary);
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

		public async Task<Resource> ReadUsersByTenantFromIDAsync(int companyId, int userid)
		{
			using (var connection = Connect(true))
			{
				var resource = await connection.QueryAsync<Resource>(
					$"select r.* from [Resource] r inner join CompanyResource c on c.ResourceID = r.ID and c.CompanyID = @companyId and c.ResourceID = @userid",
					new { companyId, userid }
				);
				if (resource == null)
				{
					return null;
				}
				else
				{
					return resource.ToList().FirstOrDefault();
				}

			}
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

			if (!string.IsNullOrEmpty(newPassword))
			{
				if (string.IsNullOrEmpty(newPassword)
					|| newPassword.Length < 7 || newPassword.Length > 25
					|| !newPassword.Any(char.IsUpper) || !newPassword.Any(char.IsLower)
					|| !newPassword.Any(char.IsDigit))
				{
					response = new(400, Error.PasswordRule);
					return response;
				}
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
				"		LastLoggedInOn = @loggedInOn," +
				"		State = 1 " +
				"where	CompanyID = @companyId and ResourceID = @resourceId; " +
				"insert into CompanyResourceLog (CompanyID, ResourceID, AuthenticationMethod, [Date]) " +
				"values (@companyId, @resourceId, @authMethod, @loggedInOn)";
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
						"insert into CompanyDigestExecution (CompanyID, InstanceID, LastExecuted) values (@companyId, @invocationId, getutcdate())", 
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
				model = await connection.QueryFirstOrDefaultAsync<Resource>("select * from [Resource] where Username = @username and [password] = @password", dbArgs);
				if (model != null && companyId.HasValue)
				{
					dbArgs = new DynamicParameters();
					dbArgs.Add("@companyId", companyId.Value);
					dbArgs.Add("@resourceId", model.ID);
					CompanyResource companyResource = await connection.QueryFirstOrDefaultAsync<CompanyResource>("select * from CompanyResource where CompanyId = @companyId and ResourceId = @resourceId", dbArgs);
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
