using Dapper;
using d360.core;
using d360.core.entities;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.model;
using d360.utils.company;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Data.Entity.Design.PluralizationServices;
using igx.functions.Core;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using JUST;
using Newtonsoft.Json.Linq;

namespace igx.functions.Timer
{
    public class GenericIgcAssets
    {

        public List<GenericIgcAsset> items { get; set; }
    }

    public class GenericIgcAsset
    {
        public string _id { get; set; }
        public string _name { get; set; }
        public string _type { get; set; }
        public string _url { get; set; }
        public string short_description { get; set; }
    }

    public static class RunIntegrationAgent
    {
        const string functionName = "RunIntegrationAgent";
        //const string timerSettings = "0 */10 * * * *";
        const string timerSettings = "*/5 * * * * *";

        internal static string GetJsonFromApi(string uri, string authorization)
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

            return jsonRaw;
        }

        internal static string PostJsonToApi(string uri, string authorization, string requestBody)
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

        [FunctionName(functionName)]
        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TraceWriter log) //   
        {
            //trigger every two hours: https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer#schedule-examples

            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var baseIntegrationUri = "http://demo.dev.data3sixty.local/services/assets/";
                var baseIntegrationAuthString = "w7gt581AOMXhXeW9mh0jWCPMe;3=f+7afAQUq9wUZgyibXq9kGa2iLGS3M0r-Ex-ZxJ6O9TAu+-7";

                var baseUri = "https://192.168.99.100:9443/ibm/iis/igc-rest/v1/";
                var authString = "Basic aXNhZG1pbjppc2FkbWlu";

                var jsonRaw = GetJsonFromApi($"{baseUri}search/%24BCM-Lifecycle", authString);


                var idTransformation = @"{ ids: { '#loop($.items)': { id: '#valueof($._id)' } } }";
                var convertedIds = JsonTransformer.Transform(idTransformation, jsonRaw);
                var rawObj = JObject.Parse(convertedIds);

                var IDs = rawObj.SelectToken("ids").Select(i => i["id"].Value<string>()).ToList();

                var arr = new JArray();
                foreach (var id in IDs)
                {
                    jsonRaw = GetJsonFromApi($"{baseUri}assets/{id}", authString);
                    var transform = @"{ TaxonomyTypeID: 1, SourceID: '#valueof($._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }";
                    var converted = JsonTransformer.Transform(transform, jsonRaw);

                    arr.Add(JObject.Parse(converted));
                }

                //var convertedJson = JsonTransformer.Transform(transform, jsonRaw);

                //var transObj = JObject.Parse(transform);
                //var rawObj = JObject.Parse(jsonRaw);
                //var convertedJson = JsonTransformer.Transform(transObj, rawObj);

                //var items = JsonConvert.DeserializeObject<GenericIgcAssets>(jsonRaw);


                var respString = PostJsonToApi(
                    $"{baseIntegrationUri}ArtifactType/202/bulk",
                    baseIntegrationAuthString, 
                    arr.ToString()
                );

                //var companies = CoreFunction.GetCompaniesByCurrentSlot();

                //companies.ForEach(c =>
                //{
                //    try
                //    {
                //        var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                //        companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);




                //        companyConnection.Close();
                //        companyConnection.Dispose();
                //    }
                //    catch (Exception ex)
                //    {
                //        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                //        log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                //    }
                //});

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.Error($"General Exception: {ex.GetFullExceptionData()}");
            }
        }
    }
}
