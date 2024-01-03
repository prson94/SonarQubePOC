using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using d360.core.entities;
using d360.core.helpers;
using Dapper.Contrib.Extensions;

namespace repositories.azure
{
	public class Community: ICommunity
	{
		public string ConnectionString { get; set; }
		public IDbConnection Connect()
			=> new SqlConnection(ConnectionString);

		public Community(string connectionString)
		{
			ConnectionString = connectionString;
		}

		public async Task<bool> ChangePasswordAsync(int resourceId, string newPassword)
		{
			bool success = false;
			var dbArgs = new DynamicParameters();
			dbArgs.Add("@userId", resourceId);
			dbArgs.Add("@newPassword", PasswordHelper.HashPassword(newPassword));
			using (var connection = Connect())
			{
				int recordsCount = await connection.ExecuteAsync("update Resource set [Password] = @newPassword, [UpdatedOn] = getutcdate() where Id = @userId", dbArgs);
				success = recordsCount > 0;
			}
			return success;
		}

		public async Task<Group> CreateGroupInTenantAsync(int companyId, Group group)
		{
			throw new NotImplementedException();
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

		public async Task<string> GetConnectionStringForTenantAsync(int companyId)
		{
			string connectionString = "";
			var dbArgs = new DynamicParameters();
			dbArgs.Add("@companyId", companyId);
			using (var connection = Connect())
			{
				var server = await connection.QueryFirstOrDefaultAsync<DatabaseServer>("select d.* from DatabaseServer d inner join Company c on c.DatabaseServerId = d.Id and c.Id = @companyId", dbArgs);
				if (server != null)
				{
					connectionString = $"server={server.Server};Database=D3S_{companyId};User ID={server.Username};Password={server.Password};MultipleActiveResultSets=True;ConnectRetryCount=5;ConnectRetryInterval=10;Connection Timeout=180;";
				}
			}

			return connectionString;
		}

		public async Task<IEnumerable<Group>> GetGroupsByTenantAsync(int companyId)
		{
			throw new NotImplementedException();
		}

		public async Task<OpenIdRequest> GetOpenIdRequestAsync(string state)
		{
			OpenIdRequest model = null;

			var dbArgs = new DynamicParameters();
			dbArgs.Add("@state", state);
			using (var connection = Connect())
			{
				model = await connection.QuerySingleAsync<OpenIdRequest>("select * from OpenIdRequest where State = @state", dbArgs);
			}

			return model;
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

		public async Task<Resource> ValidateResourceAsync(string username, string password, int? companyId)
		{
			Resource model = null;

			var dbArgs = new DynamicParameters();
			dbArgs.Add("@username", username);
			dbArgs.Add("@password", PasswordHelper.HashPassword(password));
			using (var connection = Connect())
			{
				model = await connection.QuerySingleAsync<Resource>("select * from [Resource] where Username = @username and [assword] = @password", dbArgs);
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
							companyResource.LastLoggedInOn = DateTime.UtcNow;
							await connection.UpdateAsync(companyResource);
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
