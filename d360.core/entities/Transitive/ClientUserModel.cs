using System;

namespace d360.core.entities
{
	/// <summary>
	/// Used by FeatureFlag services.
	/// </summary>
	public class ClientUserModel
	{
		public string Key { get { return $"{ClientId}.{CompanyId}.{ResourceId}"; } }

		public int ClientId { get; set; }	

		public int CompanyId { get; set; }

		public Guid TenantId { get; set; }

		public string TenantName { get; set; }

		public Guid UserId { get; set; }

		public int ResourceId { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }

		public string Email { get; set; }

		public bool IsAdministrator { get; set; }
	}
}
