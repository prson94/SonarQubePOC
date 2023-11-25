namespace d360.extensions.search.models
{
	internal class ResponsibilitySqlModel : IPagedQuerySqlModel
	{
		public long AssetID { get; set; }

		public string SecurityAsset { get; set; }

		public int SecurityAssetID { get; set; }
	}
}
