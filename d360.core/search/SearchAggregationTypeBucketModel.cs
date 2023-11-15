namespace d360.core.search
{
	public class SearchAggregationTypeBucketModel
	{
		public int doc_count { get; set; }

		public string key { get; set; }

		public SearchAggregationCategoryTypeModel category { get; set; }
	}
}
