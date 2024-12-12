using System;

namespace d360.core.entities.ChangeLog
{
	public class ChangeLog
	{
		public long Id { get; set; }
		public int? AssetTypeId { get; set; }
		public int? IntersectTypeId { get; set; }
		public int? PredicateId { get; set; }
		public long? AssetId { get; set; }
		public int? GroupId { get; set; }
		public int? ResoureId { get; set; }
		public int? TagId { get; set; }
		public int ChangedBy { get; set; }
		public DateTime ChangedOn { get; set; }
		public ChangeLogObject ChangeObject { get; set; }
		public ChangeLogAction ChangeAction { get; set; }
		public string Changes { get; set; }
		public int Version { get; set; }
	}
}
