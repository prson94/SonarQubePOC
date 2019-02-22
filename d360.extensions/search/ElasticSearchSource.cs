using d360.core;
using d360.core.entities;
using d360.core.queue;
using Dapper;
using MoreLinq;
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
        public string _id { get; set; }
        public float _score { get; set; }
        public JObject _source { get; set; }
        public JObject highlight { get; set; }
    }


    public class ElasticSearchSource : ISearchSource
    {
        private const string DEFAULT_SEARCH_SERVER = "search1-d3s.cloudapp.net:9200";
        private const int BULK_BATCH_SIZE = 5000;


        protected string SearchServerUrl { get; set; }

        #region Utility methods

        JObject createDocument(IndexObjectModel item)
        {
            var doc = new JObject();            
            doc.Add("Url", item.RelativeUrl);
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

        string getCompanyIndexName(int companyID)
        {
            return $"d3s{companyID}";
        }

        string createItemID(IndexObjectModel item)
        {
            return item.getObjectID();            
        }

        void loadSearchServerUrl(int companyID)
        {
            using (var community = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
            {
                var db = community.Query<DatabaseServer>(@"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id", new { id = companyID }).SingleOrDefault();

                SearchServerUrl = db.SearchServer ?? DEFAULT_SEARCH_SERVER;
            }

            if (string.IsNullOrEmpty(SearchServerUrl)) throw new Exception("DEV ERROR - NO SEARCH BASE URL SPECIFIED.");
        }
        
        
        HttpWebRequest createWebRequest(string method, string uri, int companyID)
        {
            if(string.IsNullOrEmpty(SearchServerUrl))
            {
                loadSearchServerUrl(companyID);
            }
            
            var webReq = HttpWebRequest.CreateHttp($"http://{SearchServerUrl}/{uri}");
            webReq.ContentType = "application/json; charset=UTF-8";
            webReq.Accept = "application/json";
            webReq.Method = method;
            return webReq;
        }

        void loadMessageInRequestBody(HttpWebRequest webReq, JObject obj)
        {
            var json = obj.ToString(Newtonsoft.Json.Formatting.None);
            byte[] schemaData;
            var encoding = new System.Text.UTF8Encoding();
            schemaData = encoding.GetBytes(json);
            webReq.ContentLength = schemaData.Length;
            var schemaStream = webReq.GetRequestStream();
            schemaStream.Write(schemaData, 0, schemaData.Length);
        }

        void loadMessageInRequestBody(HttpWebRequest webReq, string obj)
        {
            byte[] schemaData;
            var encoding = new System.Text.UTF8Encoding();
            schemaData = encoding.GetBytes(obj);
            webReq.ContentLength = schemaData.Length;
            var schemaStream = webReq.GetRequestStream();
            schemaStream.Write(schemaData, 0, schemaData.Length);
        }

        JsonResponseModel getJsonResponse(HttpWebRequest webReq, bool parseResult = true)
        {
            var model = new JsonResponseModel();
            var wr = "";
            try {
                using (var resp = (HttpWebResponse)webReq.GetResponse())
                {
                    model.Status = resp.StatusCode;
                    model.StatusMessage = resp.StatusDescription;

                    if (parseResult)
                    {
                        using (var responseStream = resp.GetResponseStream())
                        {
                            using (var rdr = new System.IO.StreamReader(responseStream))
                            {
                                wr = rdr.ReadToEnd();
                            }
                        }

                        if (!string.IsNullOrEmpty(wr))
                            model.Data = JObject.Parse(wr);
                    }
                }
            }
            catch(WebException we)
            {
                var resp = we.Response as HttpWebResponse;
                if (resp == null)
                    throw;
                model.Status = resp.StatusCode;
                model.StatusMessage = resp.StatusDescription;
            }
            return model;
        }

        /// <summary>
        /// Create an index if id doesnt exist
        /// </summary>
        /// <param name="companyID"></param>
        void createIndexIfNotExists(int companyID)
        {
            var indexName = $"{getCompanyIndexName(companyID)}";
            var webReq = createWebRequest("HEAD", indexName, companyID);
            var response = getJsonResponse(webReq);
            if (response.Status == HttpStatusCode.NotFound)
            {
                webReq = createWebRequest("PUT", indexName, companyID);
                
                loadMessageInRequestBody(webReq, JObject.Parse("{\"settings\": { \"index\": { \"number_of_shards\": 2, \"number_of_replicas\": 1 }},\"mappings\": {\"_default_\" : {\"properties\" : {\"Type\" : {\"type\" : \"string\",\"fields\" : {\"raw\" : {\"type\" : \"string\", \"index\" :\"not_analyzed\"}}}}}}}"));

                    response = getJsonResponse(webReq);
                if (response.Status != HttpStatusCode.OK)
                    throw new ApplicationException(response.StatusMessage);
            }
        }

        /// <summary>
        /// Delete an index if it exists
        /// </summary>
        /// <param name="companyID"></param>
        private void deleteIndexIfExists(int companyID)
        {
            var indexName = $"{getCompanyIndexName(companyID)}";
            var webReq = createWebRequest("HEAD", indexName, companyID);
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.NotFound)
            {
                webReq = createWebRequest("DELETE", indexName, companyID);                
                response = getJsonResponse(webReq);
                if (response.Status != HttpStatusCode.OK)
                    throw new ApplicationException(response.StatusMessage);
            }
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
            
            createIndexIfNotExists(companyId);
            
            foreach (var batch in items.Batch(BULK_BATCH_SIZE))
            {                
                var sb = new StringBuilder();

                var indexName = getCompanyIndexName(companyId);
                foreach (var item in batch)
                {
                    sb.Append("{\"index\":{\"_id\":\"");                                        
                    sb.Append(item.getObjectID());
                    sb.Append("\",\"_type\":\"");
                    sb.Append(item.Group);
                    sb.Append("\" } }\n");
                    sb.Append("{\"Url\" : \"");
                    sb.Append(item.RelativeUrl);
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

                        var val = f.Value.Replace("\r", "").Replace("\n", "").Replace("\v","").Replace("\t", "").Replace("\\", "\\\\").Replace("\"", "\\\"");
                        val = HtmlUtilities.RemoveTags(val);

                        sb.Append("\"");
                        sb.Append(f.Key);
                        sb.Append("\":\"");
                        sb.Append(val);
                        sb.Append("\"");
                    }
                    sb.Append("}\n");
                }
                sb.Append("\n");
                
                var webReq = createWebRequest("POST", $"{getCompanyIndexName(companyId)}/_bulk", companyId);
                webReq.AllowWriteStreamBuffering = false;
                loadMessageInRequestBody(webReq, sb.ToString());
                var response = getJsonResponse(webReq);
                if (response.Status != HttpStatusCode.OK)
                    throw new ApplicationException(response.StatusMessage);

                var result = response.Data;

                if (result == null) throw new Exception("Invalid response no data");

                var hasErrors = result.GetValue("errors");

                if (hasErrors.Value<bool>())
                    throw new Exception(response.Data.ToString());                
            }     
        }

        public void ClearIndex(int companyID)
        {
            deleteIndexIfExists(companyID);

            createIndexIfNotExists(companyID);            
        }

     
        public void ClearIndex(int companyID, string group)
        {
            createIndexIfNotExists(companyID);

            var webReq = createWebRequest("DELETE", $"{getCompanyIndexName(companyID)}/{group}/_query", companyID);
            loadMessageInRequestBody(webReq, JObject.Parse("{\"query\": { \"match_all\": {} }}"));

            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.OK)
                throw new ApplicationException(response.StatusMessage);
        }
      
        public IEnumerable<string> GetSearchPhrases(int companyID, string term, int maxResults)
        {
            createIndexIfNotExists(companyID);
            return null;
        }

        private bool isElasticSearchSpecialChar(char ch)
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

            if (phrase.Any( ch => isElasticSearchSpecialChar(ch)))
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

            var searchType = type != null ? type + "/" : null;
            
            var webReq = createWebRequest("POST", $"{getCompanyIndexName(companyID)}/{searchType}_search", companyID);

            StringBuilder sb = new StringBuilder();
            
            if(!string.IsNullOrEmpty(phrase))
            {
                phrase = EscapeSpecialCharacters(phrase);

                //search.service indicates "Exact match" by wrapping phrase in single quotes
                if (phrase.StartsWith("'") && phrase.EndsWith("'"))
                {
                    phrase = phrase.Trim('\'');
                    sb.Append("{\"query\":{\"filtered\": {\"query\":  { \"match_phrase\": { \"Name\":\"" + phrase + "\"} }");
                }
                else
                {
                    //Not exact match, so append *
                    if (!phrase.EndsWith("*"))
                    {
                        phrase += "*";
                    }
                    sb.Append("{\"query\":{\"filtered\": {\"query\":  { \"query_string\": { \"query\":\"" + phrase + "\"} }");
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

                    compositeSearchTerm += $"{item.field}:{searchTerm}";
                }

                sb.Append("{\"query\":{\"filtered\": {\"query\":  { \"query_string\": { \"query\":\"" + compositeSearchTerm + "\"} }");
            }                

            //if a group was specified filter by it
            if (!string.IsNullOrEmpty(group))
            {
                sb.Append(",\"filter\": { \"term\":  { \"Type.raw\": \"" + group + "\" } }");                
            }
                        
            sb.Append("}},\"from\":" + from + ",\"size\":" + size );

            // if no group filter then we need to get list of categories
            if (string.IsNullOrEmpty(group))
            {                
                sb.Append(",\"aggs\" : { \"all_types\": {\"terms\": {\"field\": \"_type\"},\"aggs\": {\"category\": {\"terms\": {\"field\": \"Type.raw\",\"size\": 0}}}}}");
            }

            //turn on highlighting

            sb.Append(", \"highlight\": {\"fields\": {\"Name\": { \"pre_tags\": [\"<em class='search-highlight'>\"],\"post_tags\": [\"</em>\"],\"number_of_fragments\" : 0 }},\"require_field_match\": false  }");
            
            sb.Append("}");

            //if no group specified get the categories for this search

            loadMessageInRequestBody(webReq, sb.ToString());
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.OK)
                throw new ApplicationException(response.StatusMessage);

            var searchResults = response.Data.ToObject<SearchResultsModel>();

            result.Results = searchResults.hits.hits.Select(h => new IndexResult {
                    Description = GetHighlightedPropertyValueIfExists(h, "Description"),
                    Group = h._type,
                    ID = h._id,
                    Name = GetHighlightedNameValueIfExists(h),
                    NormalizedScore = (searchResults.hits.max_score.GetValueOrDefault() == 0 ? 0 : (h._score/searchResults.hits.max_score.GetValueOrDefault()*100)),
                    Score = h._score,
                    Type = GetHighlightedPropertyValueIfExists(h, "Type"),
                    Url = GetHighlightedPropertyValueIfExists(h, "Url")
                }).ToList();


            if (searchResults.aggregations != null && searchResults.aggregations.all_types != null && searchResults.aggregations.all_types.buckets != null)
            {
                categories.AddRange(searchResults.aggregations.all_types.buckets.Select(h => new IndexTypeList
                {
                    Name = h.key,
                    DisplayName = mapTypeToFriendlyName(h.key),
                    ResultCount = h.doc_count,
                    Categories = h.category != null ? h.category.buckets.Select(c => new IndexCategory
                    {
                        Name = c.key,
                        ResultCount = c.doc_count
                    }).OrderBy(x =>x.Name).ToList() : null
                }).OrderBy(x =>x.DisplayName));
            }
            
            result.ElapsedMS = searchResults.took;

            if(searchResults.hits != null)
                result.Matches = searchResults.hits.total;

            return result;
        }

        private string mapTypeToFriendlyName(string key)
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
            var searchType = type != null ? type + "/" : null;

            var webReq = createWebRequest("POST", $"{getCompanyIndexName(companyID)}/{searchType}_search", companyID);
            

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

                sb.Append("]}}, \"size\":" + size + "}");
            }
            

            loadMessageInRequestBody(webReq, sb.ToString());
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.OK)
                throw new ApplicationException(response.StatusMessage);

           var searchResults = response.Data.ToObject<SearchResultsModel>();

            return searchResults.hits.hits.Select(h => new TypeaheadResult
            {
                Name = GetPropertyValue<string>(h._source, "Name"),
                DisplayName = GetDisplayName(h),//GetHighlightedPropertyValueIfExists(h, "Name"),
                Desc = GetHighlightedPropertyValueIfExists(h, "Description"),
                Type = GetTypeAheadDisplayType(h),//mapTypeToFriendlyName(h._type),
                Url = GetPropertyValue<string>(h._source, "Url"),
            });
        }

        private string GetTypeAheadDisplayType(SearchResultsHitModel h)
        {
            if((h._type ?? string.Empty).ToUpper() == "ARTIFACT")
            {
                return $"{mapTypeToFriendlyName(h._type)} - {GetPropertyValue<string>(h._source, "Type")}";
            }
            return mapTypeToFriendlyName(h._type);
        }

        private string GetTypeAheadSynonymDisplayType(SearchResultsHitModel h)
        {
            var type = GetPropertyValue<string>(h._source, "SynonymForObject");
            if ((type ?? string.Empty).ToUpper() == "ARTIFACT")
            {
                return $"{mapTypeToFriendlyName(type)} - {GetPropertyValue<string>(h._source, "SynonymForObjectType")}";
            }
            return mapTypeToFriendlyName(type);
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

            createIndexIfNotExists(companyID);

            var webReq = createWebRequest("POST", $"{getCompanyIndexName(companyID)}/_search", companyID);

            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(phrase))
                phrase = phrase.Replace("\"", "\\\"");

            sb.Append("{\"query\":{\"filtered\": {\"query\":  { \"query_string\": { \"query\":\"" + phrase + "\"} }");

            //if a group was specified filter by it
            if (!string.IsNullOrEmpty(group))
            {
                sb.Append(",\"filter\": { \"term\":  { \"Type.raw\": \"" + group + "\" } }");
            }

            sb.Append("}},\"from\":" + from + ",\"size\":" + size + ",\"sort\":{ \"_score\":{ \"order\":\"desc\"} }");
                        
            sb.Append("}");

            //if no group specified get the categories for this search

            loadMessageInRequestBody(webReq, sb.ToString());
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.OK)
                throw new ApplicationException(response.StatusMessage);

            var searchResults = response.Data.ToObject<SearchResultsModel>();

            result.Results = searchResults.hits.hits.Select(h => new IndexResult
            {
                Description = GetPropertyValue<string>(h._source, "Description"),
                Group = h._type,
                ID = h._id,
                Name = GetPropertyValue<string>(h._source, "Name"),
                NormalizedScore = (searchResults.hits.max_score.GetValueOrDefault() == 0 ? 0 : (h._score / searchResults.hits.max_score.GetValueOrDefault() * 100)),
                Score = h._score,
                Type = GetPropertyValue<string>(h._source, "Type"),
                Url = GetPropertyValue<string>(h._source, "Url")
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
                if((h._type ?? "").ToUpper() != "ARTIFACT")
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

            if ((h._type ?? "").ToUpper() == "ARTIFACT")
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


        private T GetPropertyValue<T>(JObject _source, string propName)
        {
            JToken jToken = null;
            
            if (_source != null && _source.TryGetValue(propName, out jToken))
            {
                if(jToken.Type == JTokenType.Array)
                {
                    return ((JArray)jToken)[0].Value<T>();
                }
                return jToken.Value<T>();
            }
            return default(T);
        }

        public void ReIndex(int companyID, List<AddToIndexModel> items)
        {
            ClearIndex(companyID);
            AddToIndex(items);
        }

        public void RemoveFromIndex(RemoveFromIndexModel item)
        {
            if (item == null) return;

            createIndexIfNotExists(item.CompanyID);

            var webReq = createWebRequest("DELETE", $"{getCompanyIndexName(item.CompanyID)}/{item.Group}/{createItemID(item)}", item.CompanyID);            
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.OK)
                throw new ApplicationException(response.StatusMessage);
        }

        public void RemoveFromIndex(List<RemoveFromIndexModel> items)
        {
            if (items == null || items.Count < 0) return;

            var companyID = items[0].CompanyID;

            createIndexIfNotExists(companyID);

            StringBuilder sb = new StringBuilder();

            foreach (var item in items)
            {
                sb.Append("{ \"delete\" : { \"_type\" : \"" + item.Group + "\", \"_id\" : \"" + createItemID(item) + "\"}}\n");
            }
                        
            var webReq = createWebRequest("POST", $"{getCompanyIndexName(items[0].CompanyID)}/_bulk", companyID);
            loadMessageInRequestBody(webReq, sb.ToString());
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.OK)
                throw new ApplicationException(response.StatusMessage);

            var result = response.Data;

            if (result == null) throw new Exception("Invalid response no data");

            var hasErrors = result.GetValue("errors");

            if (hasErrors.Value<bool>())
                throw new Exception(response.Data.ToString());
        }

        public void UpdateInIndex(UpdateInIndexModel item)
        {
            if (item == null) return;

            createIndexIfNotExists(item.CompanyID);

            var webReq = createWebRequest("PUT", $"{getCompanyIndexName(item.CompanyID)}/{item.Group}/{createItemID(item)}", item.CompanyID);
            loadMessageInRequestBody(webReq, createDocument(item));
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.OK)
                throw new ApplicationException(response.StatusMessage);
        }

        public void UpdateInIndex(List<UpdateInIndexModel> items)
        {
            if (items == null || items.Count < 0) return;

            var companyID = items[0].CompanyID;

            createIndexIfNotExists(companyID);

            StringBuilder sb = new StringBuilder();

            foreach (var item in items)
            {
                sb.Append("{ \"update\" : { \"_type\" : \"" + item.Group + "\", \"_id\" : \"" + createItemID(item) + "\"}}\n");

                sb.Append("{ \"doc\" : {\"Url\" : \"" + item.RelativeUrl + "\"");
                
                if (item.Fields.Any())
                    sb.Append(",");

                bool bFirst = true;
                foreach (var f in item.Fields)
                {
                    if (!bFirst)
                        sb.Append(", ");
                    else
                        bFirst = false;

                    if (f.Value == null) continue;
                    sb.Append(" \"" + f.Key + "\" : \"" + f.Value.Replace("\r", "").Replace("\n", "").Replace("\t", "") + "\" ");
                }
                sb.Append(" } }\n");
            }

            var webReq = createWebRequest("POST", $"{getCompanyIndexName(items[0].CompanyID)}/_bulk", companyID);
            loadMessageInRequestBody(webReq, sb.ToString());
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.OK)
                throw new ApplicationException(response.StatusMessage);

            var result = response.Data;

            if (result == null) throw new Exception("Invalid response no data");

            var hasErrors = result.GetValue("errors");

            if (hasErrors.Value<bool>())
                throw new Exception(response.Data.ToString());            
        }
    }
}
