using d360.core;
using d360.core.entities;
using d360.core.queue;
using d360.core.enums;
using d360.core.resources;
using Dapper;
using Elasticsearch.Net;
using MoreLinq;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;

namespace d360.extensions.search
{
    public class JsonResponseModel
    {
        public JObject Data { get; set; }
        public HttpStatusCode Status { get; set; }
        public string StatusMessage { get; set; }

        public bool IsSuccessStatusCode
        {
            get { return ((int)Status >= 200) && ((int)Status <= 299); }
        }
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
            set
            {
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

        private const string DYNAMIC_FIELD = "fields";
        public const string DYNAMIC_FIELD_PREFIX = DYNAMIC_FIELD + ".";

        private const string D3S_FIELD = "d3s";
        public static readonly string D3S_FIELD_PREFIX = D3S_FIELD + ".";

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

        #region Utility methods

        private static readonly Dictionary<string, string> NoReadMapping =
        new Dictionary<string, string>
        {
            { "R", "NoReadResourceID" },
            { "G", "NoReadGroupID" },
            { "O", "NoReadOrgID" },
        };

        private string CreateDocument(IndexObjectModel item, bool forUpdate = false)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<string, string> d3sFields = new Dictionary<string, string>();
            Dictionary<string, string> d3sNoRead = new Dictionary<string, string>();
            Dictionary<string, string> dynamicFields = item.Fields != null ? item.Fields.Where(i => !string.IsNullOrEmpty(i.Value)).ToDictionary(i => i.Key, i => i.Value) : new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(item.RelativeUrl))
            {
                d3sFields.Add("Url", item.RelativeUrl);
            }
            d3sFields.Add("AssetType", item.AssetType);
            d3sFields.Add("Category", item.Category);
            if (item.Uid.HasValue && item.Uid != Guid.Empty)
            {
                d3sFields.Add("Uid", item.Uid.ToString());
            }
            if (item.AssetTypeUid.HasValue && item.AssetTypeUid != Guid.Empty)
            {
                d3sFields.Add("AssetTypeUid", item.AssetTypeUid.ToString());
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

            if(item.IndexFlags.HasFlag(IndexMode.WithResponsibility))
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

        private string GetCompanyIndexName(int companyID)
        {
            return $"d3s{companyID}";
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
                SearchServerUrl = "192.168.33.16:9200";
            }

            if (string.IsNullOrEmpty(SearchServerUrl))
            {
                throw new ArgumentException(OthersError.NoSearchUrlError);
            }

            var uri = new Uri("http://" + SearchServerUrl);

            return new ConnectionSettings(uri).DefaultIndex(GetCompanyIndexName(companyID));
        }

        /// <summary>
        /// Create an index if it doesnt exist
        /// </summary>
        /// <param name="companyID"></param>
        private void CreateIndexIfNotExists(int companyID)
        {
            var indexName = GetCompanyIndexName(companyID);
            var client = GetElasticClient(companyID);

            if (!client.IndexExists(indexName).Exists)
            {
                CreateIndexDescriptor indexDescriptor = new CreateIndexDescriptor(indexName)
                    .Settings(s => s
                        .NumberOfReplicas(1)
                        .NumberOfShards(2)
                        .Setting("index.mapping.total_fields.limit", IndexFieldLimit)
                    ).Mappings(ms => ms
                        .Map("_doc", m => m
                            .DateDetection(false)
                            .Properties(ps => ps
                                .Object<dynamic>(o => o
                                    .Dynamic(true)
                                    .Name(DYNAMIC_FIELD)
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
                                        .Keyword(s => s.Name("NoReadOrgID"))
                                        .Boolean(b => b.Name("Data3SixtyUser"))
                                        .Text(s => s.Name("Path"))
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
        /// Delete an index if it exists
        /// </summary>
        /// <param name="companyID"></param>
        private void DeleteIndexIfExists(int companyID)
        {
            var indexName = GetCompanyIndexName(companyID);
            //NEST client
            var client = GetElasticClient(companyID);
            if (client.IndexExists(indexName).Exists)
            {
                var response = client.DeleteIndex(indexName);
                if (!response.IsValid)
                {
                    throw new ArgumentException(response.OriginalException.Message);
                }
            }
        }

        private string EscapeValueForDoc(string input, bool removeTags = true)
        {
            if (!string.IsNullOrEmpty(input))
            {
                input = input
                    .Replace("\a", "")
                    .Replace("\b", "")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\f", "")
                    .Replace("\v", "")
                    .Replace("\t", "")
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
                if(firstRun)
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

            List<QueryContainer> termQueries = new List<QueryContainer>();
            termQueries.Add(new TermQuery
            {
                Field = new Nest.Field(D3S_FIELD_PREFIX + "Category"),
                Value = category
            });
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

            var client = GetElasticClient(companyID);
            //Because the index model is variable, the LowLevel client is used and the request is turned into a JSON string
            string jsonString = client.RequestResponseSerializer.SerializeToString(sReq);

            //Refresh all shards after the query delete to avoid 409 versioning conflicts
            DeleteByQueryRequestParameters requestParameters = new DeleteByQueryRequestParameters
            {
                Refresh = true
            };
            StringResponse deleteResponse = client.LowLevel.DeleteByQuery<StringResponse>(GetCompanyIndexName(companyID), jsonString, requestParameters);

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

            var client = GetElasticClient(companyID);
            //Because the index model is variable, the LowLevel client is used and the request is turned into a JSON string
            string jsonString = client.RequestResponseSerializer.SerializeToString(sReq);
            StringResponse deleteResponse = client.LowLevel.DeleteByQuery<StringResponse>(GetCompanyIndexName(companyID), jsonString);

            if (!deleteResponse.Success)
            {
                throw new ArgumentException(deleteResponse.OriginalException.Message);
            }
        }

        private bool IsElasticSearchSpecialChar(char ch)
        {
            if (ch == '\\' || ch == '/' || ch == ':' || ch == '^' || ch == '~' || ch == ')' || ch == '(' ||
               ch == '!' || ch == '[' || ch == ']' || ch == '{' || ch == '}' || ch == '-') return true;

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
            switch(strategy)
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
                    if (phrase.EndsWith("*"))
                    {
                        strategy = STRATEGY_MatchPhrasePrefix;
                        phrase = phrase.TrimEnd('*');
                    }
                    else
                        strategy = STRATEGY_QueryString;
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
                if(fieldFilter.Values == null || fieldFilter.Values.Length == 0)
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
                            Must = values.Select(v => {
                                QueryContainer q = new NestedQuery {
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
                    } else
                    {
                        qry = new NestedQuery
                        {
                            Path = path,
                            Query = new BoolQuery
                            {
                                Should = values.Select(v => {
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
                            Must = fieldFilter.Values.Select(v => {
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
                                Should = fieldFilter.Values.Select(v => {
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
                    var segmentQueries = values.Select(v => {
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
                    Nest.Field fld = new Nest.Field(DYNAMIC_FIELD_PREFIX + fieldFilter.Field);
                    if(fieldFilter.MatchWords)
                    {
                        qry = new MatchPhraseQuery
                        {
                            Field = fld,
                            Query = values.First()
                        };
                    } else
                    {
                        string p = fieldFilter.Values.First();
                        if(p.Contains("*"))
                        {
                            if (p.EndsWith("*")) //If we have trailing *, remove before escaping
                            {
                                p = p.Remove(p.Length - 1);
                            }
                            p = EscapeSpecialCharacters(p) + "*";

                            qry = new QueryStringQuery
                            {
                                Fields = fld,
                                Query = p,
                                AnalyzeWildcard = true
                            };
                        }
                        else
                        {
                            qry = new MatchPhrasePrefixQuery
                            {
                                Field = fld,
                                Query = values.First()
                            };
                        }
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
                    case "d3sCategory":
                        fieldname = D3S_FIELD_PREFIX + "Category";
                        break;
                    case "d3sAssetType":
                        fieldname = D3S_FIELD_PREFIX + "AssetType";
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

        public IndexResults GetSearchResultsWithAggregation(int companyID, int resourceID, QueryRequest queryRequest, List<IndexTypeList> categories, QueryLimitation queryLimit)
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

            var client = GetElasticClient(companyID);
            //Because the index model is variable, the LowLevel client is used and the request is turned into a JSON string
            string jsonString = client.RequestResponseSerializer.SerializeToString(sReq);
            var response = client.LowLevel.Search<StringResponse>(GetCompanyIndexName(companyID), "_doc", jsonString);

            if (!response.Success)
            {
                throw new ArgumentException(response.OriginalException.Message);
            }

            var searchResults = JsonConvert.DeserializeObject<SearchResultsModel>(response.Body);

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
                Explanation = queryRequest.Explain ? h._explanation.ToString() : ""
            }).ToList();


            if (searchResults.aggregations != null && searchResults.aggregations.all_types != null && searchResults.aggregations.all_types.buckets != null)
            {
                categories.AddRange(searchResults.aggregations.all_types.buckets.Select(h => new IndexTypeList
                {
                    Name = h.key,
                    DisplayName = MapCategoryToFriendlyName(h.key),
                    ResultCount = h.doc_count,
                    Categories = h.category?.buckets.Select(c => new IndexCategory
                    {
                        Name = c.key,
                        ResultCount = c.doc_count
                    }).OrderBy(x => x.Name).ToList()
                }).OrderBy(x => x.DisplayName));
            }

            result.ElapsedMS = searchResults.took;

            if (searchResults.hits != null)
            {
                result.Matches = searchResults.hits.total;
            }

            return result;
        }

        /**
         * Perform a search that counts total items in index and buckets categories
         */
        public IndexResults GetStatusSearch(int companyID, List<IndexTypeList> categories, bool withTypes = false)
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

            var client = GetElasticClient(companyID);
            //Because the index model is variable, the LowLevel client is used and the request is turned into a JSON string
            string jsonString = client.RequestResponseSerializer.SerializeToString(sReq);
            var response = client.LowLevel.Search<StringResponse>(GetCompanyIndexName(companyID), "_doc", jsonString);

            if (!response.Success)
            {
                throw new ArgumentException(response.OriginalException.Message);
            }

            var searchResults = JsonConvert.DeserializeObject<SearchResultsModel>(response.Body);

            if (searchResults.aggregations.all_types != null && searchResults.aggregations.all_types.buckets != null)
            {
                categories.AddRange(searchResults.aggregations.all_types.buckets.Select(h => new IndexTypeList
                {
                    Name = h.key,
                    DisplayName = MapCategoryToFriendlyName(h.key),
                    ResultCount = h.doc_count,
                    Categories = h.category?.buckets.Select(c => new IndexCategory
                    {
                        Name = c.key,
                        ResultCount = c.doc_count
                    }).OrderBy(x => x.Name).ToList()
                }).OrderBy(x => x.DisplayName));
            }

            result.ElapsedMS = searchResults.took;

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

            var client = GetElasticClient(companyID);
            //Because the index model is variable, the LowLevel client is used and the request is turned into a JSON string
            string jsonString = client.RequestResponseSerializer.SerializeToString(sReq);
            var response = client.LowLevel.Search<StringResponse>(GetCompanyIndexName(companyID), "_doc", jsonString);

            if (!response.Success)
            {
                throw new ArgumentException(response.OriginalException.Message);
            }

            var searchResults = JsonConvert.DeserializeObject<SearchResultsModel>(response.Body);

            searchResults.aggregations.all_types?.buckets?.ForEach(b => {
                result.Add(new IndexableCount { ClassName = b.key, AssetTypeUid = Guid.Empty, CurrentCount = b.doc_count });
                result.AddRange(b.category?.buckets?.Select(t => new IndexableCount { ClassName = b.key, AssetTypeUid = Guid.Parse(t.key), CurrentCount = t.doc_count }));
            });

            return result;
        }

        private List<QueryContainer> FiltersFromLimit(QueryLimitation queryLimit)
        {
            List<QueryContainer> mustNotQueries = new List<QueryContainer>
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
                new TermsQuery
                {
                    Field = new Nest.Field(D3S_FIELD_PREFIX + "NoReadOrgID"),
                    Terms = queryLimit.ResourceOrgIDs.Select(i => i.ToString())
                }
            };

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
                    case "d3sCategory":
                        fieldname = D3S_FIELD_PREFIX + "Category";
                        break;
                    case "d3sAssetType":
                        fieldname = D3S_FIELD_PREFIX + "AssetType";
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
                case "TAXONOMY":
                    return CommonNames.AssetTypeClass_Model;
                case "DIAGRAM":
                    return "Diagram Asset";
                case "DOMAIN":
                    return "Reference";
                case "SYNONYM":
                    return "Grammatic Type";
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

        public IEnumerable<TypeaheadResult> GetTypeaheadResults(int companyID, int resourceID, string phrase, QueryLimitation queryLimit, int size = 10, string category = "")
        {
            if (string.IsNullOrEmpty(phrase))
            {
                return new List<TypeaheadResult>();
            }

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
                        mustClauses.Add(new SimpleQueryStringQuery {
                            Fields = fldName,
                            Query = part
                        });
                    }
                    else
                    {
                        if(parts.Count == 0) //Last word, search match or prefix
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
                    filterMustQueries.Add(new TermsQuery {
                        Field = fldCategory,
                        Terms = categories
                    });
                }
                else
                {
                    filterMustQueries.Add(new TermQuery {
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

            var client = GetElasticClient(companyID);
            //Because the index model is variable, the LowLevel client is used and the request is turned into a JSON string
            string jsonString = client.RequestResponseSerializer.SerializeToString(sReq);
            var response = client.LowLevel.Search<StringResponse>(GetCompanyIndexName(companyID), "_doc", jsonString);

            if (!response.Success)
                throw new ArgumentException(response.OriginalException.Message);

            var searchResults = JsonConvert.DeserializeObject<SearchResultsModel>(response.Body);

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
        /// <param name="resourceID"></param>
        /// <param name="phrase"></param>
        /// <returns></returns>
        public IndexResults GetSearchResults(int companyID, int resourceID, string phrase, int size, int from, string type = "")
        {
            IndexResults result = new IndexResults();
            CreateIndexIfNotExists(companyID);

            Nest.Field fldAssetType = new Nest.Field(D3S_FIELD_PREFIX + "AssetType");
            Nest.Field fldTag = new Nest.Field(D3S_FIELD_PREFIX + "Tags.Value");

            phrase = EscapeSpecialCharacters(phrase);

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
                    Filter = new QueryContainer[] {
                        new TermQuery{
                            Field = fldAssetType,
                            Value = type
                        }
                    }
                },
                Size = size,
                From = from,
                Sort = new List<ISort>
                {
                    new SortField { Field = "_score", Order = Nest.SortOrder.Descending }
                }
            };

            var client = GetElasticClient(companyID);
            //Because the index model is variable, the LowLevel client is used and the request is turned into a JSON string
            string jsonString = client.RequestResponseSerializer.SerializeToString(sReq);
            var response = client.LowLevel.Search<StringResponse>(GetCompanyIndexName(companyID), "_doc", jsonString);

            if (!response.Success)
                throw new ArgumentException(response.OriginalException.Message);

            var searchResults = JsonConvert.DeserializeObject<SearchResultsModel>(response.Body);

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

            result.ElapsedMS = searchResults.took;

            if (searchResults.hits != null)
                result.Matches = searchResults.hits.total;

            return result;
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

            if (!string.IsNullOrEmpty(highlightVal)) return highlightVal;

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
                 if(!_source.TryGetValue(propName, out jToken)) {
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

            if (firstItem == null) return;

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
            if (item == null) return;

            UpdateInIndex(new List<IndexObjectModel> { item }, withUpsert);
        }

        public void UpdateInIndex(IEnumerable<IndexObjectModel> items, bool withUpsert = false)
        {
            var firstItem = items.FirstOrDefault();

            if (firstItem == null) return;

            var companyId = firstItem.CompanyID;

            CreateIndexIfNotExists(companyId);

            List<string> postingErrors = new List<string>();

            StringBuilder sb = new StringBuilder();

            foreach (var item in items)
            {
                sb.AppendLine("{ \"update\" : { \"_type\" : \"_doc\", \"_id\" : \"" + item.getObjectID() + "\"}}");
                sb.AppendLine("{ \"doc\": " + CreateDocument(item, true) + (withUpsert ? ", \"doc_as_upsert\" : true" : "" ) + "}");
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

            if (result == null) {
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
