using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;

using Dapper;

using Elasticsearch.Net;

using MoreLinq;

using Nest;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace d360.extensions.search
{
    public class JsonResponseModel
    {
        public JObject Data { get; set; }
        public HttpStatusCode Status { get; set; }
        public string StatusMessage { get; set; }

        public bool IsSuccessStatusCode => ((int)Status >= 200) && ((int)Status <= 299);
    }

    public class SearchResultsModel
    {
        public int took { get; set; }
        
        public bool timed_out { get; set; }
        
        public SearchResultsShardModel _shards { get; set; }
        
        public SearchResultsHitsModel hits { get; set; }

        public SearchAggregationsModel aggregations { get; set; }
    }
    public class SearchResultsShardModel
    {
        public int total { get; set; }
        
        public int successful { get; set; }
        
        public int failed { get; set; }
    }
    public class SearchResultsHitsModel
    {
        public int total { get; set; }
        
        public float? max_score { get; set; }
        
        public List<SearchResultsHitModel> hits { get; set; }
    }

    public class SearchAggregationsModel
    {
        public SearchAggregationTypeModel all_types { get; set; }
    }

    public class SearchAggregationTypeModel
    {
        public List<SearchAggregationTypeBucketModel> buckets { get; set; }
    }

    public class SearchAggregationTypeBucketModel
    {
        public int doc_count { get; set; }
        
        public string key { get; set; }
        
        public SearchAggregationCategoryTypeModel category { get; set; }
    }

    public class SearchAggregationCategoryTypeModel
    {
        public List<SearchAggregationCategoryBucketModel> buckets { get; set; }
    }

    public class SearchAggregationCategoryBucketModel
    {
        public int doc_count { get; set; }
        
        public string key { get; set; }
    }

    public class SearchTagInnerHitsModel
    {
        public IndexTag _source { get; set; }
        
        public JObject highlight { get; set; }
        
        public string GetHighLightValue()
        {
            if (highlight != null && highlight.TryGetValue(ElasticSearchSource.D3S_FIELD_PREFIX + "Tags.Value", out JToken jToken))
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
                    JToken jToken = _source.SelectToken(ElasticSearchSource.D3S_FIELD_PREFIX + "Category");
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

    public class ElasticSearchSource : ISearchSource
    {
        private const string DEFAULT_SEARCH_SERVER = "search1-d3s.cloudapp.net:9200";
        private const int BULK_BATCH_SIZE = 5000;
        private const string INDEX_PREFIX = "d3s";

        private const string DYNAMIC_FIELD = "fields";
        public const string DYNAMIC_FIELD_PREFIX = DYNAMIC_FIELD + ".";

        private const string D3S_FIELD = "d3s";
        public static readonly string D3S_FIELD_PREFIX = D3S_FIELD + ".";

		private const string NGRAM_FIELD = "ngram";
		public static readonly string NGRAM_FIELD_PREFIX = NGRAM_FIELD + ".";

		private const string UNDERSCORE_FIELD = "underscore";
		public static readonly string UNDERSCORE_FIELD_PREFIX = UNDERSCORE_FIELD + ".";

		private readonly string CommunityConnectionString;

        public ElasticSearchSource()
        {

        }

        public ElasticSearchSource(string communityConnectionString)
        {
            CommunityConnectionString = communityConnectionString;
        }

        protected string SearchServerUrl { get; set; }

        public int? IndexFieldLimit { get; set; }
        public byte NGramMin { get; set; }
        public byte NGramMax { get; set; }

		#region Utility methods

		private static readonly Dictionary<string, string> NoReadMapping =
		new Dictionary<string, string>
		{
			{ "R", "NoReadResourceID" },
			{ "G", "NoReadGroupID" }
		};

		private static readonly Dictionary<string, string> CanReadMapping =
		new Dictionary<string, string>
		{
			{ "R", "CanReadResourceID" },
			{ "G", "CanReadGroupID" }
		};

		private string CreateDocument(IndexObjectModel item, bool forUpdate = false)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<string, string> d3sFields = new Dictionary<string, string>();
			Dictionary<string, string> d3sNoRead = new Dictionary<string, string>();
			Dictionary<string, string> d3sCanRead = new Dictionary<string, string>();
			Dictionary<string, string> dynamicFields = item.Fields != null ? item.Fields.Where(i => !string.IsNullOrEmpty(i.Value)).ToDictionary(i => i.Key, i => i.Value) : new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(item.RelativeUrl))
            {
                d3sFields.Add("Url", item.RelativeUrl);
            }
            
            d3sFields.Add("AssetType", item.AssetType);
            d3sFields.Add("Category", item.Category);
			d3sFields.Add("DefaultPermissions", item.DefaultPermisisons == true ? "true" : "false");

            if (item.Uid.HasValue && item.Uid != Guid.Empty)
            {
                d3sFields.Add("Uid", item.Uid.ToString());
            }
            if (item.AssetTypeUid.HasValue && item.AssetTypeUid != Guid.Empty)
            {
                d3sFields.Add("AssetTypeUid", item.AssetTypeUid.ToString());
            }
			if (item.Semantic != null)
			{
				d3sFields.Add("SemanticName", item.Semantic.Name);
				d3sFields.Add("SemanticQualifier", item.Semantic.Qualifier);
				d3sFields.Add("SemanticUid", item.Semantic.Uid.ToString());
			}

			//For users move Data3SixtyUser from Fields to d3sFields
			if (item.Category == AssetTypeClass.User.ToString() && item.AssetType == "User" && dynamicFields.ContainsKey("Data3SixtyUser"))
            {
                d3sFields.Add("Data3SixtyUser", dynamicFields["Data3SixtyUser"] == "1" ? "true" : "false");
                dynamicFields.Remove("Data3SixtyUser");
            }

            //Start d3s section
            sb.Append("{\"" + D3S_FIELD + "\": {");
            sb.Append(string.Join(",", d3sFields.Select(i => "\"" + i.Key + "\": \"" + EscapeValueForDoc(i.Value) + "\"").ToArray()));

            if (item.AssetPath?.Length > 0)
            {
                sb.Append($", \"Path\" : [{string.Join(",", item.AssetPath.Select(p => $"\"{EscapeValueForDoc(p)}\""))}]");
            }

            if (item.IndexFlags.HasFlag(IndexMode.WithResponsibility))
            {
                foreach (KeyValuePair<string, string> entry in NoReadMapping)
                {
                    string val = "[";
                    if (item.NoRead != null && item.NoRead.ContainsKey(entry.Key) && item.NoRead[entry.Key].Count > 0)
                    {
                        val += string.Join(",", item.NoRead[entry.Key].ToArray());
                    }
                    val += "]";
                    d3sNoRead.Add(entry.Value, val);
                }
                if (d3sNoRead.Count > 0)
                {
                    sb.Append("," + string.Join(",", d3sNoRead.Select(i => "\"" + i.Key + "\": " + EscapeValueForDoc(i.Value)).ToArray()));
                }
				foreach (KeyValuePair<string, string> entry in CanReadMapping)
				{
					string val = "[";
					if (item.CanRead != null && item.CanRead.ContainsKey(entry.Key) && item.CanRead[entry.Key].Count > 0)
					{
						val += string.Join(",", item.CanRead[entry.Key].ToArray());
					}
					val += "]";
					d3sCanRead.Add(entry.Value, val);
				}
				if (d3sCanRead.Count > 0)
				{
					sb.Append("," + string.Join(",", d3sCanRead.Select(i => "\"" + i.Key + "\": " + EscapeValueForDoc(i.Value)).ToArray()));
				}
			}

			if (item.IndexFlags.HasFlag(IndexMode.WithTags))
            {
                string[] tags = new string[] { };
                if (item.Tags != null && item.Tags.Any())
                {
                    tags = item.Tags.Select(t => "{ \"Uid\": \"" + t.Key + "\", \"Value\": \"" + EscapeValueForDoc(t.Value) + "\"}").ToArray();
                }

                //In case of update, so if there are no tags, we need to be explicit, so they will be removed (if any) on the document
                if (forUpdate || tags.Count() > 0)
                {
                    sb.Append(", \"Tags\":[");
                    sb.Append(string.Join(",", tags));
                    sb.Append("]");
                }
            }

            sb.Append("  },"); //End d3s section
            sb.Append("  \"" + DYNAMIC_FIELD + "\": {");
            sb.Append(string.Join(",", dynamicFields.Select(i => "\"" + i.Key + "\": \"" + EscapeValueForDoc(i.Value, i.Key.ToLower() != "name") + "\"").ToArray()));
            sb.Append("  }");
            sb.Append("}");

            return sb.ToString();
        }

        private static string GetCompanyIndexName(int companyID)
        {
            return $"{INDEX_PREFIX}{companyID}";
        }

        protected virtual IDbConnection GetDBConnection()
        {
            if (string.IsNullOrEmpty(CommunityConnectionString))
            {
                return new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            }
            else
            {
                return new SqlConnection(CommunityConnectionString);
            }

        }

        protected virtual IElasticClient GetElasticClient(int companyID)
        {
            return new ElasticClient(GetConnectionSettings(companyID));
        }

        private ConnectionSettings GetConnectionSettings(int companyID)
        {
            using (var community = GetDBConnection())
            {
                var db = community.Query<DatabaseServer>(@"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id", new { id = companyID }).SingleOrDefault();

                SearchServerUrl = db.SearchServer ?? DEFAULT_SEARCH_SERVER;
            }

            if (string.IsNullOrEmpty(SearchServerUrl))
            {
                throw new ArgumentException(OthersError.NoSearchUrlError);
            }

            var uri = new Uri("http://" + SearchServerUrl);

            var settings = new ConnectionSettings(uri)
				.DefaultIndex(GetCompanyIndexName(companyID))
				.MaximumRetries(5)
				.MaxRetryTimeout(TimeSpan.FromMinutes(3));

			return settings;
        }

        /// <summary>
        /// Create an index if it doesnt exist
        /// </summary>
        /// <param name="companyID"></param>
        private void CreateIndexIfNotExists(int companyID)
        {
            var indexName = GetCompanyIndexName(companyID);
            var client = GetElasticClient(companyID);

            var existsResponse = client.IndexExists(indexName);
            if(!existsResponse.IsValid)
            {
                throw new SearchServerConnectionException(
                    existsResponse.OriginalException,
                    string.Join(", ", client.ConnectionSettings.ConnectionPool.Nodes.Select(n => n.Uri.OriginalString)),
                    indexName
                );
            }
            else if (!existsResponse.Exists)
            {
                CreateIndexDescriptor indexDescriptor = new CreateIndexDescriptor(indexName)
                    .Settings(s => s
                        .NumberOfReplicas(1)
                        .NumberOfShards(2)
                        .Setting("index.mapping.total_fields.limit", IndexFieldLimit)
                        .Analysis(a => a
                            .CharFilters(cf => cf
                                .Mapping("underscore2space", mca => mca
                                    .Mappings(new []
                                    {
                                        "_ => \\u0020",
										". => \\u0020"
                                    })
                                )
                            )
                            .Tokenizers(t => t
								/* nGram Tokenizer cannot have min/max value of 0. When nGram is not enabled (min/max is 0) default to 99
								 * The tokenizer as well as the tokenized ngram.Name field will be defined in all mappings, and the ngram.Name field will
								 * be searched on any search.
								 * Only when the nGram feature is enabled (nGram min != 0) will the Name value be copied to the ngram.Name field, and that search
								 * possibly return results.
								 * Because searches of ngram.Name field will be analyzed with the nGram tokenizer, it has to be valid.
								 */
								.NGram("d3s_ngram", ng => ng // 
                                    .MinGram(NGramMin == 0 ? 99 : NGramMin)
                                    .MaxGram(NGramMax == 0 ? 99 : NGramMax)
                                    .TokenChars( new[] { TokenChar.Letter, TokenChar.Digit } )
                                )
							)
							.Analyzers(aa => aa
                                .Custom("default_underscore", ca => ca
                                    .CharFilters("underscore2space")
                                    .Tokenizer("standard")
                                    .Filters("standard", "lowercase")
                                )
                                .Custom("default_ngram", ca => ca
                                    .Tokenizer("d3s_ngram")
                                    .Filters("standard", "lowercase")
                                )
                            )
                        )
                    ).Mappings(ms => ms
                        .Map("_doc", m => m
                            .DateDetection(false)
                            .Properties(ps => ps
                                .Object<dynamic>(o => o
                                    .Dynamic(true)
                                    .Name(DYNAMIC_FIELD)
                                    .Properties(p => p
                                        .Text(t => t
                                            .Name("Name")
                                            .Fields(f => f
                                                .Keyword(k => k
                                                    .Name("keyword")
                                                    .IgnoreAbove(256)
                                                )
                                            )
											.CopyTo(ct =>
												{
													var flds = new List<Nest.Field>
													{
														UNDERSCORE_FIELD_PREFIX + "Name"
													};
													if( NGramMin > 0 )
													{
														flds.Add(NGRAM_FIELD_PREFIX + "Name");
													}
													return ct.Fields(flds);
												}
											)
										)
									)
                                )
								.Object<dynamic>(o => o
									.Dynamic(false)
									.Name(UNDERSCORE_FIELD)
									.Properties(p => p
										.Text(t => t
											.Name("Name")
											.Analyzer("default_underscore")
										)
										.Text(t => t
											.Name("SemanticQualifier")
											.Analyzer("default_underscore")
										)
									)
								)
								.Object<dynamic>(o => o
									.Dynamic(false)
									.Name(NGRAM_FIELD)
									.Properties(p => p
										.Text(t => t
											.Name("Name")
											.Analyzer("default_ngram")
										)
									)
								)
								.Object<dynamic>(o => o
                                    .Name(D3S_FIELD)
                                    .Properties(p => p
                                        .Keyword(s => s.Name("Category"))
                                        .Nested<dynamic>(n => n
                                            .Name("Tags")
                                            .Properties(np => np
                                                .Keyword(s => s.Name("Uid"))
                                                .Text(s => s
                                                    .Name("Value")
                                                    .Fields(f => f
                                                        .Keyword(k => k
                                                            .Name("keyword")
                                                            .IgnoreAbove(256)
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                        .Keyword(s => s.Name("AssetType"))
                                        .Keyword(s => s.Name("Uid"))
                                        .Keyword(s => s.Name("AssetTypeUid"))
                                        .Keyword(s => s.Name("Url").Index(false))
                                        .Keyword(s => s.Name("NoReadResourceID"))
                                        .Keyword(s => s.Name("NoReadGroupID"))
                                        .Boolean(b => b.Name("Data3SixtyUser"))
										.Keyword(s => s.Name("CanReadResourceID"))
										.Keyword(s => s.Name("CanReadGroupID"))
										.Boolean(b => b.Name("DefaultPermissions"))
										.Text(s => s.Name("Path"))
										.Text(s => s.Name("SemanticName"))
										.Text(s => s
											.Name("SemanticQualifier")
											.Fields(f => f
												.Keyword(k => k
													.Name("keyword")
													.IgnoreAbove(256)
												)
											)
											.CopyTo(ct => ct.Field(UNDERSCORE_FIELD_PREFIX + "SemanticQualifier"))
										)
										.Keyword(s => s.Name("SemanticUid"))
									)
								)
                            )
                        )
                    );

                var response = client.CreateIndex(indexDescriptor);

                //If Resource already exist, no reason to complain
                if (!response.IsValid && response.ServerError.Error.Type != "resource_already_exists_exception")
                {
                    throw new ArgumentException(response.OriginalException.Message);
                }
            }
        }

		public void UpdateMappingIfExists(int companyID)
		{
			var indexName = GetCompanyIndexName(companyID);
			var client = GetElasticClient(companyID);

			var existsResponse = client.IndexExists(indexName);
			if (!existsResponse.IsValid)
			{
				throw new SearchServerConnectionException(
					existsResponse.OriginalException,
					string.Join(", ", client.ConnectionSettings.ConnectionPool.Nodes.Select(n => n.Uri.OriginalString)),
					indexName
				);
			}
			else if (existsResponse.Exists)
			{
				var body = PostData.String(@"{
	""properties"": {
		""d3s"": {
			""properties"": {
				""DefaultPermissions"": {
					""type"": ""boolean""
				},
		""CanReadGroupID"": {
					""type"": ""keyword""
		},
		""CanReadResourceID"": {
					""type"": ""keyword""
		}
			}
		}
	}
}");
				var response = client.LowLevel.IndicesPutMapping<StringResponse>(indexName, "_doc", body);
				if (!response.Success)
				{
					throw new ArgumentException(response.OriginalException.Message);
				}
			}
		}

		/// <summary>
		/// Gets version number from Elastic server
		/// </summary>
		/// <param name="companyID"></param>
		public Version GetElasticVersion(int companyID)
        {
            Version ver = null;
            var client = GetElasticClient(companyID).LowLevel;
            var response = client.Info<StringResponse>();

            if (response.Success)
            {
                JObject result = JObject.Parse(response.Body);
                if (!Version.TryParse((string)result.SelectToken("version.number"), out ver))
                {
                    throw new ArgumentException(OthersError.NotDetermineServerVersion);
                }
            }

            return ver;
        }

        public int GetTotalRecordCount(int companyID)
        {
            int count = -1;
            var client = GetElasticClient(companyID).LowLevel;
            var response = client.Count<StringResponse>(PostData.Serializable(new { }));
            if (response.Success)
            {
                JObject result = JObject.Parse(response.Body);
                count = (int)result.SelectToken("count");
            }

            return count;
        }

        /// <summary>
        /// Find all indices on the search server in question that have been/are used by govern,
        /// in that the index name fits the govern index naming convention.
        /// Used to find indices that no longer belong to an active environment.
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <returns>List of company ids</returns>

        public static IEnumerable<int> GetCompanyByIndices(string serverUrl)
        {
            var client = new ElasticClient(new ConnectionSettings(new Uri("http://" + serverUrl)));
            ICatResponse<CatIndicesRecord> response = client.CatIndices();
            if(!response.IsValid)
            {
                throw new SearchServerConnectionException(response.OriginalException, serverUrl, "");
            }
            return response.Records.Select(r => r.Index).Where(i => i.StartsWith(INDEX_PREFIX)).Select(i => int.Parse(i.Substring(INDEX_PREFIX.Length)));
        }

		public static bool IndexHasLatestFeatures(string serverUrl, int companyID)
		{
			var client = new ElasticClient(new ConnectionSettings(new Uri("http://" + serverUrl)));
			return IndexHasLatestFeatures(client, companyID);
		}

		private static bool IndexHasLatestFeatures(IElasticClient client, int companyID)
		{
			bool hasLatest = false;
			//Latest feature is 'default_underscore' analyzer
			var state = GetIndexSettings(client, companyID);
			if (state.Settings.Analysis?.Analyzers?.ContainsKey("default_underscore") ?? false)
			{
				hasLatest = true;
			}

			//If nGram is enabled, the fields.Name field should be copied to the ngram.Name field
			if(hasLatest && (state.Settings.Analysis?.Tokenizers?.ContainsKey("d3s_ngram") ?? false))
			{
				var ngram = (NGramTokenizer)state.Settings.Analysis.Tokenizers["d3s_ngram"];
				if(ngram.MinGram != 0 && ngram.MinGram != 99)
				{
					var mapstate = GetIndexMapping(client, companyID);
					var fields = (ObjectProperty)mapstate.Mappings["_doc"].Properties.Where(p => p.Key == "fields").FirstOrDefault().Value;
					var name = (TextProperty)fields.Properties.Where(p => p.Key == "Name").FirstOrDefault().Value;

					hasLatest = (name.CopyTo?.Count() > 0);
				}
			}
			return hasLatest;
		}

		private static IndexState GetIndexSettings(IElasticClient client, int companyID)
		{
			var indexName = GetCompanyIndexName(companyID);
			var response = client.GetIndexSettings(i => i.Index(indexName));
			if (!response.IsValid)
			{
				throw new SearchException(response.OriginalException);
			}
			return response.Indices[indexName];
		}

		private static IndexMappings GetIndexMapping(IElasticClient client, int companyID)
		{
			var indexName = GetCompanyIndexName(companyID);
			var response = client.GetMapping<object>(m => m.Index(indexName).AllTypes());
			if (!response.IsValid)
			{
				throw new SearchException(response.OriginalException);
			}
			return response.Indices[indexName];
		}


	public static int GetIndexTotalFieldsLimit(string serverUrl, int companyID)
		{
			var client = new ElasticClient(new ConnectionSettings(new Uri("http://" + serverUrl)));
			return GetIndexTotalFieldsLimit(client, companyID);
		}

		private static int GetIndexTotalFieldsLimit(IElasticClient client, int companyID)
		{
			var state = GetIndexSettings(client, companyID);
			if(state.Settings.ContainsKey("index.mapping.total_fields.limit"))
			{
				return int.Parse(state.Settings["index.mapping.total_fields.limit"].ToString());
			}
			return 1000; //Default Elasticsearch value
		}

		public static int GetIndexFieldMappingCount(string serverUrl, int companyID)
		{
			var client = new ElasticClient(new ConnectionSettings(new Uri("http://" + serverUrl)));
			return GetIndexFieldMappingCount(client, companyID);
		}

		private static int GetIndexFieldMappingCount(IElasticClient client, int companyID)
		{
			var state = GetIndexMapping(client, companyID);
			var fields = (ObjectProperty)state.Mappings["_doc"].Properties.Where(p => p.Key == "fields").FirstOrDefault().Value;
			return fields.Properties?.Count() ?? 0;
		}

		public static int SuggestIndexLimit(SqlConnection context)
		{
			/*
             * To estimate the limit of fields in the index, we count the number of field types and add 20%
             * We are not indexing all field types, and field types with the same name are mapped to the same elastic field
             * If the number of field types is too high, then count the distinct field names and add 80%.
             * Because each field dynamically added in elasticSearch will be added as BOTh a text and keyword,
             * the counts form the database are doubled (count + 20% = 2.4, count + 80% = 3.6)
             * Under no circumstance should we set limit higher than 30,000
             * https://www.elastic.co/guide/en/elasticsearch/reference/6.8/mapping.html#mapping-limit-settings
             */
			var sql = @"SELECT CASE
                            WHEN a.dist > 30000 THEN 30000
                            WHEN a.total > 30000 THEN a.dist
                            ELSE a.total
                        END
                        FROM (
                            SELECT FLOOR(COUNT(*) * 2.4) AS total,
                                    FLOOR(COUNT(DISTINCT [Name]) * 3.6) AS dist
                            FROM [dbo].[FieldType]
                        ) a;";
			return context.Query<int>(sql).FirstOrDefault();
		}
		public static int CountIndexableFieldTypes(SqlConnection context)
		{
			var sql = @"SELECT COUNT(*)
                FROM [dbo].[FieldType]
				WHERE AssetTypeID IS NOT NULL
				AND [Type] not in('" + string.Join("','", SearchIndexer.ExcludedFieldTypes.ToArray()) + "')";
			return context.Query<int>(sql).FirstOrDefault();
		}

		public static void DeleteIndexIfExists(string serverUrl, int companyID)
        {
            var client = new ElasticClient(new ConnectionSettings(new Uri("http://" + serverUrl)));
            DeleteIndexIfExists(client, companyID);
        }

        private static void DeleteIndexIfExists(IElasticClient client, int companyID)
        {
            var indexName = GetCompanyIndexName(companyID);
            if (client.IndexExists(indexName).Exists)
            {
                var response = client.DeleteIndex(indexName);
                if (!response.IsValid)
                {
                    throw new ArgumentException(response.OriginalException.Message);
                }
            }
        }

        /// <summary>
        /// Delete an index if it exists
        /// </summary>
        /// <param name="companyID"></param>
        private void DeleteIndexIfExists(int companyID)
        {
            //NEST client
            var client = GetElasticClient(companyID);
            DeleteIndexIfExists(client, companyID);
        }

        private string EscapeValueForDoc(string input, bool removeTags = true)
        {
            if (!string.IsNullOrEmpty(input))
            {
                //Replace control characters including \t, r\ and \n with space to preserve word boundry
                input = Regex.Replace(input, "[\u0000-\u001F]", " ");

                input = input
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"");

                if (removeTags && input.Contains("<") && input.Contains(">"))
                {
                    input = core.helpers.HtmlHelper.RemoveTags(input);
                }
            }

            return input;
        }

        #endregion

        public void AddToIndex(IndexObjectModel item)
        {
            AddToIndex(new List<IndexObjectModel> { item });
        }

        public void AddToIndex(IEnumerable<IndexObjectModel> items)
        {
            int companyId = default(int);
            bool firstRun = true;

            List<string> postingErrors = new List<string>();

            foreach (var batch in items.Batch(BULK_BATCH_SIZE))
            {
                //Get FirstOrDefault inside batch loop to not trigger enumeration twice
                if (firstRun)
                {
                    firstRun = false;
                    var firstItem = batch.FirstOrDefault();
                    if (firstItem == null)
                    {
                        return;
                    }

                    companyId = firstItem.CompanyID;
                    CreateIndexIfNotExists(companyId);
                }

                var sb = new StringBuilder();

                foreach (var item in batch)
                {
                    sb.Append("{\"index\":{\"_id\":\"");
                    sb.Append(item.getObjectID());
                    sb.Append("\",\"_type\":\"");
                    sb.Append("_doc");
                    sb.Append("\" } }\n");
                    sb.AppendLine(CreateDocument(item));

                }
                sb.Append("\n");

                var client = GetElasticClient(companyId).LowLevel;
                var bulkResponse = client.Bulk<StringResponse>(GetCompanyIndexName(companyId), sb.ToString());

                if (!bulkResponse.Success)
                {
                    throw new ArgumentException(bulkResponse.OriginalException.Message);
                }

                var result = JObject.Parse(bulkResponse.Body);

                if (result == null)
                {
                    throw new ArgumentException(OthersError.InvalidResponseData);
                }

                var hasErrors = result.GetValue("errors");

                if (hasErrors.Value<bool>())
                {
                    foreach (var resultItem in result.GetValue("items"))
                    {
                        var errToken = resultItem.SelectToken("index.error");
                        if (errToken != null)
                        {
                            string fault = errToken.ToString();
                            string id = (string)resultItem.SelectToken("index._id");
                            postingErrors.Add(id + ":" + fault);
                        }
                    }

                }
            }

            if (postingErrors.Count > 0)
            {
                throw new ArgumentException(OthersError.AddIndexIndividualErrors + string.Join(Environment.NewLine, postingErrors.ToArray()));
            }
        }

        public void ClearIndex(int companyID)
        {
            DeleteIndexIfExists(companyID);
            CreateIndexIfNotExists(companyID);
        }

        public void ClearIndex(int companyID, string category, string assetType = null)
        {
            CreateIndexIfNotExists(companyID);

            List<QueryContainer> termQueries = new List<QueryContainer>
            {
                new TermQuery
                {
                    Field = new Nest.Field(D3S_FIELD_PREFIX + "Category"),
                    Value = category
                }
            };
            if (!string.IsNullOrEmpty(assetType))
            {
                termQueries.Add(new TermQuery
                {
                    Field = new Nest.Field(D3S_FIELD_PREFIX + "AssetType"),
                    Value = assetType
                });
            }

            SearchRequest sReq = new SearchRequest
            {
                Query = new BoolQuery
                {
                    Must = termQueries
                }
            };

            StringResponse deleteResponse = PerformDeleteByQuery(companyID, sReq);

            if (!deleteResponse.Success)
            {
                throw new ArgumentException(deleteResponse.OriginalException.Message);
            }
        }

        public void ClearIndex(int companyID, Guid assetTypeGuid)
        {
            CreateIndexIfNotExists(companyID);

            SearchRequest sReq = new SearchRequest
            {
                Query = new TermQuery
                {
                    Field = new Nest.Field(D3S_FIELD_PREFIX + "AssetTypeUid"),
                    Value = assetTypeGuid
                }
            };

            StringResponse deleteResponse = PerformDeleteByQuery(companyID, sReq);

            if (!deleteResponse.Success)
            {
                var errMessage = deleteResponse.OriginalException.Message;
                errMessage += "\nDeleting Asset Type : " + assetTypeGuid.ToString();
                throw new ArgumentException(errMessage);
            }
        }

        public void RemoveByUids(int companyID, IEnumerable<Guid> assetUids)
        {
            var indexName = GetCompanyIndexName(companyID);
            var client = GetElasticClient(companyID);

            //No index, nothing to delete
            if (!client.IndexExists(indexName).Exists)
            {
                return;
            }

            SearchRequest sReq = new SearchRequest
            {
                Query = new TermsQuery
                {
                    Field = new Nest.Field(D3S_FIELD_PREFIX + "Uid"),
                    Terms = assetUids.Select(u => u.ToString())
                }
            };

            StringResponse deleteResponse = PerformDeleteByQuery(companyID, sReq);

            if (!deleteResponse.Success)
            {
                throw new ArgumentException(deleteResponse.OriginalException.Message);
            }
        }

        private StringResponse PerformDeleteByQuery(int companyID, SearchRequest sReq)
        {
            var client = GetElasticClient(companyID);
            //Because the index model is variable, the LowLevel client is used and the request is turned into a JSON string
            string jsonString = client.RequestResponseSerializer.SerializeToString(sReq);

            //Refresh all shards after the query delete to avoid 409 versioning conflicts
            DeleteByQueryRequestParameters requestParameters = new DeleteByQueryRequestParameters
            {
                Refresh = true
            };
            return client.LowLevel.DeleteByQuery<StringResponse>(GetCompanyIndexName(companyID), jsonString, requestParameters);

        }

        private bool IsElasticSearchSpecialChar(char ch)
        {
            if (ch == '\\' || ch == '/' || ch == ':' || ch == '^' || ch == '~' || ch == ')' || ch == '(' ||
               ch == '!' || ch == '[' || ch == ']' || ch == '{' || ch == '}' || ch == '-')
            {
                return true;
            }

            return false;
        }

        private string EscapeSpecialCharacters(string phrase)
        {
            if (string.IsNullOrEmpty(phrase))
            {
                return "";
            }
            bool padWithQuotes = false;

            if (phrase.Contains('"'))
            {
                //special rules for quotes.  If quotes are in the string they must be in pairs at begining and end
                if ((phrase.Length > 0) && (phrase[0] == '"') && (phrase[phrase.Length - 1] == '"'))
                {
                    phrase = phrase.Trim('"');

                    phrase = phrase.Replace("\"", "?");

                    padWithQuotes = true;
                }
                else
                {
                    phrase = phrase.Replace("\"", "?");
                }
            }

            if (phrase.Any(ch => IsElasticSearchSpecialChar(ch)))
            {
                phrase = phrase.Replace("\\", "?");  // replace backslash with wildcard LEAVE FIRST

                phrase = phrase.Replace(":", "\\:"); // escape colon

                phrase = phrase.Replace("^", "\\^"); // escape carat

                phrase = phrase.Replace("~", "\\~"); // escape carat

                phrase = phrase.Replace("!", "\\!"); //  escape exclamation

                phrase = phrase.Replace("[", "\\["); //  escape square bracket

                phrase = phrase.Replace("]", "\\]"); //  escape square bracket

                phrase = phrase.Replace("{", "\\{"); //  escape curyly bracket

                phrase = phrase.Replace("}", "\\}"); //  escape curyly bracket

                phrase = phrase.Replace("-", "\\-"); // escape hyphen

                phrase = phrase.Replace("/", "\\/"); // replace / with escaped slash                

                phrase = phrase.Replace("(", "\\("); // replace / with escaped slash       

                phrase = phrase.Replace(")", "\\)"); // replace / with escaped slash   
            }

            if (padWithQuotes)
            {
                phrase = "\\\"" + phrase + "\\\"";
            }

            return phrase;
        }

        private const char STRATEGY_NONE = ' ';
        private const char STRATEGY_QueryString = 'Q';
        private const char STRATEGY_MatchPhrase = 'M';
        private const char STRATEGY_MatchPhrasePrefix = 'P';
        private const char STRATEGY_BestFields = 'B';
        private const char STRATEGY_MostFields = 'O';
        private const char STRATEGY_CrossFields = 'C';
        private const char STRATEGY_FullUID = 'U';
        private const char STRATEGY_PartialUID = 'W';
        private const char STRATEGY_Experimental = 'X';
        private const char STRATEGY_MatchAll = '*';

        private TextQueryType MapStrategyToType(char strategy)
        {
            switch (strategy)
            {
                case STRATEGY_BestFields:
                case '0':
                    return TextQueryType.BestFields;
                case STRATEGY_MostFields:
                case '1':
                    return TextQueryType.MostFields;
                case STRATEGY_CrossFields:
                case '2':
                    return TextQueryType.CrossFields;
                case STRATEGY_MatchPhrase:
                case '3':
                    return TextQueryType.Phrase;
                case STRATEGY_MatchPhrasePrefix:
                case '4':
                    return TextQueryType.PhrasePrefix;
                default:
                    throw new ArgumentException("Cannot map " + strategy);
            }
        }

        private List<QueryContainer> GetMainQuery(QueryRequest queryRequest)
        {
            List<QueryContainer> mainQueries = new List<QueryContainer>();
            List<QueryContainer> tagQueries = new List<QueryContainer>();

            Nest.Field fldTag = new Nest.Field(D3S_FIELD_PREFIX + "Tags.Value");
            string tagPath = D3S_FIELD_PREFIX + "Tags";

            char strategy = STRATEGY_NONE;

            /* Experimental strategy settings - Overloading search boost table, taking a float score and turning into a char array with numerica characters
             * This allows experimenting with combinations of search strategies without new deployments being necessary
             * 
             * Pos [0]: Separate name search
             *      0 = include field boosts in field list
             *      1 = separate match phrase on boost fields
             * Pos [1]: Augment search - broarder search with boost 0.5
             *      0,1,2,3,4 = Search Type as defined by MapStrategyToType (3 and 4 makes little sense)
             *      9 = disabled
             * Pos [2]: Core search
             *      0,1,2,3,4 = Search Type as defined by MapStrategyToType
             */
            int _defaultStrategy = 90; //'Original/current' is 90: best_fields, boosts included, no augment
            char[] _strategy = ((int?)queryRequest.FieldBoosters.FirstOrDefault(fb => fb.Field == "_strategy")?.Boost ?? _defaultStrategy).ToString().PadLeft(3, '0').ToCharArray();

            //Search in the DYNAMIC_FIELD_PREFIX.* namespace
            List<Nest.Field> mainFields = new List<Nest.Field>
            {
                new Nest.Field(DYNAMIC_FIELD_PREFIX + "*"),
            };

            //Search phrase
            string phrase = EscapeSpecialCharacters(queryRequest.Term);

            //Pick strategy
            if (!string.IsNullOrEmpty(phrase))
            {
                int isGuid = IsPhraseGuid(queryRequest.Term); //Use term and not escaped phrase
                if (isGuid == 1)
                {
                    strategy = STRATEGY_PartialUID;
                }
                else if (isGuid == 2)
                {
                    strategy = STRATEGY_FullUID;
                }
                else if (queryRequest.Term == "*")
                {
                    strategy = STRATEGY_MatchAll;
                }
                else if (queryRequest.Term.StartsWith("'") && queryRequest.Term.EndsWith("'")) //Use term and not escaped phrase, need to remove encapsulation '`s
                {
                    strategy = STRATEGY_MatchPhrase;
                    phrase = EscapeSpecialCharacters(queryRequest.Term.Trim('\''));
                }
                else if (phrase.Contains("*"))
                {
                    if (phrase.Count(c => c == '*') == 1 && phrase.EndsWith("*"))
                    {
                        strategy = STRATEGY_MatchPhrasePrefix;
                        phrase = phrase.TrimEnd('*');
                    }
                    else
                    {
                        strategy = STRATEGY_QueryString;
                    }
                }
                else
                {
                    strategy = STRATEGY_Experimental;
                }
            }

            switch (strategy)
            {
                case STRATEGY_NONE:
                    throw new ArgumentException(OthersError.CannotUseSearch);
                case STRATEGY_PartialUID:
                    mainQueries.Add(new PrefixQuery
                    {
                        Field = new Nest.Field(D3S_FIELD_PREFIX + "Uid"),
                        Value = queryRequest.Term.ToLower()
                    });
                    tagQueries.Add(new PrefixQuery
                    {
                        Field = new Nest.Field(D3S_FIELD_PREFIX + "Tags.Uid"),
                        Value = queryRequest.Term.ToLower()
                    });
                    break;
                case STRATEGY_MatchAll:
                    mainQueries.Add(new MatchAllQuery());
                    break;
                case STRATEGY_FullUID:
                    mainQueries.Add(new TermQuery
                    {
                        Field = new Nest.Field(D3S_FIELD_PREFIX + "Uid"),
                        Value = queryRequest.Term.ToLower()
                    });
                    tagQueries.Add(new TermQuery
                    {
                        Field = new Nest.Field(D3S_FIELD_PREFIX + "Tags.Uid"),
                        Value = queryRequest.Term.ToLower()
                    });
                    break;
                case STRATEGY_QueryString:
                    //Add any eventual boosts to fields list
                    mainQueries.Add(new QueryStringQuery
                    {
                        Fields = mainFields.Concat(
                                queryRequest.FieldBoosters
                                    .Where(fb => fb.Field.StartsWith("fields."))
                                    .Select(boost => new Nest.Field(boost.Field, boost.Boost))
                                    .ToList()
                                ).ToArray(),
                        Query = phrase,
                        AnalyzeWildcard = true
                    });
                    tagQueries.Add(new QueryStringQuery
                    {
                        DefaultField = fldTag,
                        Query = phrase,
                        AnalyzeWildcard = true
                    });
                    break;
                case STRATEGY_MatchPhrasePrefix:
                    //Add any eventual boosts to fields list
                    mainQueries.Add(new MultiMatchQuery
                    {
                        Fields = mainFields.Concat(
                                queryRequest.FieldBoosters
                                    .Where(fb => fb.Field.StartsWith("fields."))
                                    .Select(boost => new Nest.Field(boost.Field, boost.Boost))
                                    .ToList()
                                ).ToArray(),
                        Query = phrase,
                        Type = TextQueryType.PhrasePrefix
                    });
                    tagQueries.Add(new MatchPhrasePrefixQuery
                    {
                        Field = fldTag,
                        Query = phrase
                    });
                    break;
                case STRATEGY_BestFields:
                case STRATEGY_MostFields:
                case STRATEGY_CrossFields:
                case STRATEGY_MatchPhrase:
                    mainQueries.Add(new MultiMatchQuery
                    {
                        Fields = mainFields.ToArray(),
                        Query = phrase,
                        Type = MapStrategyToType(strategy)
                    });
                    tagQueries.Add(new MatchPhraseQuery
                    {
                        Field = fldTag,
                        Query = phrase
                    });
                    break;
                case STRATEGY_Experimental:
                    if (_strategy[0] == '1') //Separate defined field boosts into separate phrase queries
                    {
                        queryRequest.FieldBoosters
                            .Where(fb => fb.Field.StartsWith("fields."))
                            .ForEach(boost => mainQueries.Add(new MatchPhraseQuery
                            {
                                Field = boost.Field,
                                Query = phrase,
                                Boost = boost.Boost
                            }));

                    }
                    else
                    {
                        //Add boost fields to fields list
                        mainFields.AddRange(
                            queryRequest.FieldBoosters
                                .Where(fb => fb.Field.StartsWith("fields."))
                                .Select(boost => new Nest.Field(boost.Field, boost.Boost))
                            );
                    }

                    //Core query
                    mainQueries.Add(new MultiMatchQuery
                    {
                        Fields = mainFields.ToArray(),
                        Query = phrase,
                        Type = MapStrategyToType(_strategy[2])
                    });
					//nGram/underscore query
					mainQueries.Add(new MatchQuery {
						Field = NGRAM_FIELD_PREFIX + "Name",
						Query = phrase
					});
					mainQueries.Add(new MatchPhrasePrefixQuery {
						Field = UNDERSCORE_FIELD_PREFIX + "Name",
						Query = phrase
					});

                    if (_strategy[1] != '9') //Augment search with boost 0.5
                    {
                        mainQueries.Add(new MultiMatchQuery
                        {
                            Fields = mainFields.ToArray(),
                            Query = phrase,
                            Type = MapStrategyToType(_strategy[1]),
                            Boost = 0.5
                        });
                    }
                    tagQueries.Add(new MatchPhraseQuery
                    {
                        Field = fldTag,
                        Query = phrase
                    });
                    break;
                default:
                    throw new ArgumentException(OthersError.UnknownSearchStrategy + strategy);
            }

            double? tagBoost = null;
            if (queryRequest.FieldBoosters.Any(fb => fb.Field == tagPath))
            {
                tagBoost = queryRequest.FieldBoosters.First(fb => fb.Field == tagPath).Boost;
            }

            mainQueries.Add(new NestedQuery
            {
                Path = tagPath,
                Boost = tagBoost,
                Query = new BoolQuery
                {
                    Must = tagQueries
                },
                InnerHits = new InnerHits
                {
                    Highlight = new Highlight
                    {
                        Fields = new Dictionary<Nest.Field, IHighlightField> { { fldTag, new HighlightField { } } },
                        Encoder = HighlighterEncoder.Html
                    }
                }
            });

            return mainQueries;
        }

        private QueryContainer[] GetRefinementFilters(QueryRequest queryRequest)
        {
            List<QueryContainer> mustQueries = new List<QueryContainer>();
            List<QueryContainer> shouldQueries = new List<QueryContainer>();
            List<QueryContainer> mustNotQueries = new List<QueryContainer>();

            foreach (FieldFilter fieldFilter in queryRequest.FieldFilters)
            {
                QueryContainer qry;
                if (fieldFilter.Values == null || fieldFilter.Values.Length == 0)
                {
                    continue;
                }
                string[] values = fieldFilter.Values.Select(v => EscapeSpecialCharacters(v).ToLower(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
				if (fieldFilter.Field == "Tags")
				{
					string path = D3S_FIELD_PREFIX + "Tags";
					Nest.Field fldTag = new Nest.Field(D3S_FIELD_PREFIX + "Tags.Value");

					if (fieldFilter.Connector == SearchConnector.And)
					{
						qry = new BoolQuery
						{
							Must = values.Select(v =>
							{
								QueryContainer q = new NestedQuery
								{
									Path = path,
									Query = new MatchQuery
									{
										Field = fldTag,
										Query = v,
										Operator = Nest.Operator.And
									}
								};
								return q;
							})
						};
					}
					else
					{
						qry = new NestedQuery
						{
							Path = path,
							Query = new BoolQuery
							{
								Should = values.Select(v =>
								{
									QueryContainer q = new MatchQuery
									{
										Field = fldTag,
										Query = v,
										Operator = Nest.Operator.And
									};
									return q;
								}),
								MinimumShouldMatch = 1
							}
						};
					}
				}
				else if (fieldFilter.Field == "TagUids")
				{
					string path = D3S_FIELD_PREFIX + "Tags";
					Nest.Field fldTagUid = new Nest.Field(D3S_FIELD_PREFIX + "Tags.Uid");
					if (fieldFilter.Connector == SearchConnector.And)
					{
						qry = new BoolQuery
						{
							Must = fieldFilter.Values.Select(v =>
							{
								QueryContainer q = new NestedQuery
								{
									Path = path,
									Query = new TermQuery
									{
										Field = fldTagUid,
										Value = v
									}
								};
								return q;
							})
						};
					}
					else
					{
						qry = new NestedQuery
						{
							Path = path,
							Query = new BoolQuery
							{
								Should = fieldFilter.Values.Select(v =>
								{
									QueryContainer q = new TermQuery
									{
										Field = fldTagUid,
										Value = v
									};
									return q;
								}),
								MinimumShouldMatch = 1
							}
						};
					}
				}
				else if (fieldFilter.Field == "Path")
				{
					Nest.Field fldPath = new Nest.Field(D3S_FIELD_PREFIX + "Path");
					var segmentQueries = values.Select(v =>
					{
						QueryContainer q = new MatchQuery
						{
							Field = fldPath,
							Query = v
						};
						return q;
					});

					if (fieldFilter.Connector == SearchConnector.And)
					{
						qry = new BoolQuery
						{
							Must = segmentQueries
						};
					}
					else
					{
						qry = new BoolQuery
						{
							Should = segmentQueries,
							MinimumShouldMatch = 1
						};
					}
				}
				else
				{
					var flds = new List<Nest.Field>();
					string term = fieldFilter.Values.First();
					IEnumerable<QueryContainer> subqueries;

					if (fieldFilter.Field == "Semantictype")
					{
						flds.Add(D3S_FIELD_PREFIX + "SemanticName");
						flds.Add(D3S_FIELD_PREFIX + "SemanticQualifier");
						if (!term.StartsWith("*"))
						{
							term = $"*{term}";
						}
						if (!term.EndsWith("*"))
						{
							term = $"{term}*";
						}

						if (IsPhraseGuid(values.First()) > 0)
						{
							flds.Add(D3S_FIELD_PREFIX + "SemanticUid");
						}
					}
					else
					{
						flds.Add(DYNAMIC_FIELD_PREFIX + fieldFilter.Field);
						//For Name, also search the underscore field
						if (fieldFilter.Field == "Name")
						{
							flds.Add(UNDERSCORE_FIELD_PREFIX + fieldFilter.Field);
						}
					}

					if (fieldFilter.MatchWords)
					{
						subqueries = flds.Select(f => {
							QueryContainer q = new MatchPhraseQuery
							{
								Field = f,
								Query = term
							};
							return q;
						});
					}
					else
					{
						if (term.Contains("*"))
						{
							if (term.EndsWith("*")) //If we have trailing *, remove before escaping
							{
								term = term.Remove(term.Length - 1);
							}
							term = EscapeSpecialCharacters(term) + "*";

							subqueries = flds.Select(f =>
							{
								QueryContainer q = new QueryStringQuery
								{
									Fields = f,
									Query = term,
									AnalyzeWildcard = true
								};
								return q;
							});
						}
						else
						{
							subqueries = flds.Select(f =>
							{
								QueryContainer q = new MatchPhrasePrefixQuery
								{
									Field = f,
									Query = values.First()
								};
								return q;
							});
						}
					}

					if(subqueries.Count() == 1)
					{
						qry = subqueries.First();
					}
					else
					{
						qry = new BoolQuery
						{
							Should = subqueries,
							MinimumShouldMatch = 1
						};
					}
				}

                if (fieldFilter.Operator == SearchOperator.NotContains)
                {
                    mustNotQueries.Add(qry);
                }
                else if (queryRequest.SearchConnector == SearchConnector.Or)
                {
                    shouldQueries.Add(qry);
                }
                else
                {
                    mustQueries.Add(qry);
                }
            }

            return new QueryContainer[]
            {
                new BoolQuery
                {
                    Should = shouldQueries,
                    Must = mustQueries,
                    MustNot = mustNotQueries,
                    MinimumShouldMatch = Math.Sign(shouldQueries.Count()) // 1 if any shoulds, 0 otherwise
                }
            };
        }

        private List<QueryContainer> GetAggregationFilters(QueryRequest queryRequest, QueryLimitation queryLimit)
        {
            List<QueryContainer> aggFilters = new List<QueryContainer>();

            //Apply aggregation filters
            foreach (AggregationFilter aggFilter in queryRequest.AggregationFilters)
            {
                if (aggFilter.Values == null)
                {
                    continue;
                }

                string fieldname;
                switch (aggFilter.Field)
                {
                    case "Category":
                    case "AssetType":
                        fieldname = D3S_FIELD_PREFIX + aggFilter.Field;
                        break;
                    default:
                        fieldname = DYNAMIC_FIELD_PREFIX + aggFilter.Field;
                        break;
                }
                IEnumerable<string> terms;
                if (queryLimit.AggregationFilters.Exists(l => l.Field == aggFilter.Field))
                {
                    terms = aggFilter.Values.Except(queryLimit.AggregationFilters.Find(l => l.Field == aggFilter.Field).Values);
                }
                else
                {
                    terms = aggFilter.Values;
                }

                if (terms.Count() > 0)
                {
                    aggFilters.Add(new TermsQuery
                    {
                        Field = new Nest.Field(fieldname),
                        Terms = terms.ToArray()
                    });
                }
            }
            return aggFilters;
        }

        public IndexResults GetSearchResultsWithAggregation(int companyID, QueryRequest queryRequest, QueryLimitation queryLimit)
        {
            IndexResults result = new IndexResults();

            Nest.Field fldName = new Nest.Field(DYNAMIC_FIELD_PREFIX + "Name");
            Nest.Field fldCategory = new Nest.Field(D3S_FIELD_PREFIX + "Category");
            Nest.Field fldAssetType = new Nest.Field(D3S_FIELD_PREFIX + "AssetType");

            //If no main search phrase, return an empty result set
            if (string.IsNullOrWhiteSpace(queryRequest.Term))
            {
                return result;
            }

            SearchRequest sReq = new SearchRequest
            {
                Query = new BoolQuery
                {
                    Must = new QueryContainer[] {
                        new BoolQuery{
                            Should = GetMainQuery(queryRequest),
                            Must = GetRefinementFilters(queryRequest),
                            MinimumShouldMatch = 1
                        }
                    },
                    Filter = new QueryContainer[] { new BoolQuery {
                        Must = GetAggregationFilters(queryRequest, queryLimit),
                        MustNot = FiltersFromLimit(queryLimit)
                    } }
                },
                Highlight = new Highlight
                {
                    Fields = new Dictionary<Nest.Field, IHighlightField> { { fldName, new HighlightField {
                        PreTags = new [] { "<em class='search-highlight'>" },
                        PostTags = new [] { "</em>" },
                        NumberOfFragments = 0
                    } } },
                    RequireFieldMatch = false,
                    Encoder = HighlighterEncoder.Html
                },
                From = queryRequest.From,
                Size = queryRequest.Size
            };

            // determine if we need aggreagtions
            foreach (string aggregation in queryRequest.Aggregations)
            {
                if (aggregation == "category")
                {
                    //size=0 intepreted as integer.MAX_VALUE deprecated in ES 2.4.0.
                    //Using 2000 for EX6 for now. @TODO: Consider using Composite aggreation
                    sReq.Aggregations = new TermsAggregation("all_types")
                    {
                        Field = fldCategory,
                        Size = 20,
                        Aggregations = new TermsAggregation("category")
                        {
                            Field = fldAssetType,
                            Size = 2000
                        }
                    };
                }
            }

            if (queryRequest.Explain)
            {
                sReq.Explain = true;
            }

            var searchResults = PerformSearch(companyID, sReq);

            result.Results = searchResults.hits.hits.Select(h => new IndexResult
            {
                Name = GetHighlightedNameValueIfExists(h),
                DisplayName = GetDisplayName(h),
                Description = GetHighlightedPropertyValueIfExists(h, DYNAMIC_FIELD_PREFIX + "Description") ?? GetHighlightedPropertyValueIfExists(h, DYNAMIC_FIELD_PREFIX + "description"),
                Group = MapCategoryToFriendlyName(h.d3sCategory),
                ID = h._id,
                NormalizedScore = (searchResults.hits.max_score.GetValueOrDefault() == 0 ? 0 : (h._score / searchResults.hits.max_score.GetValueOrDefault() * 100)),
                Score = h._score,
                Type = GetHighlightedPropertyValueIfExists(h, D3S_FIELD_PREFIX + "AssetType"),
                Url = GetHighlightedPropertyValueIfExists(h, D3S_FIELD_PREFIX + "Url"),
                Uid = GetGuidPropertyIfExists(h, D3S_FIELD_PREFIX + "Uid"),
                AssetTypeUid = GetGuidPropertyIfExists(h, D3S_FIELD_PREFIX + "AssetTypeUid"),
                Tags = GetTags(h),
                Explanation = queryRequest.Explain ? h._explanation.ToString() : "",
				SemanticName = GetHighlightedPropertyValueIfExists(h, D3S_FIELD_PREFIX + "SemanticName"),
				SemanticQualifier = GetHighlightedPropertyValueIfExists(h, D3S_FIELD_PREFIX + "SemanticQualifier"),
				SemanticUid = GetGuidPropertyIfExists(h, D3S_FIELD_PREFIX + "SemanticUid")
			}).ToList();


            if (searchResults.aggregations != null && searchResults.aggregations.all_types != null && searchResults.aggregations.all_types.buckets != null)
            {
                List<IndexAggregation> categories = new List<IndexAggregation>();
                categories.AddRange(searchResults.aggregations.all_types.buckets.Select(h => new IndexAggregation
                {
                    Name = h.key,
                    DisplayName = MapCategoryToFriendlyName(h.key),
                    ResultCount = h.doc_count,
                    Items = h.category?.buckets.Select(c => new IndexAggregation
                    {
                        Name = c.key,
                        DisplayName = c.key,
                        ResultCount = c.doc_count
                    }).OrderBy(x => x.Name).ToList()
                }).OrderBy(x => x.DisplayName));

                result.Aggregations.Add("category", categories);
            }

            result.ElapsedMS.Add("Query", searchResults.took);

            if (searchResults.hits != null)
            {
                result.Matches = searchResults.hits.total;
            }

            return result;
        }

        /**
         * Perform a search that counts total items in index and buckets categories
         */
        public IndexResults GetStatusSearch(int companyID, bool withTypes = false)
        {
            IndexResults result = new IndexResults();

            SearchRequest sReq = new SearchRequest
            {
                Query = new SimpleQueryStringQuery
                {
                    Query = "*"
                },
                From = 0,
                Size = 0,
                Aggregations = new TermsAggregation("all_types")
                {
                    Field = new Nest.Field(D3S_FIELD_PREFIX + "Category"),
                    Size = 30
                }
            };

            if (withTypes)
            {
                sReq.Aggregations["all_types"].Aggregations = new TermsAggregation("category")
                {
                    Field = new Nest.Field(D3S_FIELD_PREFIX + "AssetType"),
                    Size = 2000
                };
            }

            var searchResults = PerformSearch(companyID, sReq);

            if (searchResults.aggregations != null && searchResults.aggregations.all_types != null && searchResults.aggregations.all_types.buckets != null)
            {
                List<IndexAggregation> categories = new List<IndexAggregation>();
                categories.AddRange(searchResults.aggregations.all_types.buckets.Select(h => new IndexAggregation
                {
                    Name = h.key,
                    DisplayName = MapCategoryToFriendlyName(h.key),
                    ResultCount = h.doc_count,
                    Items = h.category?.buckets.Select(c => new IndexAggregation
                    {
                        Name = c.key,
                        DisplayName = c.key,
                        ResultCount = c.doc_count
                    }).OrderBy(x => x.Name).ToList()
                }).OrderBy(x => x.DisplayName));

                result.Aggregations.Add("category", categories);
            }

            result.ElapsedMS.Add("Query", searchResults.took);

            if (searchResults.hits != null)
            {
                result.Matches = searchResults.hits.total;
            }

            return result;
        }

        public List<IndexableCount> GetStatusList(int companyID)
        {
            List<IndexableCount> result = new List<IndexableCount>();

            SearchRequest sReq = new SearchRequest
            {
                Query = new SimpleQueryStringQuery
                {
                    Query = "*"
                },
                From = 0,
                Size = 0,
                Aggregations = new TermsAggregation("all_types")
                {
                    Field = new Nest.Field(D3S_FIELD_PREFIX + "Category"),
                    Size = 30,
                    Aggregations = new TermsAggregation("category")
                    {
                        Field = new Nest.Field(D3S_FIELD_PREFIX + "AssetTypeUid"),
                        Size = 2000
                    }
                }
            };

            var searchResults = PerformSearch(companyID, sReq);

            searchResults.aggregations.all_types?.buckets?.ForEach(b =>
            {
                result.Add(new IndexableCount { ClassName = b.key, AssetTypeUid = Guid.Empty, CurrentCount = b.doc_count });
                result.AddRange(b.category?.buckets?.Select(t => new IndexableCount { ClassName = b.key, AssetTypeUid = Guid.Parse(t.key), CurrentCount = t.doc_count }));
            });

            return result;
        }

        private List<QueryContainer> FiltersFromLimit(QueryLimitation queryLimit)
        {
			var permissionField = new Nest.Field(D3S_FIELD_PREFIX + "DefaultPermissions");

			List<QueryContainer> mustNotQueries = new List<QueryContainer>
			{
				//If permission field does not exists or is true, user or group cannot be in the NoRead
				new BoolQuery
				{
					MinimumShouldMatch = 1,
					Should = new QueryContainer[]
					{
						//NoRead limitations
						new TermQuery
						{
							Field = new Nest.Field(D3S_FIELD_PREFIX + "NoReadResourceID"),
							Value = queryLimit.ResourceID
						},
						new TermsQuery
						{
							Field = new Nest.Field(D3S_FIELD_PREFIX + "NoReadGroupID"),
							Terms = queryLimit.ResourceGroupIDs.Select(i => i.ToString())
						},
					},
					Must = new QueryContainer[]
					{
						new BoolQuery
						{
							MinimumShouldMatch = 1,
							Should = new QueryContainer[]
							{
								new TermQuery
								{
									Field = permissionField,
									Value = true
								},
								new BoolQuery
								{
									MustNot = new QueryContainer[]
									{
										new ExistsQuery
										{
											Field = permissionField
										}
									}
								}
							}
						}
					}
                }
            };

			if(!queryLimit.IsAdministrator)
			{
				mustNotQueries.Add(new BoolQuery
				{
					MinimumShouldMatch = 1,
					Must = new QueryContainer[]
					{
						new TermQuery
						{
							Field = permissionField,
							Value = false
						}
					},
					Should = new QueryContainer[]
					{
						new TermQuery
						{
							Field = new Nest.Field(D3S_FIELD_PREFIX + "CanReadResourceID"),
							Value = queryLimit.ResourceID
						},
						new TermsQuery
						{
							Field = new Nest.Field(D3S_FIELD_PREFIX + "CanReadGroupID"),
							Terms = queryLimit.ResourceGroupIDs.Select(i => i.ToString())
						},
					},
				});
			}


            //User access limitations
            if (queryLimit.HideData3SixtyUsers)
            {
                mustNotQueries.Add(new BoolQuery
                {
                    Must = new QueryContainer[] {
                            new TermQuery {
                                Field = new Nest.Field(D3S_FIELD_PREFIX + "Category"),
                                Value = AssetTypeClass.User.ToString()
                            },
                            new TermQuery
                            {
                                Field = new Nest.Field(D3S_FIELD_PREFIX + "Data3SixtyUser"),
                                Value = true
                            }
                        }
                });
            }

            //Additional limitations
            foreach (AggregationFilter limitAggFilter in queryLimit.AggregationFilters)
            {
                string fieldname;
                switch (limitAggFilter.Field)
                {
                    case "Category":
                    case "AssetType":
                        fieldname = D3S_FIELD_PREFIX + limitAggFilter.Field;
                        break;
                    default:
                        fieldname = DYNAMIC_FIELD_PREFIX + limitAggFilter.Field;
                        break;
                }

                mustNotQueries.Add(new TermsQuery
                {
                    Field = new Nest.Field(fieldname),
                    Terms = limitAggFilter.Values
                });
            }
            return mustNotQueries;
        }

        private string MapCategoryToFriendlyName(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            var temp = key.Trim().ToUpperInvariant();

            switch (temp)
            {
                case "BUSINESSASSET":
                    return CommonNames.AssetTypeClass_Business;
                case "TECHNICALASSET":
                    return CommonNames.AssetTypeClass_Technical;
				case "MODEL":
				case "TAXONOMY":
                    return CommonNames.AssetTypeClass_Model;
                case "DIAGRAM":
                    return CommonNames.AssetTypeClass_DiagramAsset;
                case "DOMAIN":
				case "REFERENCE":
					return CommonNames.AssetTypeClass_Reference;
                case "SYNONYM":
                    return CommonNames.AssetTypeClass_GramaticType;
                case "SEMANTICTYPE":
                    return CommonNames.AssetTypeClass_SemanticType; 
				case "POLICY":
					return CommonNames.AssetTypeClass_Policy;
				case "GROUP":
					return CommonNames.AssetTypeClass_Group;
				case "USER":
					return CommonNames.AssetTypeClass_User;
                default:
                    return key;
            }
        }

        /// <summary>
        /// Test if a string fits a GUID pattern
        /// </summary>
        /// <param name="phrase"></param>
        /// <returns>0 - for no GUID match, 1 for partial (begining GUID), 2 for full GUID</returns>
        private int IsPhraseGuid(string phrase)
        {
            var r = new Regex("^[0-9A-F]{8}-([0-9A-F]{4})?(-[0-9A-F]{4})?(-[0-9A-F]{4})?(-[0-9A-F]{12})?", RegexOptions.IgnoreCase);
            if (r.IsMatch(phrase))
            {
                return (phrase.Length == 36) ? 2 : 1;
            }
            return 0;
        }

        public IEnumerable<TypeaheadResult> GetTypeaheadResults(int companyID, string phrase, QueryLimitation queryLimit, int size = 10, string category = "")
        {
            if (string.IsNullOrEmpty(phrase))
            {
                return new List<TypeaheadResult>();
            }

			phrase = phrase.Replace("+", "");
			if (phrase.Length > QueryRequest.SEARCH_TERM_MAX_LENGTH)
            {
                phrase = phrase.Substring(0, QueryRequest.SEARCH_TERM_MAX_LENGTH);
            }

            Nest.Field fldName = new Nest.Field(DYNAMIC_FIELD_PREFIX + "Name");
            Nest.Field fldCategory = new Nest.Field(D3S_FIELD_PREFIX + "Category");
            Nest.Field fldTag = new Nest.Field(D3S_FIELD_PREFIX + "Tags.Value");
            List<QueryContainer> mustClauses = new List<QueryContainer>();
            List<QueryContainer> filterMustQueries = new List<QueryContainer>();
            string tagSearch;

            int isGuid = IsPhraseGuid(phrase);
            if (isGuid == 1)
            {
                mustClauses.Add(new PrefixQuery
                {
                    Field = new Nest.Field(D3S_FIELD_PREFIX + "Uid"),
                    Value = phrase.ToLowerInvariant()
                });
                fldTag = new Nest.Field(D3S_FIELD_PREFIX + "Tags.Uid");
                tagSearch = phrase.ToLowerInvariant();
            }
            else if (isGuid == 2)
            {
                mustClauses.Add(new TermQuery
                {
                    Field = new Nest.Field(D3S_FIELD_PREFIX + "Uid"),
                    Value = phrase.ToLowerInvariant()
                });
                fldTag = new Nest.Field(D3S_FIELD_PREFIX + "Tags.Uid");
                tagSearch = phrase.ToLowerInvariant();
            }
            else
            {
                /* For Typeahead, the search phrase is split into words, all words but the last will be
                 * queried using 'match' and the last word will be 'prefix' or 'match'
                 * For searching tags, an asterisk is appended and a regular 'query_string' query is used 
                 */
                Queue<string> parts = new Queue<string>(phrase.ToLowerInvariant().Split(' '));
                tagSearch = EscapeSpecialCharacters(phrase.ToLowerInvariant()) + (!phrase.EndsWith("*") ? "*" : "");

                while (parts.Count > 0)
                {
                    string part = parts.Dequeue();
                    if (part.Contains("*"))
                    {
                        mustClauses.Add(new SimpleQueryStringQuery
                        {
                            Fields = fldName,
                            Query = part
                        });
                    }
                    else
                    {
                        if (parts.Count == 0) //Last word, search match or prefix
                        {
                            mustClauses.Add(new BoolQuery
                            {
                                MinimumShouldMatch = 1,
                                Should = new QueryContainer[] {
                                    new MatchQuery
                                    {
                                        Field = fldName,
                                        Query = EscapeSpecialCharacters(part)
                                    },
                                    new PrefixQuery
                                    {
                                        Field = fldName,
                                        Value = EscapeSpecialCharacters(part)
                                    }
                                }
                            });

                        }
                        else
                        {
                            mustClauses.Add(new MatchQuery
                            {
                                Field = fldName,
                                Query = EscapeSpecialCharacters(part)
                            });
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(category))
            {
                string[] categories = category.Split(',');
                if (queryLimit.AggregationFilters.Exists(l => l.Field == "d3sCategory"))
                {
                    IEnumerable<string> cats = categories.Except(queryLimit.AggregationFilters.Find(l => l.Field == "d3sCategory").Values);
                    categories = cats.ToArray();
                }

                if (categories.Length > 1)
                {
                    filterMustQueries.Add(new TermsQuery
                    {
                        Field = fldCategory,
                        Terms = categories
                    });
                }
                else
                {
                    filterMustQueries.Add(new TermQuery
                    {
                        Field = fldCategory,
                        Value = categories[0]
                    });
                }
            }

            SearchRequest sReq = new SearchRequest
            {
                Query = new BoolQuery
                {
                    Must = new QueryContainer[] {
                        new BoolQuery{
                            Should = new QueryContainer[] {
                                new BoolQuery {
                                    Must = mustClauses
                                },
								new MatchPhrasePrefixQuery {
									Field = UNDERSCORE_FIELD_PREFIX + "Name",
									Query = phrase
								},
								new NestedQuery {
                                    Path = D3S_FIELD_PREFIX + "Tags",
                                    Query = new BoolQuery{
                                        Must = new QueryContainer[] { new QueryStringQuery {
                                            DefaultField = fldTag,
                                            Query = tagSearch,
                                            AnalyzeWildcard = true
                                        }}
                                    },
                                    InnerHits = new InnerHits {
                                        Highlight = new Highlight {
                                            Fields = new Dictionary<Nest.Field, IHighlightField> { { fldTag, new HighlightField { } } },
                                            Encoder = HighlighterEncoder.Html
                                        }
                                    }
                                }
                            }
                        }
                    },
                    Filter = new QueryContainer[] { new BoolQuery {
                        Must = filterMustQueries,
                        MustNot = FiltersFromLimit(queryLimit)
                    } }
                },
                Size = size
            };

            var searchResults = PerformSearch(companyID, sReq);

            return searchResults.hits.hits.Select(h => new TypeaheadResult
            {
                Name = GetPropertyValue<string>(h._source, DYNAMIC_FIELD_PREFIX + "Name"),
                DisplayName = GetDisplayName(h),
                Group = MapCategoryToFriendlyName(h.d3sCategory),
                Type = GetPropertyValue<string>(h._source, D3S_FIELD_PREFIX + "AssetType"),
                Url = GetPropertyValue<string>(h._source, D3S_FIELD_PREFIX + "Url"),
                Uid = GetGuidPropertyIfExists(h, D3S_FIELD_PREFIX + "Uid"),
                AssetTypeUid = GetGuidPropertyIfExists(h, D3S_FIELD_PREFIX + "AssetTypeUid"),
                Tags = GetTags(h, true)
            });
        }

        private string GetTypeAheadSynonymDisplayType(SearchResultsHitModel h)
        {
            var type = GetPropertyValue<string>(h._source, "SynonymForObject");
            if ((type ?? string.Empty).ToUpperInvariant() == "ARTIFACT")
            {
                return $"{MapCategoryToFriendlyName(type)} - {GetPropertyValue<string>(h._source, "SynonymForObjectType")}";
            }
            return MapCategoryToFriendlyName(type);
        }

        /// <summary>
        /// Gets the search results from elastic search and converts them to index results
        /// </summary>
        /// <param name="companyID"></param>
        /// <param name="phrase"></param>
        /// <returns></returns>
        public IndexResults GetSearchResults(int companyID, string phrase, int size, int from, QueryLimitation queryLimit, string type = "")
        {
            IndexResults result = new IndexResults();
            CreateIndexIfNotExists(companyID);

            Nest.Field fldAssetType = new Nest.Field(D3S_FIELD_PREFIX + "AssetType");
            Nest.Field fldTag = new Nest.Field(D3S_FIELD_PREFIX + "Tags.Value");
            List<QueryContainer> filterMustQueries = new List<QueryContainer>();

            phrase = EscapeSpecialCharacters(phrase);

            if(!string.IsNullOrWhiteSpace(type))
            {
                filterMustQueries.Add(
                    new TermQuery
                    {
                        Field = fldAssetType,
                        Value = type
                    }
                );
            }

            SearchRequest sReq = new SearchRequest
            {
                Query = new BoolQuery
                {
                    Must = new QueryContainer[] {
                        new BoolQuery{
                            Should = new QueryContainer[] {
                                new QueryStringQuery {
                                    Query = phrase,
                                    AnalyzeWildcard = true
                                },
                                new NestedQuery {
                                    Path = D3S_FIELD_PREFIX + "Tags",
                                    Query = new BoolQuery{
                                        Must = new QueryContainer[] { new QueryStringQuery {
                                            DefaultField = fldTag,
                                            Query = phrase,
                                            AnalyzeWildcard = true
                                        }}
                                    },
                                    InnerHits = new InnerHits {
                                        Highlight = new Highlight {
                                            Fields = new Dictionary<Nest.Field, IHighlightField> { { fldTag, new HighlightField { } } },
                                            Encoder = HighlighterEncoder.Html
                                        }
                                    }
                                }
                            }
                        }
                    },
                    Filter = new QueryContainer[] { new BoolQuery {
                        Must = filterMustQueries,
                        MustNot= FiltersFromLimit(queryLimit)
                    } }
                },
                Size = size,
                From = from,
                Sort = new List<ISort>
                {
                    new SortField { Field = "_score", Order = Nest.SortOrder.Descending }
                }
            };

            var searchResults = PerformSearch(companyID, sReq);

            result.Results = searchResults.hits.hits.Select(h => new IndexResult
            {
                Name = GetPropertyValue<string>(h._source, DYNAMIC_FIELD_PREFIX + "Name"),
                DisplayName = GetDisplayName(h),
                Description = GetPropertyValue<string>(h._source, DYNAMIC_FIELD_PREFIX + "Description") ?? GetPropertyValue<string>(h._source, DYNAMIC_FIELD_PREFIX + "description"),
                Group = MapCategoryToFriendlyName(h.d3sCategory),
                ID = h._id,
                NormalizedScore = (searchResults.hits.max_score.GetValueOrDefault() == 0 ? 0 : (h._score / searchResults.hits.max_score.GetValueOrDefault() * 100)),
                Score = h._score,
                Type = GetPropertyValue<string>(h._source, D3S_FIELD_PREFIX + "AssetType"),
                Url = GetPropertyValue<string>(h._source, D3S_FIELD_PREFIX + "Url"),
                Uid = GetGuidPropertyIfExists(h, D3S_FIELD_PREFIX + "Uid"),
                AssetTypeUid = GetGuidPropertyIfExists(h, D3S_FIELD_PREFIX + "AssetTypeUid"),
                Tags = GetTags(h)
            }).ToList();

            result.ElapsedMS.Add("Query", searchResults.took);

            if (searchResults.hits != null)
            {
                result.Matches = searchResults.hits.total;
            }

            return result;
        }

        private SearchResultsModel PerformSearch(int companyID, SearchRequest sReq)
        {
            var client = GetElasticClient(companyID);
            //Because the index model is variable, the LowLevel client is used and the request is turned into a JSON string
            string jsonString = client.RequestResponseSerializer.SerializeToString(sReq);
            var response = client.LowLevel.Search<StringResponse>(GetCompanyIndexName(companyID), "_doc", jsonString);

            if (!response.Success)
            {
                if (response.OriginalException.InnerException.Message == "Unable to connect to the remote server")
                {
                    throw new SearchServerConnectionException(
                        response.OriginalException,
                        string.Join(", ", client.ConnectionSettings.ConnectionPool.Nodes.Select(n => n.Uri.OriginalString)),
                        client.ConnectionSettings.DefaultIndex
                    );
                }
                else
                {
                    throw new SearchResultsException(response.OriginalException);
                }
            }

            return JsonConvert.DeserializeObject<SearchResultsModel>(response.Body);
        }

        private string GetDisplayName(SearchResultsHitModel h)
        {
            var synonymFor = GetPropertyValue<string>(h._source, DYNAMIC_FIELD_PREFIX + "SynonymFor");

            var name = GetPropertyValue<string>(h._source, DYNAMIC_FIELD_PREFIX + "Name");

            if (string.IsNullOrEmpty(synonymFor))
            {
                if ((h.d3sCategory ?? "").ToUpperInvariant() != "ARTIFACT")
                {
                    return name;
                }

                var taxonomy = GetPropertyValue<string>(h._source, DYNAMIC_FIELD_PREFIX + "Taxonomy");

                return (string.IsNullOrEmpty(taxonomy) ? $"{name}" : $"{name} ({taxonomy})");
            }

            var nymType = GetPropertyValue<string>(h._source, DYNAMIC_FIELD_PREFIX + "NymType");

            return $"{name} ({nymType ?? ""} For: {GetTypeAheadSynonymDisplayType(h)}: {synonymFor})";
        }

        private string GetHighlightedNameValueIfExists(SearchResultsHitModel h)
        {
            var synonymFor = GetPropertyValue<string>(h._source, DYNAMIC_FIELD_PREFIX + "SynonymFor");
            var taxonomy = "";

            if ((h.d3sCategory ?? "").ToUpperInvariant() == "ARTIFACT")
            {
                taxonomy = GetPropertyValue<string>(h._source, DYNAMIC_FIELD_PREFIX + "Taxonomy");

                if (!string.IsNullOrEmpty(taxonomy))
                {
                    taxonomy = $" ({taxonomy})";
                }
            }

            if (!string.IsNullOrEmpty(synonymFor))
            {
                var nymType = GetPropertyValue<string>(h._source, DYNAMIC_FIELD_PREFIX + "NymType");

                synonymFor = $" ({nymType ?? ""} For: {GetTypeAheadSynonymDisplayType(h)}: {synonymFor})";
            }

            var highlightVal = GetPropertyValue<string>(h.highlight, DYNAMIC_FIELD_PREFIX + "Name");

            if (!string.IsNullOrEmpty(highlightVal))
            {
                return highlightVal + (synonymFor ?? "") + (taxonomy ?? "");
            }

            return GetPropertyValue<string>(h._source, DYNAMIC_FIELD_PREFIX + "Name") + (synonymFor ?? "") + (taxonomy ?? "");
        }

        private string GetHighlightedPropertyValueIfExists(SearchResultsHitModel h, string propName)
        {
            var highlightVal = GetPropertyValue<string>(h.highlight, propName);

            if (!string.IsNullOrEmpty(highlightVal))
            {
                return highlightVal;
            }

            return GetPropertyValue<string>(h._source, propName);
        }

        private Guid? GetGuidPropertyIfExists(SearchResultsHitModel h, string propName)
        {
            Guid.TryParse(GetPropertyValue<string>(h._source, propName), out var result);
            if (result == Guid.Empty)
            {
                return null;
            }
            return result;
        }

        private List<IndexTag> GetTags(SearchResultsHitModel h, bool onlyHits = false, bool highlightHits = true)
        {
            List<IndexTag> tags = new List<IndexTag>();
            List<IndexTag> highlights = new List<IndexTag>();

            if (h._source == null || h.inner_hits == null)
            {
                return tags;
            }

            if (h.inner_hits.TryGetValue(D3S_FIELD_PREFIX + "Tags", out JToken innerHits))
            {

                if (highlightHits && h.inner_hits != null)
                {
                    highlights = innerHits.SelectTokens("hits.hits[*]")
                        .Select(t => t.ToObject<SearchTagInnerHitsModel>())
                        .Select(hi => new IndexTag { Uid = hi._source.Uid, Value = hi._source.Value, Highlight = hi.GetHighLightValue() })
                        .ToList();
                }

                if (onlyHits && highlightHits)
                {
                    //Since we are only returning hits, no need to also find _source tags
                    return highlights.OrderBy(t => t.Value).ToList();
                }

                //Find _source tags from either inner_hits or _source
                if (onlyHits)
                {
                    tags = innerHits.SelectTokens("hits.hits[*]._source").Select(t => t.ToObject<IndexTag>()).ToList();
                }
                else
                {
                    tags = h._source.SelectTokens(D3S_FIELD_PREFIX + "Tags[*]").Select(t => t.ToObject<IndexTag>()).ToList();
                }

                if (highlightHits)
                {
                    tags = highlights.Union(tags).ToList();
                }
            }

            return tags.OrderBy(t => t.Value).ToList();
        }

        private T GetPropertyValue<T>(JObject _source, string propName)
        {
            if (_source != null)
            {
                JToken jToken;
                if (!_source.TryGetValue(propName, out jToken))
                {
                    jToken = _source.SelectToken(propName);
                }
                if (jToken != null)
                {
                    if (jToken.Type == JTokenType.Array)
                    {
                        return ((JArray)jToken)[0].Value<T>();
                    }
                    return jToken.Value<T>();
                }
            }
            return default(T);
        }

        public void ReIndex(int companyID, IEnumerable<IndexObjectModel> items)
        {
            ClearIndex(companyID);
            AddToIndex(items);
        }

        public void RemoveFromIndex(IndexObjectModel item)
        {
            if (item == null)
            {
                return;
            }

            CreateIndexIfNotExists(item.CompanyID);

            var client = GetElasticClient(item.CompanyID).LowLevel;
            var response = client.Delete<StringResponse>(GetCompanyIndexName(item.CompanyID), "_doc", item.getObjectID());

            if (!response.Success)
            {
                throw new ArgumentException(response.OriginalException.Message);
            }
        }

        public void RemoveFromIndex(IEnumerable<IndexObjectModel> items)
        {
            var firstItem = items.FirstOrDefault();

            if (firstItem == null)
            {
                return;
            }

            var companyId = firstItem.CompanyID;

            CreateIndexIfNotExists(companyId);

            StringBuilder sb = new StringBuilder();

            foreach (var item in items)
            {
                sb.Append("{ \"delete\" : { \"_type\" : \"_doc\", \"_id\" : \"" + item.getObjectID() + "\"}}\n");
            }

            var client = GetElasticClient(companyId).LowLevel;
            var bulkResponse = client.Bulk<StringResponse>(GetCompanyIndexName(companyId), sb.ToString());

            if (!bulkResponse.Success)
            {
                throw new ArgumentException(bulkResponse.OriginalException.Message);
            }

            var result = JObject.Parse(bulkResponse.Body);

            if (result == null)
            {
                throw new ArgumentNullException(OthersError.InvalidResponseData);
            }

            var hasErrors = result.GetValue("errors");

            if (hasErrors.Value<bool>())
            {
                throw new ArgumentNullException(bulkResponse.Body);
            }
        }

        public void UpdateInIndex(IndexObjectModel item, bool withUpsert = false)
        {
            if (item == null)
            {
                return;
            }

            UpdateInIndex(new List<IndexObjectModel> { item }, withUpsert);
        }

        public void UpdateInIndex(IEnumerable<IndexObjectModel> items, bool withUpsert = false)
        {
            var firstItem = items.FirstOrDefault();

            if (firstItem == null)
            {
                return;
            }

            var companyId = firstItem.CompanyID;

            CreateIndexIfNotExists(companyId);

            List<string> postingErrors = new List<string>();

            StringBuilder sb = new StringBuilder();

            foreach (var item in items)
            {
                sb.AppendLine("{ \"update\" : { \"_type\" : \"_doc\", \"_id\" : \"" + item.getObjectID() + "\"}}");
                sb.AppendLine("{ \"doc\": " + CreateDocument(item, true) + (withUpsert ? ", \"doc_as_upsert\" : true" : "") + "}");
            }

            var client = GetElasticClient(companyId).LowLevel;
            var bulkResponse = client.Bulk<StringResponse>(GetCompanyIndexName(companyId), sb.ToString());

            if (!bulkResponse.Success)
            {
                StringBuilder exMessage = new StringBuilder();
                exMessage.AppendLine(bulkResponse.OriginalException.Message);
                exMessage.Append("ES_DebugInformation: ");
                exMessage.AppendLine(bulkResponse.DebugInformation);

                throw new ArgumentException(exMessage.ToString());
            }

            var result = JObject.Parse(bulkResponse.Body);

            if (result == null)
            {
                throw new ArgumentNullException(OthersError.InvalidResponseData);
            }

            var hasErrors = result.GetValue("errors");

            if (hasErrors.Value<bool>())
            {
                foreach (var resultItem in result.GetValue("items"))
                {
                    var errToken = resultItem.SelectToken("index.error");
                    if (errToken != null)
                    {
                        string fault = errToken.ToString();
                        string id = (string)resultItem.SelectToken("index._id");
                        postingErrors.Add(id + ":" + fault);
                    }
                }
                if (postingErrors.Count > 0)
                {
                    throw new ArgumentNullException(OthersError.UpdateIndexIndividualErrors + string.Join(Environment.NewLine, postingErrors.ToArray()));
                }
            }
        }
    }
}
