namespace igx.jobs.indexer.models
{
	internal class FieldSqlModel : IPagedQuerySqlModel
	{
		public long AssetID { get; set; }
		public string Name { get; set; }
		public string FormattedValue { get; set; }
	}
}
