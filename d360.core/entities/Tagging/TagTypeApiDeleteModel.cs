using System;
using System.Runtime.Serialization;

namespace d360.core.entities
{
	public class TagTypeApiDeleteModel
	{
		[DataMember]
		public Guid uid { get; set; }

		[DataMember]
		public bool cascade { get; set; }
	}
}
