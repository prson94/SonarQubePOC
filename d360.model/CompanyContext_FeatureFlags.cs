using System;
using System.Linq;
using LaunchDarkly;

namespace d360.model
{
	public class ClientUserModel
	{
		public Guid TenantId { get; set; }

		public string TenantName { get; set; }

		public Guid UserId { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }

		public string Email { get; set; }

		public bool IsAdministrator { get; set; }
	}

	public partial class CompanyContext : BaseContext, ICompanyContext
	{
		private ClientUserModel GetFeatureFlagUser()
		{
			return this.Community.Query<ClientUserModel>(@"
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
																	inner join Client C on C.ID = E.ClientID",
																new { this.Community.CurrentCompanyID, this.Community.CurrentResourceID }).FirstOrDefault();
		}

		public LaunchDarkly.Sdk.User GetSdkFeatureFlagUser()
		{
			var itemKey = $"{this.Community.CurrentClientID}.{this.Community.CurrentResourceID}";
			var userModel = GetFeatureFlagUser();

			var b = LaunchDarkly.Sdk.User.Builder(itemKey);

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
