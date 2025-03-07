using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace d360.core.entities
{
	public class SearchResultAssetField
	{
		public string Name { get; set; }

		public string Type { get; set; }

		public string Label { get; set; }

		public string Prefix { get; set; }

		public string Suffix { get; set; }

		public string Value { get; set; }

		public bool Empty { get; set; }
	}

	public class SearchResultAssetPathSegment
	{
		public List<string> Key { get; set; }

		public string AssetType { get; set; }
	}

	public class SearchResultAssetScore 
	{
		public ScoreType ScoreType { get; set; }
		public int LowerThreshold { get; set; }
		public int UpperThreshold { get; set; }
		public decimal Value { get; set; }
	}

	public class SearchResult
	{
		public string AbsoluteUrl { get; set; }
		public string Url { get; set; }
		public Guid	Uid { get; set; }
		public string Name { get; set; }
		public string DisplayName { get { return Name; } }
		public string Type { get; set; }
		public Guid AssetTypeUid { get; set; }
		public string Icon { get; set; }
		public bool HasProfiling { get; set; }

		public string Object { get; set; }
		public int ObjectId { get; set; }
		public string ID { get; set; }

		[JsonIgnore]
		public AssetTypeClass Class { get; set; }
		public string Group { get { return Class.AsInfoModel().Name; } }

		[JsonIgnore]
		public string _AssetPath { get; set; }
		public List<SearchResultAssetPathSegment> AssetPath { 
			get {
				try
				{
					return JsonConvert.DeserializeObject<List<SearchResultAssetPathSegment>>(_AssetPath ?? "[]");
				}
				catch
				{
					return [];
				}
			} 
		}

		[JsonIgnore]
		public string _Fields { get; set; }
		public List<SearchResultAssetField> Fields { get { return JsonConvert.DeserializeObject<List<SearchResultAssetField>>(_Fields ?? "[]"); } }


		[JsonIgnore]
		public string _Scores { get; set; }
		public List<SearchResultAssetScore> Scores { get { return JsonConvert.DeserializeObject<List<SearchResultAssetScore>>(_Scores ?? "[]"); } } 
	}

	public class SearchResultAggregation 
	{
		public Guid? Uid { get; set; }
		public AssetTypeClass Class { get; set; }
		public string DisplayName { get; set; }
		public string Name { get; set; }
		public int ResultCount { get; set; }

		public List<SearchResultAggregation> Items { get; set; }
	}

	public class SearchModel
	{
		public int Matches { get; set; }

		public new Dictionary<string, List<SearchResultAggregation>> Aggregations { get; set; }

		public List<SearchResult> Results { get; set; }
	}
}
