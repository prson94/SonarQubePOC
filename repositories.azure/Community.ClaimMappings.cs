using d360.core.entities;
using d360.core.enums;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories.azure
{
	public partial class Community: ICommunity
	{
		public async Task<RepositoryResponse<int>> CreateClaimAsync(ClaimMapping claim)
		{
			RepositoryResponse<int> response = new(400);

			using (var connection = Connect())
			{
				string sql = $@"
								declare @id int;

								declare @id_tbl table
								(id int);

								insert into ClaimMapping (ClientID, CompanyID, DomainSettingID, AuthenticationType, ClaimType, [Path], IsArray, [Action]) 
								OUTPUT INSERTED.ID into @id_tbl
								values (@ClientID, @CompanyID, @DomainSettingID, @AuthenticationType, @ClaimType, @Path, @IsArray, @Action);
								select top 1 id from @id_tbl;
								";
				int id = await connection.QueryFirstOrDefaultAsync<int>(sql,
				new
				{
					claim.ClientId,
					claim.CompanyId,
					claim.DomainSettingId,
					AuthenticationType = (int)claim.AuthenticationType,
					ClaimType = (int)claim.ClaimType,
					claim.Path,
					claim.IsArray,
					Action = (int)claim.Action
				});
				response.Data = id;
				response.IsSuccess = id > 0;
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
	}
}
