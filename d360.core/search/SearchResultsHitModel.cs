using Newtonsoft.Json.Linq;

namespace d360.core.search
{
	public class SearchResultsHitModel
	{
		public string _index { get; set; }

		public string _type { get; set; }

		public string d3sCategory
		{
			get
			{
				if (_source != null)
				{
					var jToken = _source.SelectToken("d3s.Category");
					return jToken?.Value<string>();
				}
				return null;
			}
		}

		public string _id { get; set; }

		public float _score { get; set; }

		public JObject _source { get; set; }

		public JObject highlight { get; set; }

		public JObject inner_hits { get; set; }

		public JObject _explanation { get; set; }
	}
}
