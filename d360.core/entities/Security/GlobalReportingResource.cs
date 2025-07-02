using d360.core.enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
	[DataContract, Table("Global_Resource", Schema = "reporting")]
	public class GlobalReportingResource : BaseObject
	{
		[DataMember, Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public int ResourceID { get; set; }

		[DataMember]
		public string FirstName { get; set; }

		[DataMember]
		public string LastName { get; set; }

		[DataMember]
		public DateTime? LastLoggedInOn { get; set; }

		[DataMember]
		public string Email { get; set; }

		[DataMember]
		public CompanyResourceState State { get; set; }

		[DataMember]
		public bool IsAdministrator { get; set; }

		[DataMember]
		public DateTime? CreatedOn { get; set; }

		[DataMember]
		public DateTime? UpdatedOn { get; set; }

		[DataMember]
		public Guid Uid { get; set; }

		[DataMember, NotMapped]
		public string FullName => FirstName + " " + LastName;

		#region Deprecated

		[NotMapped, DataMember]
		public DateTime? DateLastLoggedIn { get; set; }

		[NotMapped, DataMember]
		public string Status { get; set; }

		#endregion
	}
}
