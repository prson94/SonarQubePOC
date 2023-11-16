using System.Collections.Generic;

namespace d360.core.search
{
	public class SearchResultsHitsModel
	{
		public int total { get; set; }

		public float? max_score { get; set; }

		public List<SearchResultsHitModel> hits { get; set; }
	}
}
