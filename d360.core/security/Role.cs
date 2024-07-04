using Newtonsoft.Json;
using System;

namespace d360.core.security
{
	/// <summary>
	/// The database class
	/// </summary>
	public class Role
	{
		public int Id { get; set; }
		public Guid Uid { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public int Permissions { get; set; }
		public int CreatedBy { get; set; }
		public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
		public int UpdatedBy { get; set; }
		public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
	}

	public class CreateRole
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("permissions")]
		public int Permissions { get; set; }
	}

	public class ReadRole: CreateRole
	{
		[JsonProperty("uid")]
		public Guid Uid { get; set; }

		[JsonProperty("updatedOn")]
		public DateTime UpdatedOn { get; set; }
	}
}
