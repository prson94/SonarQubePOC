using d360.model;
using System;
using System.Collections.Generic;
using System.Text;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using System.Linq;

namespace d360.web.Models
{
    public static class FeatureFlagExtensions
    {
        public static User GetFeatureFlagUser(this ICommunityContext ctx)
        {
            var listKey = "ClientUserModels";
            var itemKey = $"{ctx.CurrentClientID}.{ctx.CurrentResourceID}";
            var userModel = ctx.GetItemInCachedList<ClientUserModel>(listKey, itemKey);
            if (userModel == null)
            {
                userModel = ctx.Query<ClientUserModel>(@"
select	C.PublicID as TenantId,
		C.Name as TenantName,
		R.Email,
		R.FirstName,
		R.LastName,
		R.uid as UserId,
		CR.IsAdministrator
from	CompanyResource CR
		inner join [Resource] R on R.ID = CR.ResourceID and CR.CompanyID = @CurrentCompanyID and CR.ResourceID = @CurrentResourceID
		inner join Company E on E.ID = CR.CompanyID
		inner join Client C on C.ID = E.ClientID", new { ctx.CurrentCompanyID, ctx.CurrentResourceID }).FirstOrDefault();

                if (userModel != null)
                {
                    ctx.AddItemToCachedList(listKey, itemKey, userModel);
                }
            }

            var b = User.Builder(itemKey);
            if (userModel != null)
            {
                b.FirstName(userModel.FirstName)
                    .LastName(userModel.LastName)
                    .Email(userModel.Email)
                    .Custom("tenantId", userModel.TenantId.ToString())
                    .Custom("tenantName", userModel.TenantName);
            }

            return b.Build();
        }
    }
}
