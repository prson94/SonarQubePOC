namespace igx.jobs.indexer.models
{
	internal class ResponsibilitySqlModel : IPagedQuerySqlModel
	{
		public long AssetID { get; set; }
		public string SecurityAsset { get; set; }
		public int SecurityAssetID { get; set; }
	}
}
