using d360.core.enums;
using System;
using System.Runtime.Serialization;

namespace d360.core.entities
{
	public class BulkTagAsset
	{
		[DataMember]
		public Guid AssetUid { get; set; }

		[DataMember]
		public string Tag { get; set; }

		[DataMember]
		public BulkTagOperation Action { get; set; } = BulkTagOperation.Append;

	}
}
