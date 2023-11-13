namespace d360.core.search
{
	public class SearchResultsModel
	{
		public int took { get; set; }

		public bool timed_out { get; set; }

		public SearchResultsShardModel _shards { get; set; }

		public SearchResultsHitsModel hits { get; set; }

		public SearchAggregationsModel aggregations { get; set; }
	}
}
