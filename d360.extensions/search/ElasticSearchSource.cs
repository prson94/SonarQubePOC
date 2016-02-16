using System.Collections.Generic;
using d360.core.queue;
using Newtonsoft.Json.Linq;
using System.Net;
using System;
using Newtonsoft.Json;
using System.Linq;
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
        public SearchAggregationTypeModel types { get; set; }
    }

    public class SearchAggregationTypeModel
    {
        public List<SearchAggregationTypeBucketModel> buckets { get; set; }
    }

    public class SearchAggregationTypeBucketModel
    {
        public int doc_count { get; set; }
        public string Key { get; set; }
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
        #region Utility methods

        JObject createDocument(IndexObjectModel item)
        {
            var doc = new JObject();
            //doc.Add("Type", item.Type);
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
            return $"{item.Group}|{item.ID}";
        }

        HttpWebRequest createWebRequest(string method, string uri)
        {
            var webReq = HttpWebRequest.CreateHttp($"http://search1-d3s.cloudapp.net:9200/{uri}");
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

        JsonResponseModel getJsonResponse(HttpWebRequest webReq)
        {
            var model = new JsonResponseModel();
            var wr = "";
            try {
                using (var resp = (HttpWebResponse)webReq.GetResponse())
                {
                    model.Status = resp.StatusCode;
                    model.StatusMessage = resp.StatusDescription;

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
            var webReq = createWebRequest("HEAD", indexName);
            var response = getJsonResponse(webReq);
            if (response.Status == HttpStatusCode.NotFound)
            {
                webReq = createWebRequest("PUT", indexName);
                
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
            var webReq = createWebRequest("HEAD", indexName);
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.NotFound)
            {
                webReq = createWebRequest("DELETE", indexName);                
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

        public void AddToIndex(List<AddToIndexModel> items)
        {            
            if (!items.Any()) return;

            var companyId = items.First().CompanyID;

            createIndexIfNotExists(companyId);
            var sb = new StringBuilder();
            
            var indexName = getCompanyIndexName(companyId);
            foreach(var item in items)
            {             
                sb.Append("{ \"index\" : { \"_id\" : \"" + $"{item.Group}|{item.ID}" + "\", \"_type\" : \"" + item.Group + "\" } }\n");
                sb.Append("{\"Url\" : \"" + item.RelativeUrl + "\",");                
                bool bFirst = true;
                foreach (var f in item.Fields)
                {
                    if (string.IsNullOrEmpty(f.Value))
                        continue;

                    if (!bFirst)
                        sb.Append(", ");
                    else
                        bFirst = false;

                    var val = f.Value.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace("\"", "\\\"");
                    val = HtmlUtilities.RemoveTags(val);
                    sb.Append(" \"" + f.Key + "\" : \"" + val  + "\" ");
                }                
                sb.Append(" }\n");                
            }
            sb.Append("\n");
            

            var webReq = createWebRequest("POST", $"{getCompanyIndexName(companyId)}/_bulk");
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

        public void ClearIndex(int companyID)
        {
            deleteIndexIfExists(companyID);

            createIndexIfNotExists(companyID);            
        }

     
        public void ClearIndex(int companyID, string group)
        {
            createIndexIfNotExists(companyID);

            var webReq = createWebRequest("DELETE", $"{getCompanyIndexName(companyID)}/{group}/_query");
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

        /// <summary>
        /// Gets the search results from elastic search and converts them to index results
        /// </summary>
        /// <param name="companyID"></param>
        /// <param name="resourceID"></param>
        /// <param name="phrase"></param>
        /// <returns></returns>
        public IndexResults GetSearchResultsWithCategory(int companyID, int resourceID, string phrase, int size, int from, List<IndexCategory> categories, string group = "")
        {
            IndexResults result = new IndexResults();

            createIndexIfNotExists(companyID);
            
            var webReq = createWebRequest("POST", $"{getCompanyIndexName(companyID)}/_search");

            StringBuilder sb = new StringBuilder();

            if(!string.IsNullOrEmpty(phrase))
                phrase = phrase.Replace("\"","\\\"");

            sb.Append( "{\"query\":{\"filtered\": {\"query\":  { \"query_string\": { \"query\":\"" + phrase + "\"} }");

            //if a group was specified filter by it
            if (!string.IsNullOrEmpty(group))
            {
                sb.Append(",\"filter\": { \"term\":  { \"Type.raw\": \"" + group + "\" } }");                
            }
                        
            sb.Append("}},\"from\":" + from + ",\"size\":" + size+ ",\"sort\":{ \"_score\":{ \"order\":\"desc\"} }");

            // if no group filter then we need to get list of categories
            if (string.IsNullOrEmpty(group))
            {
                sb.Append(",\"aggs\" : { \"types\" : { \"terms\" : { \"field\" : \"Type.raw\",\"size\": 0 } } }");
            }

            //turn on highlighting

            sb.Append(", \"highlight\": {\"fields\": {\"*\": { \"pre_tags\": [\"<em class='search-highlight'>\"],\"post_tags\": [\"</em>\"],\"number_of_fragments\" : 0 }},\"require_field_match\": false  }");

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
                    Name = GetHighlightedPropertyValueIfExists(h, "Name"),
                    NormalizedScore = (searchResults.hits.max_score.GetValueOrDefault() == 0 ? 0 : (h._score/searchResults.hits.max_score.GetValueOrDefault()*100)),
                    Score = h._score,
                    Type = GetHighlightedPropertyValueIfExists(h, "Type"),
                    Url = GetHighlightedPropertyValueIfExists(h, "Url")
                }).ToList();


            if (searchResults.aggregations != null && searchResults.aggregations.types != null && searchResults.aggregations.types.buckets != null)
            {
                categories.AddRange(searchResults.aggregations.types.buckets.Select(h => new IndexCategory
                {
                    Name = h.Key,
                    ResultCount = h.doc_count
                }).OrderBy(x =>x.Name));
            }
            
            result.ElapsedMS = searchResults.took;

            if(searchResults.hits != null)
                result.Matches = searchResults.hits.total;

            return result;
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

            var webReq = createWebRequest("POST", $"{getCompanyIndexName(companyID)}/_search");

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

        private string GetHighlightedPropertyValueIfExists(SearchResultsHitModel h, string propName)
        {
            var highlightVal = GetPropertyValue<string>(h.highlight, propName);

            if (!string.IsNullOrEmpty(highlightVal)) return highlightVal;

            return GetPropertyValue<string>(h._source, propName);
        }


        private T GetPropertyValue<T>(JObject _source, string propName)
        {
            JToken jToken = null;
            
            if (_source.TryGetValue(propName, out jToken))
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
            createIndexIfNotExists(item.CompanyID);

            var webReq = createWebRequest("DELETE", $"{getCompanyIndexName(item.CompanyID)}/{item.Group}/{createItemID(item)}");            
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.OK)
                throw new ApplicationException(response.StatusMessage);
        }

        public void RemoveFromIndex(List<RemoveFromIndexModel> items)
        {
            if (items == null || items.Count < 0) return;

            createIndexIfNotExists(items[0].CompanyID);

            StringBuilder sb = new StringBuilder();

            foreach (var item in items)
            {
                sb.Append("{ \"delete\" : { \"_type\" : \"" + item.Group + "\", \"_id\" : \"" + createItemID(item) + "\"}}\n");
            }
                        
            var webReq = createWebRequest("POST", $"{getCompanyIndexName(items[0].CompanyID)}/_bulk");
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
            createIndexIfNotExists(item.CompanyID);

            var webReq = createWebRequest("PUT", $"{getCompanyIndexName(item.CompanyID)}/{item.Group}/{createItemID(item)}");
            loadMessageInRequestBody(webReq, createDocument(item));
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.OK)
                throw new ApplicationException(response.StatusMessage);
        }

        public void UpdateInIndex(List<UpdateInIndexModel> items)
        {
            if (items == null || items.Count < 0) return;

            createIndexIfNotExists(items[0].CompanyID);

            StringBuilder sb = new StringBuilder();

            foreach (var item in items)
            {
                sb.Append("{ \"update\" : { \"_type\" : \"" + item.Group + "\", \"_id\" : \"" + createItemID(item) + "\"}}\n");

                sb.Append("{ \"doc\" : {\"Url\" : \"" + item.RelativeUrl + "\",");
                bool bFirst = true;
                foreach (var f in item.Fields)
                {
                    if (!bFirst)
                        sb.Append(", ");
                    else
                        bFirst = false;
                    sb.Append(" \"" + f.Key + "\" : \"" + f.Value.Replace("\r", "").Replace("\n", "").Replace("\t", "") + "\" ");
                }
                sb.Append(" } }\n");
            }

            var webReq = createWebRequest("POST", $"{getCompanyIndexName(items[0].CompanyID)}/_bulk");
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
