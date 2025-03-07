using d360.core.entities;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace repositories.azure
{
	public partial class Community: ICommunity
	{
		public async Task<bool> ReadFeatureFlagByTenantAsync(int companyId, string slug)
		{
			bool response;

			using (var connection = Connect(true))
			{
				response = await connection.QueryFirstOrDefaultAsync<bool>(
					"exec ReadFeatureFlagForCompany @companyId, @slug",
					new { companyId, slug }
				);
			}

			return response;
		}

		public async Task<Dictionary<string, bool>> ReadFeatureFlagsByTenantAsync(int companyId)
		{
			Dictionary<string, bool> response = null;

			using (var connection = Connect(true))
			{
				var list = await connection.QueryAsync<dynamic>(
					"exec ReadFeatureFlagsForCompany @companyId", 
					new { companyId }
				);
				response = list.ToDictionary(
					ff => (string)ff.Slug, 
					ff => (bool)ff.Value
				);
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

		public async Task<Dictionary<string, bool>> UpsertFeatureFlagsForTenantAsync(int companyId, string slug, bool value)
		{
			Dictionary<string, bool> response = null;

			using (var connection = Connect(true))
			{
				var list = await connection.QueryAsync<dynamic>(
					"exec UpsertCompanyFeatureFlag @companyId, @slug, @value",
					new { companyId, slug, value }
				);
				response = list.ToDictionary(
					ff => (string)ff.Slug,
					ff => (bool)ff.Value
				);
			}

			return response;
		}
	}
}
