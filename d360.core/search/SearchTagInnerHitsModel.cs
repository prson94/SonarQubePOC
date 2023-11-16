using Newtonsoft.Json.Linq;

namespace d360.core.search 
{ 
	public class SearchTagInnerHitsModel
	{
		public IndexTag _source { get; set; }

		public JObject highlight { get; set; }

		public string GetHighLightValue()
		{
			if (highlight != null && highlight.TryGetValue(constants.D3S_FIELD_PREFIX + "Tags.Value", out var jToken))
			{
				if (jToken.Type == JTokenType.Array)
				{
					return ((JArray)jToken)[0].Value<string>();
				}
				return jToken.Value<string>();
			}
			return null;
		}
	}
}
