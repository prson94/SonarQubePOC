using System;

namespace igx.jobs.indexer.models
{
	internal class TagSqlModel : IPagedQuerySqlModel
	{
		public long AssetID { get; set; }
		public Guid AssetUID { get; set; }
		public Guid TagUID { get; set; }
		public string Value { get; set; }
	}
}
