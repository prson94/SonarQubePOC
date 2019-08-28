using d360.core;
using d360.core.entities;
using d360.core.queue;
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
    }


    public class ElasticSearchSource : ISearchSource
    {
        private const string DEFAULT_SEARCH_SERVER = "search1-d3s.cloudapp.net:9200";
        private const int BULK_BATCH_SIZE = 5000;

        private const string MAPPING_VERSION_5 = "{\"settings\": { \"index\": { \"number_of_shards\": 2, \"number_of_replicas\": 1 }},\"mappings\": {\"_doc\" : {\"properties\" : {\"d3sGroup\" : {\"type\" : \"keyword\" }, \"Type\" : {\"type\" : \"keyword\" }}}}}";

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
            input.Replace("\r", "").Replace("\n", "").Replace("\v", "").Replace("\t", "").Replace("\\", "\\\\").Replace("\"", "\\\"");
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
                
                phrase = phrase.Replace(":", "\\\\:"); // escape colon

                phrase = phrase.Replace("^", "\\\\^"); // escape carat

                phrase = phrase.Replace("~", "\\\\~"); // escape carat

                phrase = phrase.Replace("!", "\\\\!"); //  escape exclamation

                phrase = phrase.Replace("[", "\\\\["); //  escape square bracket

                phrase = phrase.Replace("]", "\\\\]"); //  escape square bracket

                phrase = phrase.Replace("{", "\\\\{"); //  escape curyly bracket

                phrase = phrase.Replace("}", "\\\\}"); //  escape curyly bracket

                phrase = phrase.Replace("-", "\\\\-"); // escape hyphen

                phrase = phrase.Replace("/", "\\\\/"); // replace / with escaped slash                

                phrase = phrase.Replace("(", "\\\\("); // replace / with escaped slash       

                phrase = phrase.Replace(")", "\\\\)"); // replace / with escaped slash   
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

            string searchType = type ?? null;
            List<string> searchFilters = new List<string>();
            searchType = "_doc";
            if(!string.IsNullOrEmpty(type))
            {
                string[] types = type.Split(',');
                if (types.Length > 1)
                {
                    searchFilters.Add(" { \"terms\":  { \"d3sGroup\": [\"" + String.Join("\",\"",types) + "\"] } }");
                }
                else
                {
                    searchFilters.Add(" { \"term\":  { \"d3sGroup\": \"" + type + "\" } }");
                }
            }

            StringBuilder sb = new StringBuilder();
            
            if(!string.IsNullOrEmpty(phrase))
            {
                phrase = EscapeSpecialCharacters(phrase);

                //search.service indicates "Exact match" by wrapping phrase in single quotes
                if (phrase.StartsWith("'") && phrase.EndsWith("'"))
                {
                    phrase = phrase.Trim('\'');
                    sb.Append("{\"query\":{\"bool\": {\"must\":  { \"match_phrase\": { \"Name\":\"" + phrase + "\"} }");
                }
                else
                {
                    if (!phrase.EndsWith("*"))
                    {
                        phrase += "*";
                    }
                    sb.Append("{\"query\":{\"bool\": {\"must\":  { \"query_string\": { \"query\":\"" + phrase + "\"} }");
                }
            }
            else if(!string.IsNullOrEmpty(advancedFilterJSON))
            {
                var advFilters = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AdvancedSearchParameters>>(advancedFilterJSON);
                //deserialize advanced search parameters

                var compositeSearchTerm = string.Empty;
                SearchConnector con = SearchConnector.And;

                foreach (var item in advFilters)
                {
                    if (!string.IsNullOrEmpty(compositeSearchTerm)) compositeSearchTerm += $" {con.ToString().ToUpper()} ";

                    if (string.IsNullOrEmpty(item.value)) continue;

                    con = item.connector;

                    var searchTerm = phrase = EscapeSpecialCharacters(item.value);

                    if (item.exact)
                    {
                        searchTerm = searchTerm.Replace("\\\"","");

                        searchTerm = "\\\"" + searchTerm + "\\\"";
                    }
                    else if(!searchTerm.EndsWith("*"))
                    {
                        //Not exact, so append * to searchTerm if it does not already end with *
                        searchTerm += "*";
                    }
                    var field = item.field;
                    if (field == "_type")
                        field = "d3sGroup";
                    compositeSearchTerm += $"{field}:{searchTerm}";
                }

                sb.Append("{\"query\":{\"bool\": {\"must\":  { \"query_string\": { \"query\":\"" + compositeSearchTerm + "\"} }");
            }                

            //if a group was specified filter by it
            if (!string.IsNullOrEmpty(group))
            {
                searchFilters.Add("{ \"term\":  { \"Type\": \"" + group + "\" } }");
            }

            if(searchFilters.Count > 0)
            {
                sb.Append(",\"filter\":  { \"bool\": { \"must\": [ ");
                sb.Append(string.Join(",",searchFilters));
                sb.Append("]}}");
            }

            sb.Append("}},\"from\":" + from + ",\"size\":" + size );

            // if no group filter then we need to get list of categories
            if (string.IsNullOrEmpty(group))
            {
                //size=0 intepreted as integer.MAX_VALUE deprecated in ES 2.4.0.
                //Using 2000 for EX6 for now. @TODO: Consider using Composite aggreation
                int bucketSize = 2000;
                sb.Append(",\"aggs\" : { \"all_types\": {\"terms\": {\"field\": \"d3sGroup\"},\"aggs\": {\"category\": {\"terms\": {\"field\": \"Type\",\"size\": " + bucketSize+"}}}}}");
            }

            //turn on highlighting

            sb.Append(", \"highlight\": {\"fields\": {\"Name\": { \"pre_tags\": [\"<em class='search-highlight'>\"],\"post_tags\": [\"</em>\"],\"number_of_fragments\" : 0 }},\"require_field_match\": false  }");
            
            sb.Append("}");

            //if no group specified get the categories for this search

            var client = new ElasticLowLevelClient(GetConnectionSettings(companyID));
            var response = client.Search<StringResponse>(GetCompanyIndexName(companyID), searchType, sb.ToString());

            if (!response.Success)
                throw new ApplicationException(response.OriginalException.Message);

            var searchResults = JsonConvert.DeserializeObject<SearchResultsModel>(response.Body);

            result.Results = searchResults.hits.hits.Select(h => new IndexResult {
                Description = GetHighlightedPropertyValueIfExists(h, "Description"),
                Group = h.d3sGroup,
                ID = h._id,
                Name = GetHighlightedNameValueIfExists(h),
                NormalizedScore = (searchResults.hits.max_score.GetValueOrDefault() == 0 ? 0 : (h._score/searchResults.hits.max_score.GetValueOrDefault()*100)),
                Score = h._score,
                Type = GetHighlightedPropertyValueIfExists(h, "Type"),
                Url = GetHighlightedPropertyValueIfExists(h, "Url"),
                Uid = GetUid(h, "Uid"),
                Icon = GetIcon(h)
            }).ToList();


            if (searchResults.aggregations != null && searchResults.aggregations.all_types != null && searchResults.aggregations.all_types.buckets != null)
            {
                categories.AddRange(searchResults.aggregations.all_types.buckets.Select(h => new IndexTypeList
                {
                    Name = h.key,
                    DisplayName = MapTypeToFriendlyName(h.key),
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

        private string GetIcon(SearchResultsHitModel hit)
        {
            return "fa-search";
        }

        private string MapTypeToFriendlyName(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            var temp = key.Trim().ToUpper();

            switch (temp)
            {
                case "FUSIONATTRIBUTES":
                    return "Fusion";
                case "ARTIFACT":
                    return "Glossary";
                case "TAXONOMY":
                    return "Model";
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
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(phrase))
            {
                phrase = phrase.Replace("\"", "");

                phrase = EscapeSpecialCharacters(phrase).ToLower();
                                
                //split on spaces
                var parts = phrase.Split(' ');
                sb.Append("{\"query\": {\"bool\": {\"must\": [ ");
                int indx = 0;

                foreach (var word in parts)
                {
                    var matchType = (indx == (parts.Length - 1)) ? "prefix" : "match";

                    if (indx > 0) sb.Append(',');
                    sb.Append("{\""+ matchType + "\": {\"Name\": \"" + word + "\"}}");

                    indx++;
                }
                sb.Append("]");

                if (!string.IsNullOrEmpty(type))
                {
                    sb.Append(",\"filter\":  { \"bool\": { \"must\": [ ");
                    string[] types = type.Split(',');
                    if (types.Length > 1)
                    {
                        sb.Append(" { \"terms\":  { \"d3sGroup\": [\"" + String.Join("\",\"", types) + "\"] } }");
                    }
                    else
                    {
                        sb.Append(" { \"term\":  { \"d3sGroup\": \"" + type + "\" } }");
                    }
                    sb.Append("]}}");
                }

                sb.Append("}}, \"size\":" + size + "}");
            }

            var client = new ElasticLowLevelClient(GetConnectionSettings(companyID));
            var response = client.Search<StringResponse>(GetCompanyIndexName(companyID), "_doc", sb.ToString());

            if (!response.Success)
                throw new ApplicationException(response.OriginalException.Message);

            var searchResults = JsonConvert.DeserializeObject<SearchResultsModel>(response.Body);

            return searchResults.hits.hits.Select(h => new TypeaheadResult
            {
                Name = GetPropertyValue<string>(h._source, "Name"),
                DisplayName = GetDisplayName(h),//GetHighlightedPropertyValueIfExists(h, "Name"),
                Desc = GetHighlightedPropertyValueIfExists(h, "Description"),
                Type = GetTypeAheadDisplayType(h),//mapTypeToFriendlyName(h._type),
                Url = GetPropertyValue<string>(h._source, "Url"),
                Uid = GetUid(h, "Uid"),
                Icon = GetIcon(h)
            });
        }

        private string GetTypeAheadDisplayType(SearchResultsHitModel h)
        {
            if ((h.d3sGroup ?? string.Empty).ToUpper() == "ARTIFACT")
            {
                return $"{MapTypeToFriendlyName(h.d3sGroup)} - {GetPropertyValue<string>(h._source, "Type")}";
            }
            return MapTypeToFriendlyName(h.d3sGroup);
        }

        private string GetTypeAheadSynonymDisplayType(SearchResultsHitModel h)
        {
            var type = GetPropertyValue<string>(h._source, "SynonymForObject");
            if ((type ?? string.Empty).ToUpper() == "ARTIFACT")
            {
                return $"{MapTypeToFriendlyName(type)} - {GetPropertyValue<string>(h._source, "SynonymForObjectType")}";
            }
            return MapTypeToFriendlyName(type);
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

            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(phrase))
                phrase = phrase.Replace("\"", "\\\"");

            sb.Append("{\"query\":{\"bool\": {\"must\":  { \"query_string\": { \"query\":\"" + phrase + "\"} }");

            //if a group was specified filter by it
            if (!string.IsNullOrEmpty(group))
            {
                sb.Append(",\"filter\": { \"term\":  { \"Type\": \"" + group + "\" } }");
            }

            sb.Append("}},\"from\":" + from + ",\"size\":" + size + ",\"sort\":{ \"_score\":{ \"order\":\"desc\"} }");
                        
            sb.Append("}");

            //if no group specified get the categories for this search

            var client = new ElasticLowLevelClient(GetConnectionSettings(companyID));
            var response = client.Search<StringResponse>(GetCompanyIndexName(companyID), sb.ToString());

            if (!response.Success)
                throw new ApplicationException(response.OriginalException.Message);

            var searchResults = JsonConvert.DeserializeObject<SearchResultsModel>(response.Body);

            result.Results = searchResults.hits.hits.Select(h => new IndexResult
            {
                Description = GetPropertyValue<string>(h._source, "Description"),
                Group = h.d3sGroup,
                ID = h._id,
                Name = GetPropertyValue<string>(h._source, "Name"),
                NormalizedScore = (searchResults.hits.max_score.GetValueOrDefault() == 0 ? 0 : (h._score / searchResults.hits.max_score.GetValueOrDefault() * 100)),
                Score = h._score,
                Type = GetPropertyValue<string>(h._source, "Type"),
                Url = GetPropertyValue<string>(h._source, "Url"),
                Uid = GetUid(h, "Uid"),
                Icon = GetIcon(h)
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

        private Guid? GetUid(SearchResultsHitModel h, string propName)
        {
            Guid result = new Guid();
            Guid.TryParse(GetPropertyValue<string>(h._source, "Uid"), out result);
            if (result == Guid.Empty)
                return null;
            return result;
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
