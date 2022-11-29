using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text;

namespace d360.core.entities
{
	[DataContract(Namespace = NAMESPACE)]
	public class ResourceSetting : BaseObject
	{
		[DataMember]
		[Key]
		[Column(Order = 1)]
		public int ResourceID { get; set; }

		[DataMember]
		[Key]
		[Column(Order = 2)]
		public int AssetTypeID { get; set; }

		[DataMember]
		[Key]
		[Column(Order = 3)]
		public string Setting { get; set; }

		[DataMember]
		public string Value { get; set; }
	}
}
