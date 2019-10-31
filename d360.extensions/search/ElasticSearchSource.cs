using d360.core;
using d360.core.entities;
using d360.core.queue;
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
        public SearchAggregationCategoryTypeModel category {get; set;}        
    }

    public class SearchAggregationCategoryTypeModel {
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
            if(highlight != null && highlight.TryGetValue("d3sTags.Value", out JToken jToken))
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
        public string d3sGroup {
            get
            {
                if (_source != null && _source.TryGetValue("d3sGroup", out JToken jToken))
                {
                    return jToken.Value<string>();
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
    }


    public class ElasticSearchSource : ISearchSource
    {
        private const string DEFAULT_SEARCH_SERVER = "search1-d3s.cloudapp.net:9200";
        private const int BULK_BATCH_SIZE = 5000;

        private const string MAPPING_VERSION_5 = "{\"settings\": {\"index\": {\"number_of_shards\": 2,\"number_of_replicas\": 1}},\"mappings\": {\"_doc\": {\"date_detection\": false, \"properties\": {\"d3sGroup\": {\"type\": \"keyword\"},	\"d3sTags\": { \"type\": \"nested\", \"properties\": { \"Uid\": { \"type\": \"keyword\" }}}, \"Type\": { \"type\": \"keyword\" }, \"Uid\": { \"type\": \"keyword\"	}, \"AssetTypeUid\": { \"type\": \"keyword\"}}}}}";

        protected string SearchServerUrl { get; set; }

        public int? IndexFieldLimit { get; set; }

        #region Utility methods

        private JObject CreateDocument(IndexObjectModel item)
        {
            var doc = new JObject
            {
                { "Url", item.RelativeUrl }
            };
            if (item.Fields != null)
            {
                foreach (var key in item.Fields.Keys)
                {
                    if (item.Fields[key] != null)
                        doc.Add(key, item.Fields[key].ToString());
                }
            }
            return doc;
        }

        private string GetCompanyIndexName(int companyID)
        {
            return $"d3s{companyID}";
        }

        private string CreateItemID(IndexObjectModel item)
        {
            return item.getObjectID();            
        }

        private ConnectionSettings GetConnectionSettings(int companyID)
        {
            using (var community = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
            {
                var db = community.Query<DatabaseServer>(@"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id", new { id = companyID }).SingleOrDefault();

                SearchServerUrl = db.SearchServer ?? DEFAULT_SEARCH_SERVER;
            }

            if (string.IsNullOrEmpty(SearchServerUrl)) throw new Exception("DEV ERROR - NO SEARCH BASE URL SPECIFIED.");

            var uri = new Uri("http://"+SearchServerUrl);

            return new ConnectionSettings(uri).DefaultIndex(GetCompanyIndexName(companyID));
        }

        /// <summary>
        /// Create an index if id doesnt exist
        /// </summary>
        /// <param name="companyID"></param>
        private void CreateIndexIfNotExists(int companyID)
        {
            var indexName = GetCompanyIndexName(companyID);
            //NEST client
            var client = new ElasticClient(GetConnectionSettings(companyID));
            if (!client.IndexExists(indexName).Exists)
            {
                string esSettings = MAPPING_VERSION_5;
                if (IndexFieldLimit.HasValue)
                {
                    esSettings = esSettings.Replace("\"number_of_replicas\": 1", "\"number_of_replicas\": 1, \"mapping.total_fields.limit\" : " + IndexFieldLimit);
                }
                var response = client.LowLevel.IndicesCreate<CreateResponse>(indexName, esSettings);
                if(!response.IsValid)
                    throw new ApplicationException(response.OriginalException.Message);
            }

        }

        /// <summary>
        /// Gets version number from Elastic server
        /// </summary>
        /// <param name="companyID"></param>
        public Version GetElasticVersion(int companyID)
        {
            Version ver = null;
            var client = new ElasticLowLevelClient(GetConnectionSettings(companyID));
            var response = client.Info<StringResponse>();

            if(response.Success)
            {
                JObject result = JObject.Parse(response.Body);
                if (!Version.TryParse((string)result.SelectToken("version.number"), out ver))
                {
                    throw new ApplicationException("Could not determine server version");
                }
            }
            return ver;
        }

        public int GetTotalRecordCount(int companyID)
        {
            int count = -1;
            var client = new ElasticLowLevelClient(GetConnectionSettings(companyID));
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
            var client = new ElasticClient(GetConnectionSettings(companyID));
            if(client.IndexExists(indexName).Exists)
            {
                var response = client.DeleteIndex(indexName);
                if(!response.IsValid)
                    throw new ApplicationException(response.OriginalException.Message);
            }
        }

        private string EscapeValueForDoc(string input)
        {
            input = input.Replace("\r", "").Replace("\n", "").Replace("\v", "").Replace("\t", "").Replace("\\", "\\\\").Replace("\"", "\\\"");
            input = HtmlUtilities.RemoveTags(input);
            return input;
        }

        #endregion

        public void AddToIndex(AddToIndexModel item)
        {
            AddToIndex(new List<AddToIndexModel> { item });
        }

        public void AddToIndex(IEnumerable<AddToIndexModel> items)
        {
            var firstItem = items.FirstOrDefault();

            if (firstItem == null) return;         
            
            var companyId = firstItem.CompanyID;
            
            CreateIndexIfNotExists(companyId);
            
            foreach (var batch in items.Batch(BULK_BATCH_SIZE))
            {                
                var sb = new StringBuilder();

                var indexName = GetCompanyIndexName(companyId);
                foreach (var item in batch)
                {
                    sb.Append("{\"index\":{\"_id\":\"");                                        
                    sb.Append(item.getObjectID());
                    sb.Append("\",\"_type\":\"");
                    sb.Append( "_doc");
                    sb.Append("\" } }\n");
                    sb.Append("{\"Url\" : \"");
                    sb.Append(item.RelativeUrl);
                    sb.Append("\"");
                    sb.Append(",\"d3sGroup\":\"");
                    sb.Append(item.Group);
                    sb.Append("\"");

                    if (item.Fields.Any())
                        sb.Append(",");

                    bool bFirst = true;
                    foreach (var f in item.Fields)
                    {
                        if (string.IsNullOrEmpty(f.Value))
                            continue;

                        if (!bFirst)
                            sb.Append(',');
                        else
                            bFirst = false;

                        sb.Append("\"");
                        sb.Append(f.Key);
                        sb.Append("\":\"");
                        sb.Append(EscapeValueForDoc(f.Value));
                        sb.Append("\"");
                    }

                    if(item.Tags != null && item.Tags.Any())
                    {
                        string[] tags = item.Tags.Select(t => "{ \"Uid\": \"" + t.Key + "\", \"Value\": \"" + EscapeValueForDoc(t.Value) + "\"}").ToArray();
                        sb.Append(", \"d3sTags\":[");
                        sb.Append(string.Join(",", tags));
                        sb.Append("]");
                    }

                    sb.Append("}\n");
                }
                sb.Append("\n");

                var client = new ElasticLowLevelClient(GetConnectionSettings(companyId));
                var bulkResponse = client.Bulk<StringResponse>(GetCompanyIndexName(companyId), sb.ToString());

                if (!bulkResponse.Success)
                    throw new ApplicationException(bulkResponse.OriginalException.Message);

                var result = JObject.Parse(bulkResponse.Body);

                if (result == null) throw new Exception("Invalid response no data");

                var hasErrors = result.GetValue("errors");

                if (hasErrors.Value<bool>())
                    throw new Exception(bulkResponse.Body);                
            }     
        }

        public void ClearIndex(int companyID)
        {
            DeleteIndexIfExists(companyID);

            CreateIndexIfNotExists(companyID);            
        }

     
        public void ClearIndex(int companyID, string group)
        {
            CreateIndexIfNotExists(companyID);

            var client = new ElasticLowLevelClient(GetConnectionSettings(companyID));
            StringResponse deleteResponse;
            deleteResponse = client.DeleteByQuery<StringResponse>(GetCompanyIndexName(companyID), PostData.Serializable(new
            {
                query = new
                {
                    term = new
                    {
                        d3sGroup = group
                    }
                }
            }));

            if (!deleteResponse.Success)
                throw new ApplicationException(deleteResponse.OriginalException.Message);
        }
      
        public IEnumerable<string> GetSearchPhrases(int companyID, string term, int maxResults)
        {
            CreateIndexIfNotExists(companyID);
            return null;
        }

        private bool IsElasticSearchSpecialChar(char ch)
        {
            if (ch == '\\' || ch == '/' || ch == ':' || ch == '^' || ch == '~' || ch == ')' || ch == '(' ||
               ch == '!' || ch == '[' || ch == ']' || ch == '{' || ch == '}' || ch == '-' ) return true;

            return false;
        }

        private string EscapeSpecialCharacters(string phrase)
        {
            if (string.IsNullOrEmpty(phrase)) return "";
            bool padWithQuotes = false;
                        
            if(phrase.Contains('"'))
            {
                //special rules for quotes.  If quotes are in the string they must be in pairs at begining and end
                if((phrase.Length > 0) && (phrase[0] == '"') && (phrase[phrase.Length-1] == '"'))
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

            if (phrase.Any( ch => IsElasticSearchSpecialChar(ch)))
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

            if(padWithQuotes)
                phrase = "\\\"" + phrase + "\\\"";

            return phrase;
        }
        /// <summary>
        /// Gets the search results from elastic search and converts them to index results
        /// </summary>
        /// <param name="companyID"></param>
        /// <param name="resourceID"></param>
        /// <param name="phrase"></param>
        /// <returns></returns>
        public IndexResults GetSearchResultsWithCategory(int companyID, int resourceID, string phrase, int size, int from, List<IndexTypeList> categories, string group = "", string type = "", string advancedFilterJSON = "")
        {
            IndexResults result = new IndexResults();

            Nest.Field fldName = new Nest.Field("Name");
            Nest.Field fldGroup = new Nest.Field("d3sGroup");
            Nest.Field fldType = new Nest.Field("Type");
            Nest.Field fldTag = new Nest.Field("d3sTags.Value");

            string tagSearch = "";
            QueryBase coreQuery = null;
            List<QueryContainer> filterQueries = new List<QueryContainer>();

            if (!string.IsNullOrEmpty(phrase)) {
                //Regular search
                if (phrase.StartsWith("'") && phrase.EndsWith("'"))
                {
                    phrase = EscapeSpecialCharacters(phrase.Trim('\''));
                    coreQuery = new MatchPhraseQuery
                    {
                        Field = fldName,
                        Query = phrase
                    };
                    tagSearch = phrase;
                }
                else
                {
                    if(phrase.EndsWith("*")) //If we have trailing *, remove before escaping
                        phrase = phrase.Remove(phrase.Length - 1);
                    phrase = EscapeSpecialCharacters(phrase) + "*";

                    coreQuery = new QueryStringQuery
                    {
                        Query = phrase
                    };
                    tagSearch = phrase;
                }
            }
            else if (!string.IsNullOrEmpty(advancedFilterJSON))
            {
                //Advanced search
                //deserialize advanced search parameters
                var advFilters = JsonConvert.DeserializeObject<List<AdvancedSearchParameters>>(advancedFilterJSON);
                
                var compositeSearchTerm = string.Empty;
                SearchConnector con = SearchConnector.And;

                foreach (var item in advFilters)
                {
                    if (string.IsNullOrEmpty(item.value)) continue;

                    var field = item.field;
                    if (field == "_type")
                        field = "d3sGroup";
                    else if (field == "d3sTags")
                    {
                        tagSearch = EscapeSpecialCharacters(item.value);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(compositeSearchTerm)) compositeSearchTerm += $" {con.ToString().ToUpper()} ";
                    con = item.connector;

                    var searchTerm = EscapeSpecialCharacters(item.value);
                    if (item.exact)
                    {
                        searchTerm = searchTerm.Replace("\"", "");
                        searchTerm = "\"" + searchTerm + "\"";
                    }
                    else if (!searchTerm.EndsWith("*"))
                    {
                        //Not exact, so append * to searchTerm if it does not already end with *
                        searchTerm += "*";
                    }

                    compositeSearchTerm += $"{field}:{searchTerm}";
                }
                coreQuery = new QueryStringQuery
                {
                    Query = compositeSearchTerm
                };
            }

            //If neither advanced nor a phrase is available, return an empty result set
            if (coreQuery == null)
                return result;

            if (!string.IsNullOrEmpty(type))
            {
                string[] types = type.Split(',');
                if (types.Length > 1)
                {
                    filterQueries.Add(new TermsQuery {
                        Field = fldGroup,
                        Terms = types
                    });
                }
                else
                {
                    filterQueries.Add(new TermQuery
                    {
                        Field = fldGroup,
                        Value = type,
                    });
                }
            }
            //if a group was specified filter by it
            if (!string.IsNullOrEmpty(group))
            {
                filterQueries.Add(new TermQuery
                {
                    Field = fldType,
                    Value = group,
                });
            }

            SearchRequest sReq = new SearchRequest
            {
                Query = new BoolQuery
                {
                    Must = new QueryContainer[] {
                        new BoolQuery{
                            Should = new QueryContainer[] {
                                coreQuery, 
                                new NestedQuery {
                                    Path = "d3sTags",
                                    Query = new BoolQuery{
                                        Must = new QueryContainer[] { new QueryStringQuery {
                                            DefaultField = fldTag,
                                            Query = tagSearch
                                        }}
                                    },
                                    InnerHits = new InnerHits {
                                        Highlight = new Highlight {
                                            Fields = new Dictionary<Nest.Field, IHighlightField> { { fldTag, new HighlightField { } } }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    Filter = new QueryContainer[] { new BoolQuery {
                        Must = filterQueries
                    } }
                },
                Highlight = new Highlight {
                    Fields = new Dictionary<Nest.Field, IHighlightField> { { fldName, new HighlightField {
                        PreTags = new [] { "<em class='search-highlight'>" },
                        PostTags = new [] { "</em>" },
                        NumberOfFragments = 0
                    } } },
                    RequireFieldMatch = false
                },
                From = from,
                Size = size
            };

            // if no group filter then we need to get list of categories
            if (string.IsNullOrEmpty(group)) { 
                //size=0 intepreted as integer.MAX_VALUE deprecated in ES 2.4.0.
                //Using 2000 for categories and 20 for Group/asset class
                sReq.Aggregations = new TermsAggregation("all_types")
                {
                    Field = fldGroup,
                    Size = 20,
                    Aggregations = new TermsAggregation("category")
                    {
                        Field = fldType,
                        Size = 2000
                    }
                };
            }

            var client = new ElasticClient(GetConnectionSettings(companyID));
            //Because the index model is variable, the LowLevel client is used and the request is turned into a JSON string
            string jsonString = client.RequestResponseSerializer.SerializeToString(sReq);
            var response = client.LowLevel.Search<StringResponse>(GetCompanyIndexName(companyID), "_doc", jsonString);

            if (!response.Success)
                throw new ApplicationException(response.OriginalException.Message);

            var searchResults = JsonConvert.DeserializeObject<SearchResultsModel>(response.Body);

            result.Results = searchResults.hits.hits.Select(h => new IndexResult {
                Name = GetHighlightedNameValueIfExists(h),
                DisplayName = GetDisplayName(h),
                Description = GetHighlightedPropertyValueIfExists(h, "Description"),
                Group = MapGroupToFriendlyName(h.d3sGroup),
                ID = h._id,
                NormalizedScore = (searchResults.hits.max_score.GetValueOrDefault() == 0 ? 0 : (h._score/searchResults.hits.max_score.GetValueOrDefault()*100)),
                Score = h._score,
                Type = GetHighlightedPropertyValueIfExists(h, "Type"),
                Url = GetHighlightedPropertyValueIfExists(h, "Url"),
                Uid = GetGuidPropertyIfExists(h, "Uid"),
                AssetTypeUid = GetGuidPropertyIfExists(h, "AssetTypeUid"),
                Tags = GetTags(h)
            }).ToList();


            if (searchResults.aggregations != null && searchResults.aggregations.all_types != null && searchResults.aggregations.all_types.buckets != null)
            {
                categories.AddRange(searchResults.aggregations.all_types.buckets.Select(h => new IndexTypeList
                {
                    Name = h.key,
                    DisplayName = MapGroupToFriendlyName(h.key),
                    ResultCount = h.doc_count,
                    Categories = h.category?.buckets.Select(c => new IndexCategory
                    {
                        Name = c.key,
                        ResultCount = c.doc_count
                    }).OrderBy(x =>x.Name).ToList()
                }).OrderBy(x =>x.DisplayName));
            }
            
            result.ElapsedMS = searchResults.took;

            if(searchResults.hits != null)
                result.Matches = searchResults.hits.total;

            return result;
        }

        public IndexResults GetSearchResultsWithAggregation(int companyID, int resourceID, QueryRequest queryRequest, List<IndexTypeList> categories)
        {
            IndexResults result = new IndexResults();

            Nest.Field fldName = new Nest.Field("Name");
            Nest.Field fldGroup = new Nest.Field("d3sGroup");
            Nest.Field fldType = new Nest.Field("Type");
            Nest.Field fldTag = new Nest.Field("d3sTags.Value");

            string tagSearch = "";
            bool tagMust = false;
            List<QueryContainer> shouldQueries = new List<QueryContainer>();
            List<QueryContainer> mustQueries = new List<QueryContainer>();
            List<QueryContainer> filterQueries = new List<QueryContainer>();

            string phrase = queryRequest.Term;
            if (!string.IsNullOrEmpty(phrase))
            {
                //Regular search
                if (phrase.StartsWith("'") && phrase.EndsWith("'"))
                {
                    phrase = EscapeSpecialCharacters(phrase.Trim('\''));
                    shouldQueries.Add(new MatchPhraseQuery
                    {
                        Field = fldName,
                        Query = phrase
                    });
                    tagSearch = phrase;
                }
                else
                {
                    if (phrase.EndsWith("*")) //If we have trailing *, remove before escaping
                        phrase = phrase.Remove(phrase.Length - 1);
                    phrase = EscapeSpecialCharacters(phrase) + "*";

                    shouldQueries.Add(new QueryStringQuery
                    {
                        Query = phrase
                    });
                    tagSearch = phrase;
                }
            }

            foreach(FieldFilter fieldFilter in queryRequest.FieldFilters)
            {
                if (string.IsNullOrEmpty(fieldFilter.Phrase))
                    continue;

                Nest.Field fld;
                switch (fieldFilter.Field)
                {
                    case "d3sTags":
                        tagSearch = EscapeSpecialCharacters(fieldFilter.Phrase);
                        tagMust = true;
                        continue;
                    case "_type":
                        fld = new Nest.Field("d3sGroup");
                        break;
                    default:
                        fld = new Nest.Field(fieldFilter.Field);
                        break;
                }
                if(fieldFilter.MatchWords)
                {
                    filterQueries.Add(new MatchPhraseQuery
                    {
                        Field = fld,
                        Query = fieldFilter.Phrase
                    });
                } else
                {
                    string p = fieldFilter.Phrase;
                    if (p.EndsWith("*")) //If we have trailing *, remove before escaping
                        p = p.Remove(p.Length - 1);
                    p = EscapeSpecialCharacters(p) + "*";

                    filterQueries.Add(new QueryStringQuery
                    {
                        Fields = fld,
                        Query = p
                    });

                }
            }

            //Tag query
            if(tagSearch != "")
            {
                NestedQuery tagQuery = new NestedQuery
                {
                    Path = "d3sTags",
                    Query = new BoolQuery
                    {
                        Must = new QueryContainer[] { new QueryStringQuery {
                                            DefaultField = fldTag,
                                            Query = tagSearch
                                        }}
                    },
                    InnerHits = new InnerHits
                    {
                        Highlight = new Highlight
                        {
                            Fields = new Dictionary<Nest.Field, IHighlightField> { { fldTag, new HighlightField { } } }
                        }
                    }
                };
                if (tagMust)
                    filterQueries.Add(tagQuery);
                else
                    shouldQueries.Add(tagQuery);
            }

            //If neither advanced nor a phrase is available, return an empty result set
            if (shouldQueries.Count == 0)
                return result;

            //No need to ignore filters with empty values. NEST does that for us
            foreach (AggregationFilter aggFilter in queryRequest.AggregationFilters)
            {
                filterQueries.Add(new TermsQuery
                {
                    Field = new Nest.Field(aggFilter.Field),
                    Terms = aggFilter.Values
                });
            }

            SearchRequest sReq = new SearchRequest
            {
                Query = new BoolQuery
                {
                    Must = new QueryContainer[] {
                        new BoolQuery{
                            Should = shouldQueries,
                            Must = mustQueries
                        }
                    },
                    Filter = new QueryContainer[] { new BoolQuery {
                        Must = filterQueries
                    } }
                },
                Highlight = new Highlight
                {
                    Fields = new Dictionary<Nest.Field, IHighlightField> { { fldName, new HighlightField {
                        PreTags = new [] { "<em class='search-highlight'>" },
                        PostTags = new [] { "</em>" },
                        NumberOfFragments = 0
                    } } },
                    RequireFieldMatch = false
                },
                From = queryRequest.From,
                Size = queryRequest.Size
            };

            // determine if we need aggreagtions
            foreach(string aggregation in queryRequest.Aggregations)
            {
                if(aggregation == "category")
                {
                    //size=0 intepreted as integer.MAX_VALUE deprecated in ES 2.4.0.
                    //Using 2000 for EX6 for now. @TODO: Consider using Composite aggreation
                    sReq.Aggregations = new TermsAggregation("all_types")
                    {
                        Field = fldGroup,
                        Aggregations = new TermsAggregation("category")
                        {
                            Field = fldType,
                            Size = 2000
                        }
                    };
                }
            }

            var client = new ElasticClient(GetConnectionSettings(companyID));
            //Because the index model is variable, the LowLevel client is used and the request is turned into a JSON string
            string jsonString = client.RequestResponseSerializer.SerializeToString(sReq);
            var response = client.LowLevel.Search<StringResponse>(GetCompanyIndexName(companyID), "_doc", jsonString);

            if (!response.Success)
                throw new ApplicationException(response.OriginalException.Message);

            var searchResults = JsonConvert.DeserializeObject<SearchResultsModel>(response.Body);

            result.Results = searchResults.hits.hits.Select(h => new IndexResult
            {
                Name = GetHighlightedNameValueIfExists(h),
                DisplayName = GetDisplayName(h),
                Description = GetHighlightedPropertyValueIfExists(h, "Description"),
                Group = MapGroupToFriendlyName(h.d3sGroup),
                ID = h._id,
                NormalizedScore = (searchResults.hits.max_score.GetValueOrDefault() == 0 ? 0 : (h._score / searchResults.hits.max_score.GetValueOrDefault() * 100)),
                Score = h._score,
                Type = GetHighlightedPropertyValueIfExists(h, "Type"),
                Url = GetHighlightedPropertyValueIfExists(h, "Url"),
                Uid = GetGuidPropertyIfExists(h, "Uid"),
                AssetTypeUid = GetGuidPropertyIfExists(h, "AssetTypeUid"),
                Tags = GetTags(h)
            }).ToList();


            if (searchResults.aggregations != null && searchResults.aggregations.all_types != null && searchResults.aggregations.all_types.buckets != null)
            {
                categories.AddRange(searchResults.aggregations.all_types.buckets.Select(h => new IndexTypeList
                {
                    Name = h.key,
                    DisplayName = MapGroupToFriendlyName(h.key),
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
                result.Matches = searchResults.hits.total;

            return result;
        }

        private string MapGroupToFriendlyName(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            var temp = key.Trim().ToUpper();

            switch (temp)
            {
                case "FUSIONATTRIBUTES":
                    return "Fusion";
                case "BUSINESSASSET":
                    return CommonNames.AssetTypeClass_Business;
                case "TECHNICALASSET":
                    return CommonNames.AssetTypeClass_Technical;
                case "TAXONOMY":
                    return CommonNames.AssetTypeClass_Model;
                case "DOMAIN":
                    return "Reference";
                case "SYNONYM":
                    return "Grammatic Type";
                default:
                    return key;
            }

        }


        public IEnumerable<TypeaheadResult> GetTypeaheadResults(int companyID, int resourceID, string phrase, int size = 10, string type = "")
        {
            if (string.IsNullOrEmpty(phrase))
                return new List<TypeaheadResult>();

            Nest.Field fldName = new Nest.Field("Name");
            Nest.Field fldGroup = new Nest.Field("d3sGroup");
            Nest.Field fldTag = new Nest.Field("d3sTags.Value");
            List<QueryContainer> mustClauses = new List<QueryContainer>();
            BoolQuery filterQuery = null;

            /* For Typeahead, the search phrase is split into words, all words but the last will be
             * queried using 'match' and the last word will be 'prefix'
             * For searching tags, an asterkiks is appended and a regular 'query_string' query is used 
             */
            Queue<string> parts = new Queue<string>( phrase.ToLower().Split(' '));
            string tagSearch = EscapeSpecialCharacters(phrase.ToLower()) + (!phrase.EndsWith("*") ? "*" :  "");

            while (parts.Count > 1)
            {
                mustClauses.Add(new MatchQuery
                {
                    Field = fldName,
                    Query = EscapeSpecialCharacters(parts.Dequeue())
                });
            }
            mustClauses.Add(new PrefixQuery
            {
                Field = fldName,
                Value = EscapeSpecialCharacters(parts.Dequeue())
            });

            if (!string.IsNullOrEmpty(type))
            {
                string[] types = type.Split(',');
                if (types.Length > 1)
                {
                    filterQuery = new BoolQuery {
                        Must = new QueryContainer[] {
                            new TermsQuery {
                                Field = fldGroup,
                                Terms = types
                            }
                        }
                    };
                }
                else
                {
                    filterQuery = new BoolQuery {
                        Must = new QueryContainer[] {
                            new TermQuery {
                                Field = fldGroup,
                                Value = type
                            }
                        }
                    };
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
                                    Path = "d3sTags",
                                    Query = new BoolQuery{
                                        Must = new QueryContainer[] { new QueryStringQuery {
                                            DefaultField = fldTag,
                                            Query = tagSearch
                                        }}
                                    },
                                    InnerHits = new InnerHits {
                                        Highlight = new Highlight {
                                            Fields = new Dictionary<Nest.Field, IHighlightField> { { fldTag, new HighlightField { } } }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    Filter = new QueryContainer[] { filterQuery }
                },
                Size = size
            };

            var client = new ElasticClient(GetConnectionSettings(companyID));
            //Because the index model is variable, the LowLevel client is used and the request is turned into a JSON string
            string jsonString = client.RequestResponseSerializer.SerializeToString(sReq);
            var response = client.LowLevel.Search<StringResponse>(GetCompanyIndexName(companyID), "_doc", jsonString);
           
            if (!response.Success)
                throw new ApplicationException(response.OriginalException.Message);

            var searchResults = JsonConvert.DeserializeObject<SearchResultsModel>(response.Body);

            return searchResults.hits.hits.Select(h => new TypeaheadResult
            {
                Name = GetPropertyValue<string>(h._source, "Name"),
                DisplayName = GetDisplayName(h),
                Group = MapGroupToFriendlyName(h.d3sGroup),
                Type = GetPropertyValue<string>(h._source, "Type"),
                Url = GetPropertyValue<string>(h._source, "Url"),
                Uid = GetGuidPropertyIfExists(h, "Uid"),
                AssetTypeUid = GetGuidPropertyIfExists(h, "AssetTypeUid"),
                Tags = GetTags(h, true)
            });
        }

        private string GetTypeAheadSynonymDisplayType(SearchResultsHitModel h)
        {
            var type = GetPropertyValue<string>(h._source, "SynonymForObject");
            if ((type ?? string.Empty).ToUpper() == "ARTIFACT")
            {
                return $"{MapGroupToFriendlyName(type)} - {GetPropertyValue<string>(h._source, "SynonymForObjectType")}";
            }
            return MapGroupToFriendlyName(type);
        }

        /// <summary>
        /// Gets the search results from elastic search and converts them to index results
        /// </summary>
        /// <param name="companyID"></param>
        /// <param name="resourceID"></param>
        /// <param name="phrase"></param>
        /// <returns></returns>
        public IndexResults GetSearchResults(int companyID, int resourceID, string phrase, int size, int from, string group = "")
        {
            IndexResults result = new IndexResults();
            CreateIndexIfNotExists(companyID);

            Nest.Field fldType = new Nest.Field("Type");
            Nest.Field fldTag = new Nest.Field("d3sTags.Value");

            phrase = EscapeSpecialCharacters(phrase);

            SearchRequest sReq = new SearchRequest
            {
                Query = new BoolQuery
                {
                    Must = new QueryContainer[] {
                        new BoolQuery{
                            Should = new QueryContainer[] {
                                new QueryStringQuery {
                                    Query = phrase
                                },
                                new NestedQuery {
                                    Path = "d3sTags",
                                    Query = new BoolQuery{
                                        Must = new QueryContainer[] { new QueryStringQuery {
                                            DefaultField = fldTag,
                                            Query = phrase
                                        }}
                                    },
                                    InnerHits = new InnerHits {
                                        Highlight = new Highlight {
                                            Fields = new Dictionary<Nest.Field, IHighlightField> { { fldTag, new HighlightField { } } }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    Filter = new QueryContainer[] {
                        new TermQuery{
                            Field = fldType,
                            Value = group
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

            var client = new ElasticClient(GetConnectionSettings(companyID));
            //Because the index model is variable, the LowLevel client is used and the request is turned into a JSON string
            string jsonString = client.RequestResponseSerializer.SerializeToString(sReq);
            var response = client.LowLevel.Search<StringResponse>(GetCompanyIndexName(companyID), "_doc", jsonString);

            if (!response.Success)
                throw new ApplicationException(response.OriginalException.Message);

            var searchResults = JsonConvert.DeserializeObject<SearchResultsModel>(response.Body);

            result.Results = searchResults.hits.hits.Select(h => new IndexResult
            {
                Name = GetPropertyValue<string>(h._source, "Name"),
                DisplayName = GetDisplayName(h),
                Description = GetPropertyValue<string>(h._source, "Description"),
                Group = MapGroupToFriendlyName(h.d3sGroup),
                ID = h._id,
                NormalizedScore = (searchResults.hits.max_score.GetValueOrDefault() == 0 ? 0 : (h._score / searchResults.hits.max_score.GetValueOrDefault() * 100)),
                Score = h._score,
                Type = GetPropertyValue<string>(h._source, "Type"),
                Url = GetPropertyValue<string>(h._source, "Url"),
                Uid = GetGuidPropertyIfExists(h, "Uid"),
                AssetTypeUid = GetGuidPropertyIfExists(h, "AssetTypeUid"),
                Tags = GetTags(h)
            }).ToList();
            
            result.ElapsedMS = searchResults.took;

            if (searchResults.hits != null)
                result.Matches = searchResults.hits.total;

            return result;
        }

        private string GetDisplayName(SearchResultsHitModel h)
        {
            var synonymFor = GetPropertyValue<string>(h._source, "SynonymFor");
            
            var name = GetPropertyValue<string>(h._source, "Name");

            if (string.IsNullOrEmpty(synonymFor))
            {
                if((h.d3sGroup ?? "").ToUpper() != "ARTIFACT")
                    return name;

                var taxonomy = GetPropertyValue<string>(h._source, "Taxonomy");

                return (string.IsNullOrEmpty(taxonomy) ? $"{name}" : $"{name} ({taxonomy})");                
            }

            var nymType = GetPropertyValue<string>(h._source, "NymType");

            return $"{name} ({nymType ?? ""} For: {GetTypeAheadSynonymDisplayType(h)}: {synonymFor})";
        }
        
        private string GetHighlightedNameValueIfExists(SearchResultsHitModel h)
        {
            var synonymFor = GetPropertyValue<string>(h._source, "SynonymFor");
            var taxonomy = "";

            if ((h.d3sGroup ?? "").ToUpper() == "ARTIFACT")
            {
                taxonomy = GetPropertyValue<string>(h._source, "Taxonomy");

                if (!string.IsNullOrEmpty(taxonomy))
                {
                    taxonomy = $" ({taxonomy})";
                }
            }

            if (!string.IsNullOrEmpty(synonymFor))
            {
                var nymType = GetPropertyValue<string>(h._source, "NymType");

                synonymFor = $" ({nymType ?? ""} For: {GetTypeAheadSynonymDisplayType(h)}: {synonymFor})";
            }

            var highlightVal = GetPropertyValue<string>(h.highlight, "Name");

            if (!string.IsNullOrEmpty(highlightVal)) return highlightVal + (synonymFor ?? "") + (taxonomy ?? "");

            return GetPropertyValue<string>(h._source, "Name") + (synonymFor ?? "") + (taxonomy ?? "");
        }

        private string GetHighlightedPropertyValueIfExists(SearchResultsHitModel h, string propName)
        {
            var highlightVal = GetPropertyValue<string>(h.highlight, propName);

            if (!string.IsNullOrEmpty(highlightVal)) return highlightVal;

            return GetPropertyValue<string>(h._source, propName);
        }

        private Guid? GetGuidPropertyIfExists(SearchResultsHitModel h, string propName)
        {
            Guid result = new Guid();
            Guid.TryParse(GetPropertyValue<string>(h._source, propName), out result);
            if (result == Guid.Empty)
                return null;
            return result;
        }

        private List<IndexTag> GetTags(SearchResultsHitModel h, bool onlyHits = false, bool highlightHits = true)
        {
            List<IndexTag> tags = new List<IndexTag>();
            List<IndexTag> highlights = new List<IndexTag>();

            if (h._source == null || (onlyHits && h.inner_hits == null))
                return tags;

            if(highlightHits && h.inner_hits != null)
                highlights = h.inner_hits.SelectTokens("d3sTags.hits.hits[*]")
                    .Select(t => t.ToObject<SearchTagInnerHitsModel>())
                    .Select(hi => new IndexTag { Uid = hi._source.Uid, Value = hi._source.Value, Highlight = hi.GetHighLightValue() })
                    .ToList();

            if (onlyHits && highlightHits)
            {
                //Since we are only returning hits, no need to also find _source tags
                return highlights.OrderBy(t => t.Value).ToList();
            }

            //Find _source tags from either inner_hits or _source
            if (onlyHits)
                tags = h.inner_hits.SelectTokens("d3sTags.hits.hits[*]._source").Select(t => t.ToObject<IndexTag>()).ToList();
            else
                tags = h._source.SelectTokens("d3sTags[*]").Select(t => t.ToObject<IndexTag>()).ToList();

            if (highlightHits)
                tags = highlights.Union(tags).ToList();

            return tags.OrderBy(t => t.Value).ToList();
        }

        private T GetPropertyValue<T>(JObject _source, string propName)
        {
            if (_source != null && _source.TryGetValue(propName, out JToken jToken))
            {
                if (jToken.Type == JTokenType.Array)
                {
                    return ((JArray)jToken)[0].Value<T>();
                }
                return jToken.Value<T>();
            }
            return default(T);
        }

        public void ReIndex(int companyID, IEnumerable<AddToIndexModel> items)
        {
            ClearIndex(companyID);
            AddToIndex(items);
        }

        public void RemoveFromIndex(RemoveFromIndexModel item)
        {
            if (item == null) return;

            CreateIndexIfNotExists(item.CompanyID);

            var client = new ElasticLowLevelClient(GetConnectionSettings(item.CompanyID));
            var response = client.Delete<StringResponse>(GetCompanyIndexName(item.CompanyID), "_doc" , CreateItemID(item));

            if(!response.Success)
                throw new ApplicationException(response.OriginalException.Message);
        }

        public void RemoveFromIndex(IEnumerable<RemoveFromIndexModel> items)
        {
            var firstItem = items.FirstOrDefault();

            if (firstItem == null) return;

            var companyId = firstItem.CompanyID;

            CreateIndexIfNotExists(companyId);

            StringBuilder sb = new StringBuilder();

            foreach (var item in items)
            {
                sb.Append("{ \"delete\" : { \"_type\" : \"_doc\", \"_id\" : \"" + CreateItemID(item) + "\"}}\n");
            }

            var client = new ElasticLowLevelClient(GetConnectionSettings(companyId));
            var bulkResponse = client.Bulk<StringResponse>(GetCompanyIndexName(companyId), sb.ToString());

            if (!bulkResponse.Success)
                throw new ApplicationException(bulkResponse.OriginalException.Message);

            var result = JObject.Parse(bulkResponse.Body);

            if (result == null) throw new Exception("Invalid response no data");

            var hasErrors = result.GetValue("errors");

            if (hasErrors.Value<bool>())
                throw new Exception(bulkResponse.Body);
        }

        public void UpdateInIndex(UpdateInIndexModel item)
        {
            if (item == null) return;

            CreateIndexIfNotExists(item.CompanyID);

            var client = new ElasticLowLevelClient(GetConnectionSettings(item.CompanyID));
            var response = client.Update<StringResponse>(GetCompanyIndexName(item.CompanyID), "_doc", CreateItemID(item), CreateDocument(item).ToString());

            if(!response.Success)
                throw new ApplicationException(response.OriginalException.Message);
        }

        public void UpdateInIndex(IEnumerable<UpdateInIndexModel> items)
        {
            var firstItem = items.FirstOrDefault();

            if (firstItem == null) return;

            var companyId = firstItem.CompanyID;

            CreateIndexIfNotExists(companyId);

            StringBuilder sb = new StringBuilder();

            foreach (var item in items)
            {

                sb.Append("{ \"update\" : { \"_type\" : \"_doc\", \"_id\" : \"" + CreateItemID(item) + "\"}}\n");

                sb.Append("{ \"doc\" : {\"Url\" : \"" + item.RelativeUrl + "\"");
                
                if (item.Fields.Any())
                    sb.Append(",");

                bool bFirst = true;
                foreach (var f in item.Fields)
                {
                    if (string.IsNullOrEmpty(f.Value))
                        continue;

                    if (!bFirst)
                        sb.Append(", ");
                    else
                        bFirst = false;

                    sb.Append(" \"" + f.Key + "\" : \"" + EscapeValueForDoc(f.Value) + "\" ");
                }

                //This is an update, so if there are no tags, we need to be explicit, so they will be removed (if any) on the document
                sb.Append(", \"d3sTags\":[");
                if (item.Tags != null && item.Tags.Any())
                {
                    string[] tags = item.Tags.Select(t => "{ \"Uid\": \"" + t.Key + "\", \"Value\": \"" + EscapeValueForDoc(t.Value) + "\"}").ToArray();
                    sb.Append(string.Join(",", tags));
                }
                sb.Append("]");

                sb.Append(" } }\n");
            }

            var client = new ElasticLowLevelClient(GetConnectionSettings(companyId));
            var bulkResponse = client.Bulk<StringResponse>(GetCompanyIndexName(companyId), sb.ToString());

            if (!bulkResponse.Success)
            {
                StringBuilder exMessage = new StringBuilder();
                exMessage.AppendLine(bulkResponse.OriginalException.Message);
                exMessage.Append("ES_DebugInformation: ");
                exMessage.AppendLine(bulkResponse.DebugInformation);

                throw new ApplicationException(exMessage.ToString()); ;
            }

            var result = JObject.Parse(bulkResponse.Body);

            if (result == null) throw new Exception("Invalid response no data");

            var hasErrors = result.GetValue("errors");

            if (hasErrors.Value<bool>())
                throw new Exception(bulkResponse.Body);            
        }
    }
}
