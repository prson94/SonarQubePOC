using d360.core;
using d360.model;
using igx.functions.integration.igc;
using JUST;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace igx.functions
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

        internal static string TransformJson(string transformation, string jsonRaw)
        {
            return JsonTransformer.Transform(transformation, jsonRaw);
        }

        internal static string GetTransformedDataFromApi(string sourceUri, string sourceAuthString, string sourceSearchString, string transformation, string filePath = "")
        {
            string jsonRaw = "";

            if (string.IsNullOrEmpty(filePath))
            {
                var url = $"{sourceUri}search/{sourceSearchString}"; ;
                jsonRaw = GetJsonFromApi(url, sourceAuthString);
            }
            else
            {
                jsonRaw = File.ReadAllText(filePath);
            }

            return TransformJson(transformation, jsonRaw);
        }

        internal static void LoadSource(
            string sourceUri, string sourceAuthString, string sourceSearchString,
            string detailTransformation,
            string targetUri, string targetAuthString, SystemObjects targetType, int targetTypeID, string jsonRaw = "")
        {
            //var jsonRaw = GetJsonFromApi($"{sourceUri}search/{sourceSearchString}", sourceAuthString);

            //var idTransformation = @"{ ids: { '#loop($.items)': { id: '#valueof($._id)' } } }";
            //var convertedIds = JsonTransformer.Transform(idTransformation, jsonRaw);
            //var rawObj = JObject.Parse(convertedIds);

            //var IDs = rawObj.SelectToken("ids").Select(i => i["id"].Value<string>()).ToList();
            var IDs = new List<string>();
            GenericIgcAssetsModel model = null;
            string url;

            url = $"{sourceUri}search/{sourceSearchString}";

            var arr = new JArray();

            if (string.IsNullOrEmpty(jsonRaw))
            {
                // Raw json came in already provided. No need to retrieve it from source server.
                var converted = TransformJson(detailTransformation, jsonRaw);
                arr.Add(JObject.Parse(converted));
            }
            else
            {
                // Gets all the IDs we need to get details for below.
                while (!string.IsNullOrEmpty(url))
                {
                    jsonRaw = GetJsonFromApi(url, sourceAuthString);
                    model = JsonConvert.DeserializeObject<GenericIgcAssetsModel>(jsonRaw);
                    IDs.AddRange(model.items.Select(i => i._id));
                    url = model.paging.next;
                }
                
                // Loop through each ID and get the details for it.
                foreach (var id in IDs)
                {
                    jsonRaw = GetJsonFromApi($"{sourceUri}assets/{id}", sourceAuthString);
                    var converted = TransformJson(detailTransformation, jsonRaw);
                    arr.Add(JObject.Parse(converted));
                }
            }

            var respString = PostJsonToApi(
                $"{targetUri}{targetType.ToString()}/{targetTypeID}/bulk",
                targetAuthString,
                arr.ToString()
            );
        }

        internal static void postMultipart(string baseUri, string uri, string authString, object obj)
        {
            var api = new HttpClient();
            api.BaseAddress = new Uri(baseUri);
            api.DefaultRequestHeaders.TransferEncodingChunked = true;
            api.Timeout = new TimeSpan(0, 1, 0, 0, 0);
            api.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authString);

            using (var content = new MultipartContent())
            {
                content.Add(
                    new StreamContent(
                        new MemoryStream(
                            Encoding.UTF8.GetBytes(
                                JsonConvert.SerializeObject(obj)
                            )
                        )
                    )
                );

                var message = api.PostAsync(uri, content).Result;

                var json = message.Content.ReadAsStringAsync().Result;
            }
        }

        [FunctionName(functionName), Disable()]
        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TraceWriter log) //   
        {
            //trigger every two hours: https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer#schedule-examples

            try
            {
                CoreFunction.AITrackJobStart(functionName);
                //var targetUri = "http://demo.dev.data3sixty.local/services/assets/";
                var targetUri = "http://ssb-igx.dev.data3sixty.local/services/assets/";
                //var targetUri = "https://ssb.dev.data3sixty.com/services/assets/";
                var targetAuthString = "w7gt581AOMXhXeW9mh0jWCPMe;3=f+7afAQUq9wUZgyibXq9kGa2iLGS3M0r-Ex-ZxJ6O9TAu+-7";

                var sourceUri = "https://192.168.99.100:9443/ibm/iis/igc-rest/v1/";
                var sourceAuthString = "Basic aXNhZG1pbjppc2FkbWlu";

                #region General

                //LoadSource(
                //    sourceUri, sourceAuthString, "$ApplicationCatalog-ApplicationCatalog",
                //    @"{ SourceID: '#valueof($._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)',  CMDBApplicationCode: '#valueof($.$CMDBAppCode)' }",
                //    targetUri, targetAuthString, SystemObjects.ArtifactType, 2);

                #endregion

                #region BCM

                //LoadSource(
                //    sourceUri, sourceAuthString, "$BCM-Lifecycle",
                //    @"{ SourceID: '#valueof($._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)', CMDBApplicationCode: '#valueof($.long_description)' }",
                //    targetUri, targetAuthString, SystemObjects.ArtifactType, 3);

                //LoadSource(
                //    sourceUri, sourceAuthString, "$BCM-Function",
                //    @"{ SourceID: '#valueof($._id)', ParentSourceID: '#valueof($._context[0]._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                //    targetUri, targetAuthString, SystemObjects.ArtifactType, 4);

                //LoadSource(
                //    sourceUri, sourceAuthString, "$BCM-SubFunction",
                //    @"{ SourceID: '#valueof($._id)', ParentSourceID: '#valueof($._context[1]._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                //    targetUri, targetAuthString, SystemObjects.ArtifactType, 5);

                #endregion

                #region Usage Hierarchy

                //LoadSource(
                //    sourceUri, sourceAuthString, "$UsageHierarchy-Division",
                //    @"{ SourceID: '#valueof($._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                //    targetUri, targetAuthString, SystemObjects.ArtifactType, 6);

                //LoadSource(
                //    sourceUri, sourceAuthString, "$UsageHierarchy-BusinessUnit",
                //    @"{ SourceID: '#valueof($._id)', ParentSourceID: '#valueof($._context[0]._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                //    targetUri, targetAuthString, SystemObjects.ArtifactType, 7);

                //LoadSource(
                //    sourceUri, sourceAuthString, "$UsageHierarchy-Function",
                //    @"{ SourceID: '#valueof($._id)', ParentSourceID: '#valueof($._context[1]._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                //    targetUri, targetAuthString, SystemObjects.ArtifactType, 8);


                //LoadSource(
                //    sourceUri, sourceAuthString, "$UsageHierarchy-SubFunction",
                //    @"{ SourceID: '#valueof($._id)', ParentSourceID: '#valueof($._context[2]._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                //    targetUri, targetAuthString, SystemObjects.ArtifactType, 9);

                #endregion

                #region Fusion Data

                List<Dictionary<string, string>> postModels;

                //var structureWithChildren = "";

                #region Basel II

                // Database
                var b_database = JsonConvert.DeserializeObject<DatabaseModel>(File.ReadAllText(@"C:\Users\mike\Desktop\SSG - Samples for IGC\JSON Samples\Basel II Datawarhouse.txt"));
                //structureWithChildren =
                //    GetTransformedDataFromApi(
                //        sourceUri, sourceAuthString, "",
                //        @"{ 
                //            SourceID: '#valueof($._id)',
                //            Name: '#valueof($._name)',
                //            'Items': { 
                //                '#loop($.database_schemas.items)': { 'SourceID': '#currentvalueatpath($._id)', ParentSourceID: '#valueof($._context[0]._id)',  'Name': '#currentvalueatpath($._name)' } 
                //            } 
                //          }",
                //        @"C:\Users\mike\Desktop\SSG - Samples for IGC\JSON Samples\Basel II Datawarhouse.txt"
                //    );

                // Schema
                var b_schema = JsonConvert.DeserializeObject<DatabaseSchemaModel>(File.ReadAllText(@"C:\Users\mike\Desktop\SSG - Samples for IGC\JSON Samples\Basel II Datawarhouse Schema.txt"));
                //structureWithChildren =
                //    GetTransformedDataFromApi(
                //        sourceUri, sourceAuthString, "",
                //        @"{ 
                //            SourceID: '#valueof($._id)',
                //            Name: '#valueof($._name)',
                //            'Items': { 
                //                '#loop($.database_tables.items)': { 'SourceID': '#currentvalueatpath($._id)', 'Name': '#currentvalueatpath($._name)' } 
                //            } 
                //          }",
                //        @"C:\Users\mike\Desktop\SSG - Samples for IGC\JSON Samples\Basel II Datawarhouse Schema.txt"
                //    );
                //LoadSource(
                //    sourceUri, sourceAuthString, "BaselII",
                //    @"{ SourceID: '#valueof($._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                //    targetUri, targetAuthString, SystemObjects.FusionAttributeType, 2);

                // Table
                //LoadSource(
                //    sourceUri, sourceAuthString, "BaselII",
                //    @"{ SourceID: '#valueof($._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                //    targetUri, targetAuthString, SystemObjects.FusionAttributeType, 3);

                // Column
                //LoadSource(
                //    sourceUri, sourceAuthString, "BaselII",
                //    @"{ SourceID: '#valueof($._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                //    targetUri, targetAuthString, SystemObjects.FusionAttributeType, 4);

                postModels = new List<Dictionary<string, string>>();

                postModels.Add(new Dictionary<string, string> {
                    { "SourceID", b_schema._id },
                    { "Name", b_schema._name },
                    { "FusionAttributeTypeID", "2" },
                    { "Action", "A" }
                });

                foreach (var t in b_schema.database_tables.items)
                {
                    postModels.Add(new Dictionary<string, string> {
                        { "SourceID", t._id },
                        { "Name", t._name },
                        { "ParentSourceID", b_schema._id },
                        { "FusionAttributeTypeID", "3" },
                        { "Action", "A" }
                    });
                }

                postMultipart("https://ssb.dev.data3sixty.com", "/services/fusion/2/configurations/2/attributes", targetAuthString, new BulkFusionImport { Models = postModels });

                #endregion

                #region ORI

                //Database
                var ori_database = JsonConvert.DeserializeObject<DatabaseModel>(File.ReadAllText(@"C:\Users\mike\Desktop\SSG - Samples for IGC\JSON Samples\ORI Database.txt"));

                // Schema
                var ori_schema = JsonConvert.DeserializeObject<DatabaseSchemaModel>(File.ReadAllText(@"C:\Users\mike\Desktop\SSG - Samples for IGC\JSON Samples\ORI Schema.txt"));
                //LoadSource(
                //    sourceUri, sourceAuthString, "$ORI",
                //    @"{ SourceID: '#valueof($._id)', ParentSourceID: '#valueof($._context[0]._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                //    targetUri, targetAuthString, SystemObjects.FusionAttributeType, 2);

                // Table
                var ori_tables = JsonConvert.DeserializeObject<List<DatabaseTableModel>>(File.ReadAllText(@"C:\Users\mike\Desktop\SSG - Samples for IGC\JSON Samples\ORI Schema.Table.txt"));
                //LoadSource(
                //    sourceUri, sourceAuthString, "$ORI",
                //    @"{ SourceID: '#valueof($._id)', ParentSourceID: '#valueof($._context[0]._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                //    targetUri, targetAuthString, SystemObjects.FusionAttributeType, 3);

                // Column
                //LoadSource(
                //    sourceUri, sourceAuthString, "$ORI",
                //    @"{ SourceID: '#valueof($._id)', ParentSourceID: '#valueof($._context[0]._id)', Name: '#valueof($._name)', Description: '#valueof($.short_description)', LongDescription: '#valueof($.long_description)' }",
                //    targetUri, targetAuthString, SystemObjects.FusionAttributeType, 4);

                postModels = new List<Dictionary<string, string>>();

                postModels.Add(new Dictionary<string, string> {
                    { "SourceID", ori_schema._id },
                    { "Name", ori_schema._name },
                    { "FusionAttributeTypeID", "2" },
                    { "Action", "A" }
                });

                foreach (var t in ori_tables)
                {
                    postModels.Add(new Dictionary<string, string> {
                        { "SourceID", t._id },
                        { "Name", t._name },
                        { "ParentSourceID", t.database_schema._id },
                        { "FusionAttributeTypeID", "3" },
                        { "Action", "A" }
                    });

                    foreach (var c in t.database_columns.items)
                    {
                        postModels.Add(new Dictionary<string, string> {
                        { "SourceID", c._id },
                        { "Name", c._name },
                        { "ParentSourceID", t._id },
                        { "FusionAttributeTypeID", "4" },
                        { "Action", "A" }
                    });
                    }
                }

                postMultipart("https://ssb.dev.data3sixty.com", "/services/fusion/2/configurations/3/attributes", targetAuthString, new BulkFusionImport { Models = postModels });

                #endregion

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
