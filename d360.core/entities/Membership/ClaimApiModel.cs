using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace d360.core.entities.Membership
{
	public class ClaimPostApiModel
	{
		public ClaimLocation Location { get; set; }
		public ClaimType ClaimType { get; set; }
		public bool IsArray { get; set; }
		public string Path { get; set; }
		public ClaimAction Action { get; set; }
	}

	public class ClaimPutApiModel
	{
		public bool IsArray { get; set; }
		public string Path { get; set; }
		public ClaimAction Action { get; set; }
	}


	public class ClaimApiViewModel
	{
		public int Id { get; set; }
		public ClaimLocation Location { get; set; }
		public ClaimType ClaimType { get; set; }
		public bool IsArray { get; set; }
		public string Path { get; set; }
		public ClaimAction Action { get; set; }
	}

	public class SettingValuesForWorkflow
	{
		[DataMember]
		public string defaultGroup { get; set; } = "0";

		[DataMember]
		public string fromName { get; set; }= string.Empty;

		[DataMember]
		public string fromEmail { get; set; }=string.Empty;
	}

}
