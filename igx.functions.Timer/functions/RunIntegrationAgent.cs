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
    public class GenericIgcAssetsModel
    {
        public List<GenericIgcAssetModel> items { get; set; }
        public GenericIgcPagingModel paging { get; set; }
    }

    public class GenericIgcAssetModel
    {
        public string _id { get; set; }
        public string _name { get; set; }
        public string _type { get; set; }
        public string _url { get; set; }
        public string short_description { get; set; }
        public List<GenericIgcContextModel> _context { get; set; }
    }

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

    public static class RunIntegrationAgent
    {
        const string functionName = "RunIntegrationAgent";
        const string timerSettings = "0 */10 * * * *";
        //const string timerSettings = "*/5 * * * * *";

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

        internal static void LoadSource(
            string sourceUri, string sourceAuthString, string sourceSearchString,
            string detailTransformation,
            string targetUri, string targetAuthString, SystemObjects targetType, int targetTypeID)
        {
            //var jsonRaw = GetJsonFromApi($"{sourceUri}search/{sourceSearchString}", sourceAuthString);

            //var idTransformation = @"{ ids: { '#loop($.items)': { id: '#valueof($._id)' } } }";
            //var convertedIds = JsonTransformer.Transform(idTransformation, jsonRaw);
            //var rawObj = JObject.Parse(convertedIds);

            //var IDs = rawObj.SelectToken("ids").Select(i => i["id"].Value<string>()).ToList();
            var IDs = new List<string>();
            GenericIgcAssetsModel model = null;
            string jsonRaw;
            string url;

            url = $"{sourceUri}search/{sourceSearchString}";

            while (!string.IsNullOrEmpty(url))
            {
                jsonRaw = GetJsonFromApi(url, sourceAuthString);
                model = JsonConvert.DeserializeObject<GenericIgcAssetsModel>(jsonRaw);
                IDs.AddRange(model.items.Select(i => i._id));
                url = model.paging.next;
            }

            var arr = new JArray();
            foreach (var id in IDs)
            {
                jsonRaw = GetJsonFromApi($"{sourceUri}assets/{id}", sourceAuthString);
                var converted = JsonTransformer.Transform(detailTransformation, jsonRaw);

                arr.Add(JObject.Parse(converted));
            }

            var respString = PostJsonToApi(
                $"{targetUri}{targetType.ToString()}/{targetTypeID}/bulk",
                targetAuthString,
                arr.ToString()
            );
        }

        [FunctionName(functionName), Disable()]
        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TraceWriter log) //   
        {
            //trigger every two hours: https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer#schedule-examples

            try
            {
                CoreFunction.AITrackJobStart(functionName);
                //var targetUri = "http://demo.dev.data3sixty.local/services/assets/";
                var targetUri = "https://ssb.dev.data3sixty.com/services/assets/";
                var targetAuthString = "w7gt581AOMXhXeW9mh0jWCPMe;3=f+7afAQUq9wUZgyibXq9kGa2iLGS3M0r-Ex-ZxJ6O9TAu+-7";

                var sourceUri = "https://192.168.99.100:9443/ibm/iis/igc-rest/v1/";
                var sourceAuthString = "Basic aXNhZG1pbjppc2FkbWlu";

                #region General

                LoadSource(
                    sourceUri, sourceAuthString, "$ApplicationCatalog-ApplicationCatalog",
                    @"{ SourceID: '#valueof($._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)',  CMDBApplicationCode: '#valueof($.$CMDBAppCode)' }",
                    targetUri, targetAuthString, SystemObjects.ArtifactType, 2);

                #endregion

                #region BCM

                LoadSource(
                    sourceUri, sourceAuthString, "$BCM-Lifecycle",
                    @"{ SourceID: '#valueof($._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)', CMDBApplicationCode: '#valueof($.long_description)' }",
                    targetUri, targetAuthString, SystemObjects.ArtifactType, 3);

                LoadSource(
                    sourceUri, sourceAuthString, "$BCM-Function",
                    @"{ SourceID: '#valueof($._id)', ParentSourceID: '#valueof($._context[0]._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                    targetUri, targetAuthString, SystemObjects.ArtifactType, 4);

                LoadSource(
                    sourceUri, sourceAuthString, "$BCM-SubFunction",
                    @"{ SourceID: '#valueof($._id)', ParentSourceID: '#valueof($._context[1]._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                    targetUri, targetAuthString, SystemObjects.ArtifactType, 5);

                #endregion

                #region Usage Hierarchy

                LoadSource(
                    sourceUri, sourceAuthString, "$UsageHierarchy-Division",
                    @"{ SourceID: '#valueof($._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                    targetUri, targetAuthString, SystemObjects.ArtifactType, 6);

                LoadSource(
                    sourceUri, sourceAuthString, "$UsageHierarchy-BusinessUnit",
                    @"{ SourceID: '#valueof($._id)', ParentSourceID: '#valueof($._context[0]._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                    targetUri, targetAuthString, SystemObjects.ArtifactType, 7);

                LoadSource(
                    sourceUri, sourceAuthString, "$UsageHierarchy-Function",
                    @"{ SourceID: '#valueof($._id)', ParentSourceID: '#valueof($._context[1]._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                    targetUri, targetAuthString, SystemObjects.ArtifactType, 8);


                LoadSource(
                    sourceUri, sourceAuthString, "$UsageHierarchy-SubFunction",
                    @"{ SourceID: '#valueof($._id)', ParentSourceID: '#valueof($._context[2]._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                    targetUri, targetAuthString, SystemObjects.ArtifactType, 9);

                #endregion

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
