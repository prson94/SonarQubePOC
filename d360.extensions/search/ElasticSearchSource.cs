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

    public class SearchResultsHitModel
    {
        public string _index { get; set; }
        public string _type { get; set; }
        public string _id { get; set; }
        public float _score { get; set; }
        public JObject _source { get; set; }
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
                loadMessageInRequestBody(webReq, JObject.Parse("{\"settings\": { \"index\": { \"number_of_shards\": 1, \"number_of_replicas\": 1 }}}"));
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
                //loadMessageInRequestBody(webReq, JObject.Parse("{\"settings\": { \"index\": { \"number_of_shards\": 1, \"number_of_replicas\": 1 }}}"));
                response = getJsonResponse(webReq);
                if (response.Status != HttpStatusCode.OK)
                    throw new ApplicationException(response.StatusMessage);
            }
        }

        #endregion

        public void AddToIndex(AddToIndexModel item)
        {
            createIndexIfNotExists(item.CompanyID);

            var webReq = createWebRequest("PUT", $"{getCompanyIndexName(item.CompanyID)}/{item.Group}/{createItemID(item)}");
            loadMessageInRequestBody(webReq, createDocument(item));
            var response = getJsonResponse(webReq);
            
            if (!response.IsSuccessStatusCode)
                throw new ApplicationException(response.StatusMessage);
        }

        public void AddToIndex(List<AddToIndexModel> items)
        {
            if (items == null || items.Count < 0) return;

            createIndexIfNotExists(items[0].CompanyID);
            var sb = new StringBuilder();
            
            var indexName = getCompanyIndexName(items[0].CompanyID);
            items.ForEach(item => {
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
                    sb.Append(" \"" + f.Key + "\" : \"" + val  + "\" ");
                }                
                sb.Append(" }\n");                
            });
            sb.Append("\n");
            
            /*
{ "update" : { "_id" : "Artifact|732" } }
{ "doc": { "Url" : "#/artifacts/2/732", "Name" : "Security Master Data Mart", "Description" : "<p>A dimensional model containing security master data sourced from vendors.</p>", "Status" : "Under Review", "Type" : "Application", "SubjectArea" : "Investments" } }
{ "update" : { "_id" : "Artifact|733" } }
{ "doc": { "Url" : "#/artifacts/2/733", "Name" : "Data Warehouse", "Description" : "<p> Enterprise security master and DWH system</p>", "Status" : "Certified", "Type" : "Application", "SubjectArea" : "Enterprise Applications" } }
{ "create" : { "_id" : "Artifact|4651" } }
{ "Url" : "#/artifacts/1/4651", "Name" : "Country of Risk", "Description" : "<p>Provides a way to communicate the true geographic risk of a company. The country of risk is the International Organization for Standardization (ISO) country code of the issuer's principal place of business. It is derived from four factors listed in order of importance: management location, country of primary listing, sales/revenue and reporting currency of the issuer.        Exceptions are made for American Depositary Receipts (ADR's) and Hong Kong 'H' Shares where the country of listing will not be a factor.'   Vendor sourced values may be overridden by the portfolio manager with the approval of the Data Governance team.</p>", "Status" : "Certified", "Type" : "Business Term", "SubjectArea" : "Investments" }
            
            */

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
        public List<IndexResult> GetSearchResults(int companyID, int resourceID, string phrase)
        {
            createIndexIfNotExists(companyID);
            var webReq = createWebRequest("GET", $"{getCompanyIndexName(companyID)}/_search?q={phrase}");
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.OK)
                throw new ApplicationException(response.StatusMessage);

            var results = response.Data.ToObject<SearchResultsModel>();
            
            return results.hits.hits.Select(h => new IndexResult {
                    Description = GetPropertyValue<string>(h._source, "Description"),
                    Group = h._type,
                    ID = h._id,
                    Name = GetPropertyValue<string>(h._source, "Name"),
                    NormalizedScore = (results.hits.max_score.GetValueOrDefault() == 0 ? 0 : h._score/results.hits.max_score.GetValueOrDefault()),
                    Score = h._score,
                    Type = GetPropertyValue<string>(h._source, "Type"),
                    Url = GetPropertyValue<string>(h._source, "Url")
                }).ToList();
        }

        private T GetPropertyValue<T>(JObject _source, string propName)
        {
            JToken jToken = null;
            
            if (_source.TryGetValue(propName, out jToken))
            {
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
