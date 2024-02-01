using d360.core.entities;
using System;
using System.Linq;

namespace d360.model
{
	public partial interface ICompanyContext
	{
		ClientUserModel GetFeatureFlagUser();
	}

	public partial class CompanyContext : BaseContext, ICompanyContext
	{
		public ClientUserModel GetFeatureFlagUser()
		{
			var listKey = "ClientUserModels";
			var itemKey = $"{CurrentClientID}.{CurrentCompanyID}.{CurrentResourceID}";
			var userModel = Community.GetItemInCachedList<ClientUserModel>(listKey, itemKey);

			if (userModel == null)
			{
				string sql;
				if (CurrentResourceID == 0)
				{
					sql = @"
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
		inner join Client C on C.ID = E.ClientID and E.ID = @CurrentCompanyID";
				}
				else
				{
					sql = @"
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
		inner join [Resource] R on R.ID = CR.ResourceID and CR.CompanyID = @CurrentCompanyID and CR.ResourceID = @CurrentResourceID
		inner join Company E on E.ID = CR.CompanyID
		inner join Client C on C.ID = E.ClientID";
				}

				userModel = Community.Query<ClientUserModel>(sql, new { CurrentCompanyID, CurrentResourceID }).FirstOrDefault();
				if (userModel != null)
				{
					Community.AddItemToCachedList(listKey, itemKey, userModel);
				}
			}

			return userModel;
		}
	}
}
