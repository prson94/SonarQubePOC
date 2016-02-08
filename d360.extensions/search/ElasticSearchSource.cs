using System.Collections.Generic;
using d360.core.queue;
using Newtonsoft.Json.Linq;
using System.Net;
using System;
using Newtonsoft.Json;
using System.Linq;

namespace d360.extensions.search
{
    public class JsonResponseModel
    {
        public JObject Data { get; set; }
        public HttpStatusCode Status { get; set; }
        public string StatusMessage { get; set; }
    }

    /*
{
  "took": 1,
  "timed_out": false,
  "_shards": {
    "total": 1,
    "successful": 1,
    "failed": 0
  },
  "hits": {
    "total": 1,
    "max_score": 0.59884083,
    "hits": [
      {
        "_index": "d3s4",
        "_type": "artifact",
        "_id": "733",
        "_score": 0.59884083,
        "_source": {
          "Name": "Eagle PACE",
          "Description": "Houses equity data in a trading system."
        }
      }
    ]
  }
}    
        */
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
        public float maxscore { get; set; }
        public List<SearchResultsHitModel> hits { get; set; }
    }

    public class SearchResultsHitModel
    {
        public string _index { get; set; }
        public string _type { get; set; }
        public string _id { get; set; }
        public float score { get; set; }
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

        //void deleteDocument(IndexWriter writer, IndexObjectModel item)
        //{
        //    writer.DeleteDocuments(
        //        new TermQuery(new Term("Group", item.Group)),
        //        new TermQuery(new Term("Type", item.Type)),
        //        new TermQuery(new Term("ID", item.ID.ToString()))
        //    );
        //}

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
            return model;
        }

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

        #endregion

        public void AddToIndex(AddToIndexModel item)
        {
            createIndexIfNotExists(item.CompanyID);

            var webReq = createWebRequest("PUT", $"{getCompanyIndexName(item.CompanyID)}/{item.Group}/{createItemID(item)}");
            loadMessageInRequestBody(webReq, createDocument(item));
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.Created)
                throw new ApplicationException(response.StatusMessage);
        }

        public void AddToIndex(List<AddToIndexModel> items)
        {
            createIndexIfNotExists(items[0].CompanyID);
            var json = "";

            var indexName = getCompanyIndexName(items[0].CompanyID);
            items.ForEach(item => {
                json += "{ \"index\" : { \"_id\" : \"" + $"{item.Group}|{item.ID}" + "\" } }\n";
                json += "{ ";
                json += "\"Url\" : \"" + item.RelativeUrl + "\" }\n";
                foreach (var f in item.Fields)
                {
                    json += " \"" + f.Key + "\" : \"" + f.Value.Replace("\\r", "").Replace("\\n", "").Replace("\\t", "") + "\"";
                }
                json += " }\n";
            });
            json += "\n";

            /*
{ "update" : { "_id" : "Artifact|732" } }
{ "doc": { "Url" : "#/artifacts/2/732", "Name" : "Security Master Data Mart", "Description" : "<p>A dimensional model containing security master data sourced from vendors.</p>", "Status" : "Under Review", "Type" : "Application", "SubjectArea" : "Investments" } }
{ "update" : { "_id" : "Artifact|733" } }
{ "doc": { "Url" : "#/artifacts/2/733", "Name" : "Data Warehouse", "Description" : "<p> Enterprise security master and DWH system</p>", "Status" : "Certified", "Type" : "Application", "SubjectArea" : "Enterprise Applications" } }
{ "create" : { "_id" : "Artifact|4651" } }
{ "Url" : "#/artifacts/1/4651", "Name" : "Country of Risk", "Description" : "<p>Provides a way to communicate the true geographic risk of a company. The country of risk is the International Organization for Standardization (ISO) country code of the issuer's principal place of business. It is derived from four factors listed in order of importance: management location, country of primary listing, sales/revenue and reporting currency of the issuer.        Exceptions are made for American Depositary Receipts (ADR's) and Hong Kong 'H' Shares where the country of listing will not be a factor.'   Vendor sourced values may be overridden by the portfolio manager with the approval of the Data Governance team.</p>", "Status" : "Certified", "Type" : "Business Term", "SubjectArea" : "Investments" }
            
            */

            var webReq = createWebRequest("POST", $"{getCompanyIndexName(items[0].CompanyID)}/{items[0].Group}/_bulk");
            loadMessageInRequestBody(webReq, json);
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.Created)
                throw new ApplicationException(response.StatusMessage);
        }

        public void ClearIndex(int companyID)
        {
            //createIndexIfNotExists(companyID);

            //var client = getSearchClient();

            //var deleteResponse = client.DeleteIndex(getCompanyIndexName(companyID));
            //if (!deleteResponse.IsValid)
            //    throw deleteResponse.OriginalException;

            //var createResponse = client.CreateIndex(getCompanyIndexName(companyID));
            //if (!createResponse.IsValid)
            //    throw createResponse.OriginalException;
        }

        public void ClearIndex(int companyID, string group)
        {
            createIndexIfNotExists(companyID);

            //var indices = Indices.Parse(getCompanyIndexName(companyID));
            //var types = Types.Parse(group);
            //var req= new DeleteRequest(getCompanyIndexName(companyID), )
            //var deleteResponse = client.DeleteMany(indices, types);
            //if (!deleteResponse.Success)
            //    throw deleteResponse.OriginalException;
        }
      
        public IEnumerable<string> GetSearchPhrases(int companyID, string term, int maxResults)
        {
            createIndexIfNotExists(companyID);
            return null;
        }

        public List<IndexResult> GetSearchResults(int companyID, int resourceID, string phrase)
        {
            createIndexIfNotExists(companyID);
            var webReq = createWebRequest("GET", $"{getCompanyIndexName(companyID)}/_search?q={phrase}");
            var response = getJsonResponse(webReq);
            if (response.Status != HttpStatusCode.OK)
                throw new ApplicationException(response.StatusMessage);

            var results = response.Data.ToObject<SearchResultsModel>();

            return results.hits.hits.Select(h => new IndexResult {
                Description = h._source.Properties().Values("Description").Value<string>(),
                Group = h._type,
                ID = h._id,
                //Name = h._source.Values("Name").Value<string>(),
                NormalizedScore = h.score/results.hits.maxscore,
                Score = h.score//,
                //Type = h._source.Values("Type").Value<string>(),
                //Url = h._source.Values("Url").Value<string>()
            }).ToList();

            //Group = doc.Get("Group") + "",
            //Name = doc.Get("Name") + "",
            //Type = doc.Get("Type"),
            //ID = doc.Get("ID"),
            //Description = doc.Get("Description") + "",
            //Url = doc.Get("Url") + "",
            //NormalizedScore = x.Score / maxScore,
            //Score = x.Score

            //return null;
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
            createIndexIfNotExists(items[0].CompanyID);

            //var client = getSearchClient();
            //var req = new BulkRequest(getCompanyIndexName(items[0].CompanyID), new TypeName { Name = items[0].Group });
            //items.ForEach(item => {
            //    req.Operations.Add(new BulkDeleteOperation<JObject>(createDocument(item)));
            //});
            //var response = client.Bulk(req);

            //if (!response.IsValid && response.OriginalException != null)
            //    throw response.OriginalException;
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
            createIndexIfNotExists(items[0].CompanyID);

            //var client = getSearchClient();
            //var req = new BulkRequest(getCompanyIndexName(items[0].CompanyID), new TypeName { Name = items[0].Group });
            //items.ForEach(item => {
            //    req.Operations.Add(new BulkUpdateOperation<JObject, JObject>(createDocument(item)));
            //});
            //var response = client.Bulk(req);

            //if (!response.IsValid && response.OriginalException != null)
            //    throw response.OriginalException;
        }
    }
}
