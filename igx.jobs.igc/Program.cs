using ApplicationInsights.Helpers.WebJobs;
using d360.core;
using d360.core.entities;
using d360.utils.company;
using Dapper;
using igx.jobs.igc;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace igx.jobs
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class IgcIntegration
    {
        const string functionName = "IGC_Integration";
        //const string timerSettings = "0 */30 * * * *";
        const string timerSettings = "*/5 * * * * *";

        #region State street Settings - NEED to Externalize

        const string SourceUri = "https://edgm-catalog-uat.statestreet.com/ibm/iis/igc-rest/v1/";
        const string SourceAuthString = "Basic dGVzdDM2MDpkYXRhMzYw";   //State Street UAT
        //const string SourceUri = "https://edgm-catalog.statestreet.com/ibm/iis/igc-rest/v1/";
        //const string SourceAuthString = "Basic c3BsRFRTV0VCMjg2MjM6cChMWlsxfF1bYkl1";   //State Street PROD //UID: splDTSWEB28623    PWD: p(LZ[1|][bIu

        #endregion

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            var available = false;
            try
            {
                available = CoreFunction.LockWebJobIfAvailable(functionName);

                if (available)
                {
                    CoreFunction.AITrackJobStart(functionName);

                    var companies = CoreFunction.GetCompaniesByCurrentSlot();
                    companies.ForEach(async c =>
                    {
                        try
                        {
                            var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                            company.OpenWithRetry(RetryPolicy.DefaultFixed);

                            var settings = company.Query<IntegrationSetting>("select * from integration.Setting").ToList();

                            List<IntegrationAssetType> mappings = null;
                            List<IntegrationAssetTypeFieldItem> mappingFields = null;
                            List<IntegrationAssetTypeRelationItem> mappingRelations = null;
                            List<IntegrationAssetTypeRoleItem> mappingRoles = null;

                            // Do this call in here so we do not incur the cost of four DB calls for every database unless we absolutely have to.
                            if (settings.Count > 0)
                            {
                                mappings = company.Query<IntegrationAssetType>("select * from integration.SynchedAssetType").ToList();
                                mappingFields = company.Query<IntegrationAssetTypeFieldItem>("select * from integration.SynchedAssetTypeFieldItem").ToList();
                                mappingRelations = company.Query<IntegrationAssetTypeRelationItem>("select * from integration.SynchedAssetTypeRelationItem").ToList();
                                mappingRoles = company.Query<IntegrationAssetTypeRoleItem>("select * from integration.SynchedAssetTypeRoleItem").ToList();
                            }

                            foreach (var setting in settings)
                            {
                                #region Get the resource for this setting.

                                var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
                                cnn.OpenWithRetry(RetryPolicy.DefaultFixed);
                                var resource = cnn.Query<Resource>("select * from Resource where ID = @id", new { id = setting.TargetResourceID }).SingleOrDefault();
                                cnn.Close();
                                cnn.Dispose();

                                #endregion

                                if (setting.IntegrationSystem == d360.core.enums.IntegrationSystem.IGC)
                                {
                                    foreach (var item in mappings.Where(i => i.Active && i.ToGovern && i.IntegrationSettingID == setting.ID))
                                    {
                                        var fields = mappingFields.Where(i => i.SynchedAssetTypeID == item.ID).ToList();
                                        var relations = mappingRelations.Where(i => i.SynchedAssetTypeID == item.ID).ToList();
                                        var roles = mappingRoles.Where(i => i.SynchedAssetTypeID == item.ID).ToList();

                                        var success = false;

                                        long? epoch = null;

                                        if (item.LastSynchOn.HasValue)
                                        {
                                            epoch = ConvertDateToUnixTimeMilliseconds(item.LastSynchOn.Value);
                                        }

                                        success = IGC_LoadAssetsByMappingTypeAsync(c.CompanyID, setting, epoch, c.UrlPrefix, resource, item, fields, relations, roles);
                                        if (success)
                                        {
                                            company.Execute("update integration.SynchedAssetType set LastSynchOn = @dt where ID = @id", new { id = item.ID, dt = DateTime.UtcNow });
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                            //log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                        }
                    });

                    CoreFunction.AITrackJobCompletedNoErrors(functionName);
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                //log.Error($"General Exception: {ex.GetFullExceptionData()}");
            }
            finally
            {
                if (available)
                    CoreFunction.UnlockWebJob(functionName);
            }

            CoreFunction.AIFlush();
        }

        #region Generic

        public static long ConvertDateToUnixTimeMilliseconds(DateTime? date = null)
        {
            long epoch = 0;

            if (!date.HasValue)
                date = DateTime.UtcNow;

            epoch = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;

            return epoch;
        }

        internal static T GetFromApi<T>(string uri, string authorization)
        {
            var cleanUri = new Uri(uri);
            if (cleanUri.Port != 80 && cleanUri.Port != 443)
            {
                uri = uri.Replace($":{cleanUri.Port}", "");
            }

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

        static async System.Threading.Tasks.Task<string> PostJsonToApiAsync(string uri, string authorization, string requestBody)
        {
            var jsonToReturn = "";

            using (var client = new HttpClient()) //WebClient()
            {
                client.Timeout = new TimeSpan(1, 0, 0);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);
                var response = await client.PostAsync(uri, new StringContent(requestBody));
                response.EnsureSuccessStatusCode();
                jsonToReturn = await response.Content.ReadAsStringAsync();
                //client.Headers.Set(HttpRequestHeader.Accept, "application/json");
                //client.Headers.Set(HttpRequestHeader.ContentType, "application/json");
                //client.Headers.Set(HttpRequestHeader.Authorization, authorization);
                //jsonToReturn = client.UploadString(uri, requestBody);
            }

            return jsonToReturn;
        }

        static string buildSearchUri(string type, List<string> properties)
        {
            var url = $"{SourceUri}search/?pageSize=75&types={type}";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }
            return url;
        }

        #endregion

        /// <summary>
        /// Synchronizes a specific type of asset from IGC with the customer's environment, based off a serieis of field, relationship, and ownership mappings.
        /// </summary>
        /// <param name="companyID">The ID of the customer's environment.</param>
        /// <param name="setting">The high-level setting that define the type of system we are connecting to and synchronizing.</param>
        /// <param name="lastSynchEpoch">The last time, in milliseconds (UNIX time), that the asset type was synchronized. This value is used to get only changes from IGC.</param>
        /// <param name="urlPrefix">the Govern environment prefix URL.</param>
        /// <param name="resource">The user account whose API key and secret we will use to post data into Govern.</param>
        /// <param name="mapping">The high-level asset-to-asset mapping.</param>
        /// <param name="fields">The asset field mappings.</param>
        /// <param name="relations">The asset relationship mappings.</param>
        /// <param name="roles">The asset role mappings.</param>
        /// <returns>An asynchronous boolean to indicate whether the process was successful or not.</returns>
        public static bool IGC_LoadAssetsByMappingTypeAsync(int companyID, IntegrationSetting setting, long? lastSynchEpoch, string urlPrefix, Resource resource, IntegrationAssetType mapping, List<IntegrationAssetTypeFieldItem> fields, List<IntegrationAssetTypeRelationItem> relations, List<IntegrationAssetTypeRoleItem> roles)
        {
            var success = true;

            var url = $"{setting.SourceUri}search/?pageSize=500&types={mapping.SourceAssetTypeName}";

            // Add the properties we are after for this IGC type.
            url += string.Concat(fields.Where(i => i.IncludeInPropertyRequest).Select(i => $"&properties={WebUtility.UrlEncode(i.SourceField)}"));
            url += string.Concat(relations.Where(i => i.IncludeInPropertyRequest).Select(i => $"&properties={WebUtility.UrlEncode(i.SourceField)}"));
            url += string.Concat(roles.Where(i => i.IncludeInPropertyRequest).Where(i => !string.IsNullOrEmpty(i.SourceIdField)).Select(i => $"&properties={WebUtility.UrlEncode(i.SourceIdField)}"));
            url += string.Concat(roles.Where(i => i.IncludeInPropertyRequest).Where(i => !string.IsNullOrEmpty(i.SourceNameField)).Select(i => $"&properties={WebUtility.UrlEncode(i.SourceNameField)}"));


            var igcData = new IgcDynamicArrayModels();
            var arr = new JArray();
            var relationships = new List<D3sRelationshipModel>();
            var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

            Func<JArray, JArray> parse = delegate (JArray root)
            {
                var fieldErrors = new Dictionary<string, string>();

                foreach (var obj in root.Children())
                {
                    var igcObjectSourceID = obj["_id"].Value<string>();

                    // Field Load Logic.
                    var d3s = new JObject();
                    fields.ForEach(f =>
                    {
                        if (f.ParentContextPosition.HasValue)
                        {
                            // There is a hierarchy here, and we need to resolve it.
                            var context = (obj[f.SourceField] as JArray); // obj[f.SourceField].Cast<List<GenericIgcContextModel>>().FirstOrDefault();
                            if (context != null)
                            {
                                d3s.Add(f.TargetField, context[f.ParentContextPosition.Value]["_id"].Value<string>());
                            }
                        }
                        else
                        {
                            if (f.IsArray)
                            {
                                d3s.Add(f.TargetField, (obj[f.SourceField] != null) ? string.Join(", ", obj[f.SourceField]) : "");
                            }
                            else
                            {
                                try
                                {
                                    d3s.Add(f.TargetField, obj[f.SourceField].Value<string>());

                                    // Set default value if empty and there is a default value to be used.
                                    if (!d3s[f.TargetField].HasValues && !string.IsNullOrEmpty(f.DefaultValue))
                                    {
                                        d3s[f.TargetField] = f.DefaultValue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (!fieldErrors.ContainsKey(f.TargetField))
                                    {
                                        fieldErrors.Add(f.TargetField, ex.GetFullExceptionData());
                                    }
                                    // Set default value.
                                    if (!string.IsNullOrEmpty(f.DefaultValue))
                                    {
                                        d3s[f.TargetField] = f.DefaultValue;
                                    }
                                }
                            }
                        }

                    });

                    // This is where we can inject an optional FusionID, or some other required identifier.
                    if (!string.IsNullOrEmpty(mapping.OptionalIDName) && mapping.OptionalID.HasValue)
                    {
                        d3s.Add(mapping.OptionalIDName, mapping.OptionalID.Value.ToString());
                    }

                    // Add object to collection.
                    arr.Add(d3s);



                    // Relation Load Logic.
                    relations.ForEach(r =>
                    {
                        try
                        {
                            var rm = obj[r.SourceField].ToObject<IgcRelationshipModel>();
                            var items = (
                                        from i in rm.items
                                        select i
                                        ).ToList();

                            relationships.AddRange(
                                items.Select(i => new D3sRelationshipModel
                                {
                                    SubjectSourceID = r.IsSubject ? igcObjectSourceID : i.SourceID,
                                    ObjectSourceID = r.IsSubject ? i.SourceID : igcObjectSourceID,
                                    PredicateType = r.PredicateType
                                })
                            );
                        }
                        catch (Exception)
                        {
                        }
                    });

                    // Role Load Logic.
                    roles.ForEach(r => {
                        var userFullName = "";
                        var userId = "";
                        if (!string.IsNullOrEmpty(r.SourceNameField))
                        {
                            if (obj[r.SourceNameField] != null)
                            {
                                userFullName = obj[r.SourceNameField].Value<string>();
                            }
                        }
                        if (!string.IsNullOrEmpty(r.SourceIdField))
                        {
                            if (obj[r.SourceIdField] != null)
                            {
                                userId = obj[r.SourceIdField].Value<string>();
                            }
                        }
                        ownershipTopModel.Items.Add(new D3sOwnershipModel {
                            RoleName = r.RoleName,
                            SourceID = igcObjectSourceID,
                            UserFullName = userFullName,
                            UserId = userId
                        });
                    });
                }

                if (fieldErrors.Keys.Count > 0)
                {
                    CoreFunction.AITrackEvent(functionName, "Parse Asset", fieldErrors, companyID);
                }

                return root;
            };

            var sourceAuthString = $"Basic {Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(setting.SourceUser + ":" + setting.SourcePassword))}";
            var targetAuthString = $"{resource.APIPublicKey};{resource.APIPrivateKey}";
            var targetBaseUri = "";

            if (!urlPrefix.Contains("-igx"))
            {
                if (urlPrefix.Contains(".preview")) urlPrefix = urlPrefix.Replace(".preview", "-igx.preview");
                else if (urlPrefix.Contains(".dev")) urlPrefix = urlPrefix.Replace(".dev", "-igx.dev");
                else if (urlPrefix.Contains(".uat")) urlPrefix = urlPrefix.Replace(".uat", "-igx.uat");
                else urlPrefix = urlPrefix + "-igx";
            }

            targetBaseUri = $"http://{urlPrefix}.data3sixty.local";
            //targetBaseUri = $"https://{urlPrefix}.data3sixty.com";
            targetBaseUri += $"/services/assets/";

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcDynamicArrayModels>(url, sourceAuthString);
                    if (models != null)
                    {
                        parse(models.items);
                        url = models.paging.next;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                    url = null;
                }
            }

            // If any items to send to server.

            if (arr.Count > 0)
            {
                try
                {
                    var respString = PostJsonToApiAsync(
                        $"{targetBaseUri}{mapping.Object}/{mapping.ObjectID}/bulk",
                        targetAuthString,
                        JsonConvert.SerializeObject(arr)
                    ).Result;

                    var assetResults = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(respString);
                    string assetErrorMessage = string.Empty;

                    assetResults.ForEach(r =>
                    {
                        if (!r.Success)
                        {
                            assetErrorMessage += $"{r.SourceID} : {r.Message}.";
                        }
                    });

                    if (!string.IsNullOrEmpty(assetErrorMessage))
                    {
                        CoreFunction.AITrackEvent(functionName, "Bulk Import Assets", new Dictionary<string, string>() { { "Error", assetErrorMessage } }, companyID);
                    }
                    
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                }
            }

            // If any owners to send to server.
            if (ownershipTopModel.Items.Count > 0)
            {
                var uniqueUsers = ownershipTopModel.Items
                    .Where(i => !string.IsNullOrEmpty(i.UserFullName) && !string.IsNullOrEmpty(i.UserId))
                    .Select(i => new { i.UserFullName, i.UserId })
                    .Distinct()
                    .ToList();

                // Populate the UserIDs that are missing, based can be looked up by user's full name. TODO: Confirm this logic, as it may not be correct if two or more user's have the same name.
                foreach (var item in ownershipTopModel.Items.Where(i => string.IsNullOrEmpty(i.UserId)))
                {
                    var match = uniqueUsers.FirstOrDefault(i => i.UserFullName == item.UserFullName);
                    if (match != null)
                    {
                        item.UserId = match.UserId;
                    }
                }

                //Now, remove any users whose internal ID cannot be resolved.
                ownershipTopModel.Items.RemoveAll(i => string.IsNullOrEmpty(i.UserId));

                try
                {
                    if (ownershipTopModel.Items.Count > 0)
                    {
                        var respString = PostJsonToApiAsync(
                            $"{targetBaseUri}ownership/bulk",
                            targetAuthString,
                            JsonConvert.SerializeObject(ownershipTopModel)
                        ).Result;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                }
            }

            if (relationships.Count > 0)
            {
                try
                {
                    var respString = PostJsonToApiAsync(
                        $"{targetBaseUri}relationships/bulk",
                        targetAuthString,
                        JsonConvert.SerializeObject(relationships)
                    ).Result;
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                }
            }

            return success;
        }

        public static void GetTypes()
        {
            var url = $"{SourceUri}types";

            var models = GetFromApi<dynamic>(url, SourceAuthString);
            //if (models != null)
            //{
            //    url = models.paging.next;
            //}

        }
    }
}
