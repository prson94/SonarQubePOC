using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.IO;
using d360.core;
using Newtonsoft.Json.Linq;

namespace igx.tests
{
    public class GenericIgcContextModel
    {
        public string _type { get; set; }
        public string _id { get; set; }
        public string _url { get; set; }
        public string _name { get; set; }
    }

    public class GenericIgcPagingModel
    {
        public int numTotal { get; set; }
        public string next { get; set; }
        public int pageSize { get; set; }
        public int end { get; set; }
        public int begin { get; set; }
    }

    public class IgcModel
    {
        [JsonProperty(PropertyName = "_id")]
        public string SourceID { get; set; }

        [JsonProperty(PropertyName = "_name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "short_description")]
        public string ShortDescription { get; set; }

        [JsonProperty(PropertyName = "_url")]
        public string IgcUrl { get; set; }

        [JsonProperty(PropertyName = "_type")]
        public string Type { get; set; }

        public List<GenericIgcContextModel> _context { get; set; }
    }

    #region Specific

    public class IgcApplicationCatalogsModel
    {
        public List<IgcApplicationCatalogModel> items { get; set; }
        public GenericIgcPagingModel paging { get; set; }
    }

    public class IgcApplicationCatalogModel : IgcModel
    {
        [JsonProperty(PropertyName = "$MaturityLevel")]
        public string MaturityLevel { get; set; }

        [JsonProperty(PropertyName = "$CMDBAppCode")]
        public string CMDBAppCode { get; set; }

        [JsonProperty(PropertyName = "$PersonalData")]
        public string PersonalData { get; set; }

        [JsonProperty(PropertyName = "$ComponentType")]
        public string ComponentType { get; set; }

        [JsonProperty(PropertyName = "$DataOwner")]
        public string DataOwner { get; set; }

        [JsonProperty(PropertyName = "$DataSteward")]
        public string DataSteward { get; set; }

        //[JsonProperty("$KeyApplicationType")]
        //public string KeyApplicationType { get; set; }

        [JsonProperty(PropertyName = "$AuthoritativeSource")]
        public string AuthoritativeSource { get; set; }

        [JsonProperty(PropertyName = "$BusinessOwnerId")]
        public string BusinessOwnerId { get; set; }

        [JsonProperty(PropertyName = "$ComponentSAID")]
        public string ComponentSAID { get; set; }

        [JsonProperty(PropertyName = "$BookOfRecord")]
        public string BookOfRecord { get; set; }

        [JsonProperty(PropertyName = "$DataLocation")]
        public string DataLocation { get; set; }

        [JsonProperty(PropertyName = "$Comments")]
        public string Comments { get; set; }

        [JsonProperty(PropertyName = "$ApplicationAlias")]
        public string ApplicationAlias { get; set; }

        [JsonProperty(PropertyName = "long_description")]
        public string LongDescription { get; set; }

        [JsonProperty(PropertyName = "$SSID")]
        public string SSID { get; set; }

        [JsonProperty(PropertyName = "$ApplicationOwner")]
        public string ApplicationOwner { get; set; }

        [JsonProperty(PropertyName = "$Status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "$DataStewardId")]
        public string DataStewardId { get; set; }

        [JsonProperty(PropertyName = "$EDGMStewardId")]
        public string EDGMStewardId { get; set; }

        [JsonProperty(PropertyName = "$BusinessOwner")]
        public string BusinessOwner { get; set; }
    }

    #endregion

    [TestClass]
    public class IgcTests
    {
        internal T GetFromApi<T>(string uri, string authorization)
        {
            var req = HttpWebRequest.CreateHttp(uri);
            req.Accept = "application/json";
            req.Headers.Set(HttpRequestHeader.Authorization, authorization);
            req.ServerCertificateValidationCallback = delegate { return true; };

            var jsonRaw = "";

            var response = req.GetResponse();
            using (var responseStream = response.GetResponseStream())
            {
                using (var rdr = new StreamReader(responseStream))
                {
                    jsonRaw = rdr.ReadToEnd();
                }
            }

            return JsonConvert.DeserializeObject<T>(jsonRaw, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
        }

        void LoadBasedOnSearch(
            string sourceUri, string sourceAuthString, string searchType, string searchFields,
            string targetUri, string targetAuthString, SystemObjects targetType, int targetTypeID)
        {
            var properties = searchFields.Split(',');
            var fullUrl = $"{sourceUri}search/?pageSize=75&types={searchType}";
            foreach (var p in properties)
            {
                fullUrl += $"&properties={p}";
            }

            var arr = new JArray();

            var models = GetFromApi<IgcApplicationCatalogsModel>(fullUrl, sourceAuthString);
            if (models != null)
            {
                models.items.ForEach(m =>
                {
                    arr.Add(JsonConvert.SerializeObject(new {
                        m.ApplicationAlias,
                        m.ApplicationOwner,
                        m.AuthoritativeSource,
                        m.BookOfRecord,
                        m.CMDBAppCode,
                        m.Comments,
                        m.ComponentSAID,
                        m.ComponentType,
                        m.DataLocation,
                        m.LongDescription,
                        m.MaturityLevel,
                        m.Name,
                        m.PersonalData,
                        m.ShortDescription,
                        m.SourceID,
                        m.SSID,
                        m.Status,
                        m.Type
                    }));
                });
            }

            while (!string.IsNullOrEmpty(models.paging.next))
            {
                models = GetFromApi<IgcApplicationCatalogsModel>(models.paging.next, sourceAuthString);
                if (models != null)
                {
                    models.items.ForEach(m =>
                    {
                        arr.Add(JsonConvert.SerializeObject(new
                        {
                            m.ApplicationAlias,
                            m.AuthoritativeSource,
                            m.BookOfRecord,
                            m.CMDBAppCode,
                            m.Comments,
                            m.ComponentSAID,
                            m.ComponentType,
                            m.DataLocation,
                            m.LongDescription,
                            m.MaturityLevel,
                            m.Name,
                            m.PersonalData,
                            m.ShortDescription,
                            m.SourceID,
                            m.SSID,
                            m.Status
                        }));
                    });
                }
            }

            var respString = PostJsonToApi(
                $"{targetUri}{targetType.ToString()}/{targetTypeID}/bulk",
                targetAuthString,
                arr.ToString()
            );
        }

        string PostJsonToApi(string uri, string authorization, string requestBody)
        {
            var jsonToReturn = "";

            using (var client = new WebClient())
            {
                client.Headers.Set(HttpRequestHeader.Accept, "application/json");
                client.Headers.Set(HttpRequestHeader.ContentType, "application/json");
                client.Headers.Set(HttpRequestHeader.Authorization, authorization);
                jsonToReturn = client.UploadString(uri, requestBody);
            }

            return jsonToReturn;
        }


        [TestMethod]
        public void GetApplicationCatalog()
        {
            //var targetUri = "https://ssb.dev.data3sixty.com/services/assets/";
            var targetUri = "http://ssb-igx.dev.data3sixty.local/services/assets/";
            var targetAuthString = "w7gt581AOMXhXeW9mh0jWCPMe;3=f+7afAQUq9wUZgyibXq9kGa2iLGS3M0r-Ex-ZxJ6O9TAu+-7";

            var sourceUri = "https://192.168.99.100:9443/ibm/iis/igc-rest/v1/";
            var sourceAuthString = "Basic aXNhZG1pbjppc2FkbWlu";

            LoadBasedOnSearch(
                sourceUri,
                sourceAuthString,
                "$ApplicationCatalog-ApplicationCatalog",
                "short_description,long_description,labels,stewards,assigned_to_terms,implements_rules,governed_by_rules,$CMDBAppCode,$ApplicationAlias,$BusinessOwner,$BusinessOwnerId,$ApplicationOwner,$ApplicationOwnerId,$DataSteward,$DataStewardId,$DataOwner,$EDGMStewardId,$Comments,$SSID,$KeyApplicationType,$Status,$DataLocation,$PersonalData,$ComponentType,$ComponentCode,$ComponentSAID,$AuthoritativeSource,$MaturityLevel,$BookOfRecord",
                targetUri,
                targetAuthString,
                SystemObjects.ArtifactType, 2);

        }
    }
}
