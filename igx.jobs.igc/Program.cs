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
        const string timerSettings = "0 */5 * * * *";
        //const string timerSettings = "*/5 * * * * *";

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            var available = false;
            try
            {
                available = true;// CoreFunction.LockWebJobIfAvailable(functionName);

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
                            List<IntegrationAssetTypeRelationItemTarget> mappingRelationTargets = null;
                            List<IntegrationAssetTypeRoleItem> mappingRoles = null;

                            // Do this call in here so we do not incur the cost of four DB calls for every database unless we absolutely have to.
                            if (settings.Count > 0)
                            {
                                mappings = company.Query<IntegrationAssetType>("select * from integration.SynchedAssetType where Active = 1").ToList(); // where ID = 20").ToList();
                                mappingFields = company.Query<IntegrationAssetTypeFieldItem>("select * from integration.SynchedAssetTypeFieldItem where Active = 1").ToList();
                                mappingRelations = company.Query<IntegrationAssetTypeRelationItem>("select * from integration.SynchedAssetTypeRelationItem").ToList();
                                mappingRelationTargets = company.Query<IntegrationAssetTypeRelationItemTarget>("select * from integration.SynchedAssetTypeRelationItemTarget").ToList();
                                mappingRoles = company.Query<IntegrationAssetTypeRoleItem>("select * from integration.SynchedAssetTypeRoleItem").ToList();
                            }

                            // Close the connection for now.
                            company.Close();

                            foreach (var setting in settings)
                            {
                                #region Get the resource for this setting.

                                var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
                                cnn.OpenWithRetry(RetryPolicy.DefaultFixed);
                                var resource = cnn.Query<Resource>("select * from Resource where ID = @id", new { id = setting.TargetResourceID }).SingleOrDefault();
                                cnn.Close();
                                cnn.Dispose();

                                #endregion

                                bool checkForChangesOnly = true; // TRUE = POST to IGC API
                                var now = DateTime.UtcNow;
                                if (setting.LastRefreshOn.HasValue)
                                {
                                    checkForChangesOnly = (setting.LastRefreshOn.Value > DateTime.UtcNow.AddHours(-setting.RefreshInterval));
                                }
                                else
                                {
                                    checkForChangesOnly = false;
                                }


                                if (setting.IntegrationSystem == d360.core.enums.IntegrationSystem.IGC)
                                {
                                    foreach (var item in mappings.Where(i => i.Active && i.ToGovern && i.IntegrationSettingID == setting.ID)) // && i.ID == 20
                                    {
                                        if (checkForChangesOnly)
                                        {
                                            checkForChangesOnly = item.AllowChangeDetection; //Final check to see if we even allow for DELTA checking on this asset type.
                                        }

                                        var fields = mappingFields.Where(i => i.SynchedAssetTypeID == item.ID).ToList();
                                        var relations = mappingRelations.Where(i => i.SynchedAssetTypeID == item.ID).ToList();
                                        var relationIDs = relations.Select(i => i.ID).ToList();
                                        var relationTargets = mappingRelationTargets.Where(i => relationIDs.Contains(i.SynchedAssetTypeRelationItemID)).ToList();
                                        var roles = mappingRoles.Where(i => i.SynchedAssetTypeID == item.ID).ToList();

                                        var success = false;
                                        //DateTime? lastDateCheckedSuccessfully = null;
                                        success = IGC_LoadAssetsByMappingType(c.CompanyID, setting, checkForChangesOnly, now, c.UrlPrefix, resource, item, fields, relations, relationTargets, roles, company);
                                        if (success)
                                        {
                                            company.OpenWithRetry(RetryPolicy.DefaultFixed);
                                            company.Execute("update integration.SynchedAssetType set LastSynchOn = @dt, LastSuccessfulCount = @cnt where ID = @id", new { id = item.ID, dt = item .LastSynchOn, cnt = item.LastSuccessfulCount });
                                            company.Close();
                                        }
                                    }

                                    if (!checkForChangesOnly)
                                    {
                                        //Update the RefreshedOn property on the setting record, as we just did a full refresh.
                                        company.OpenWithRetry(RetryPolicy.DefaultFixed);
                                        company.Execute("update integration.Setting set LastRefreshOn = @dt where ID = @id", new { id = setting.ID, dt = now });
                                        company.Close();
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

            epoch = (long)(date.Value.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;

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

        static async Task<T> PostJsonToApiAsync<T>(string uri, string authorization, string requestBody)
        {
            var jsonToReturn = "";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = new TimeSpan(1, 0, 0);
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    //client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);
                    var response = await client.PostAsync(uri, new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json"));
                    response.EnsureSuccessStatusCode();
                    jsonToReturn = await response.Content.ReadAsStringAsync();
                }

                return JsonConvert.DeserializeObject<T>(jsonToReturn, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string>();
                properties.Add("Uri", uri);
                properties.Add("Request Body", requestBody);
                CoreFunction.AITrackException(functionName, ex, null, properties);
                throw ex;
            }
        }

        static async Task<string> PostJsonToApiAsync(string uri, string authorization, string requestBody)
        {
            var jsonToReturn = "";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = new TimeSpan(1, 0, 0);
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);
                    var response = await client.PostAsync(uri, new StringContent(requestBody));
                    response.EnsureSuccessStatusCode();
                    jsonToReturn = await response.Content.ReadAsStringAsync();
                }

                return jsonToReturn;
            }

            catch (Exception ex)
            {
                var properties = new Dictionary<string, string>();
                properties.Add("Uri", uri);
                properties.Add("Request Body", requestBody);
                CoreFunction.AITrackException(functionName, ex, null, properties);
                throw ex;
            }
        }

        #endregion

        /// <summary>
        /// Synchronizes a specific type of asset from IGC with the customer's environment, based off a serieis of field, relationship, and ownership mappings.
        /// </summary>
        /// <param name="companyID">The ID of the customer's environment.</param>
        /// <param name="setting">The high-level setting that define the type of system we are connecting to and synchronizing.</param>
        /// <param name="checkForChangesOnly">Determine if this is a DELTA check, checking for changes only, or if this is a full refresh of the content.</param>
        /// <param name="now">The current date in UTC.</param>
        /// <param name="urlPrefix">the Govern environment prefix URL.</param>
        /// <param name="resource">The user account whose API key and secret we will use to post data into Govern.</param>
        /// <param name="mapping">The high-level asset-to-asset mapping.</param>
        /// <param name="fields">The asset field mappings.</param>
        /// <param name="relations">The asset relationship mappings.</param>
        /// <param name="roles">The asset role mappings.</param>
        /// <returns>An asynchronous boolean to indicate whether the process was successful or not.</returns>
        public static bool IGC_LoadAssetsByMappingType(int companyID, IntegrationSetting setting, bool checkForChangesOnly, DateTime now, string urlPrefix, Resource resource, 
            IntegrationAssetType mapping, 
            List<IntegrationAssetTypeFieldItem> fields, 
            List<IntegrationAssetTypeRelationItem> relations,
            List<IntegrationAssetTypeRelationItemTarget> relationTargets,
            List<IntegrationAssetTypeRoleItem> roles, 
            SqlConnection company)//out DateTime? lastDateChecked)
        {
            //lastDateChecked = now;

            var success = true;

            var igcData = new IgcDynamicArrayModels();
            var arr = new JArray();
            var relationships = new List<D3sRelationshipModel>();
            var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

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

            //targetBaseUri = $"http://{urlPrefix}.data3sixty.local";
            targetBaseUri = $"https://{urlPrefix}.data3sixty.com";
            targetBaseUri += $"/services/assets/";

            DateTime? currentParsedUnvalidatedDate = null;

            var enumFields = new List<string>();
            var enumValues = new List<EnumResolutionModel>();

            Func<JArray, JArray> parse = delegate (JArray root)
            {
                var fieldErrors = new Dictionary<string, string>();

                try
                {
                    if (root.Count > 0)
                    {
                        currentParsedUnvalidatedDate = root[root.Count - 1]["modified_on"].Value<DateTime?>();
                        if (!currentParsedUnvalidatedDate.HasValue)
                        {
                            currentParsedUnvalidatedDate = root[root.Count - 1]["created_on"].Value<DateTime?>();
                        }
                    }
                }
                catch
                {
                }

                foreach (var obj in root.Children())
                {
                    try
                    {
                        var igcObjectSourceID = obj["_id"].Value<string>();

                        // Field Load Logic.
                        var targetObject = new JObject();
                        fields.ForEach(f =>
                        {
                            if (f.ParentContextPosition.HasValue)
                            {
                                // There is a hierarchy here, and we need to resolve it.
                                try
                                {
                                    var context = (obj[f.SourceField] as JArray); // obj[f.SourceField].Cast<List<GenericIgcContextModel>>().FirstOrDefault();
                                    if (context != null)
                                    {
                                        if (targetObject.Property(f.TargetField) == null)
                                        {
                                            if (f.ParentContextPosition.Value == 99)
                                            {
                                                targetObject.Add(f.TargetField, context.Last["_id"].Value<string>());
                                            }
                                            else
                                            {
                                                targetObject.Add(f.TargetField, context[f.ParentContextPosition.Value]["_id"].Value<string>());
                                            }
                                        }

                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (!fieldErrors.ContainsKey($"{f.SourceField}"))
                                    {
                                        fieldErrors.Add($"{f.SourceField}", ex.GetFullExceptionData());
                                    }
                                }
                            }
                            else
                            {
                                if (f.IsArray)
                                {
                                    // If there is not already a target field with this name that is populated.
                                    if (targetObject.Property(f.TargetField) == null)
                                    {
                                        if (!string.IsNullOrEmpty(f.ArrayValueDelimiter) && !string.IsNullOrEmpty(f.ArrayValueFieldName))
                                        {
                                            try
                                            {
                                                // Concatenate a particular field from the array and delimit each string value into one consolidated string. For example, a path.
                                                var delimitedFieldArray = obj[f.SourceField] as JArray;
                                                var delimitedCollection = new List<string>();
                                                delimitedCollection.AddRange(
                                                    delimitedFieldArray.Select(i => i[f.ArrayValueFieldName].Value<string>())
                                                );

                                                targetObject.Add(f.TargetField, string.Join(f.ArrayValueDelimiter, delimitedCollection));
                                            }
                                            catch (Exception ex)
                                            {
                                                targetObject.Add(f.TargetField, $"ERROR: {ex.Message}");
                                            }
                                        }
                                        else
                                        {
                                            if (enumFields.Contains(f.SourceField))
                                            {
                                                try
                                                {
                                                    // If this field is an enumeration, then resolve to the underlying display values.
                                                    var codes = obj[f.SourceField].Values<string>().ToList();

                                                    var displayValues = enumValues
                                                    .Where(i =>
                                                        i.PropertyName == f.SourceField &&
                                                        codes.Contains(i.Code)
                                                    )
                                                    .Select(i => i.DisplayValue)
                                                    .ToList();
                                                    targetObject.Add(
                                                        f.TargetField,
                                                        (obj[f.SourceField] != null) ? string.Join(", ", displayValues) : "");
                                                }
                                                catch (Exception ex)
                                                {
                                                    targetObject.Add(f.TargetField, $"ERROR: {ex.Message}");
                                                }
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    // Treat this is a straight value (non-enumeration).
                                                    targetObject.Add(f.TargetField, (obj[f.SourceField] != null) ? string.Join(", ", obj[f.SourceField]) : "");
                                                }
                                                catch (Exception ex)
                                                {
                                                    targetObject.Add(f.TargetField, $"ERROR: {ex.Message}");
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        if (targetObject.Property(f.TargetField) == null)
                                        {
                                            if (enumFields.Contains(f.SourceField))
                                            {
                                                var displayValue = enumValues.Where(i => i.PropertyName == f.SourceField && i.Code == obj[f.SourceField].Value<string>()).FirstOrDefault();
                                                if (displayValue != null)
                                                {
                                                    targetObject.Add(f.TargetField, displayValue.DisplayValue);
                                                }
                                                else
                                                {
                                                    targetObject.Add(f.TargetField, obj[f.SourceField].Value<string>());
                                                }
                                            }
                                            else
                                            {
                                                targetObject.Add(f.TargetField, obj[f.SourceField].Value<string>());
                                            }
                                            
                                        }

                                        // Set default value if empty and there is a default value to be used.
                                        if (!targetObject[f.TargetField].HasValues && !string.IsNullOrEmpty(f.DefaultValue))
                                        {
                                            targetObject[f.TargetField] = f.DefaultValue;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        if (!fieldErrors.ContainsKey($"{f.SourceField}"))
                                        {
                                            fieldErrors.Add($"{f.SourceField}", ex.GetFullExceptionData());
                                        }
                                        // Set default value.
                                        if (!string.IsNullOrEmpty(f.DefaultValue))
                                        {
                                            targetObject[f.TargetField] = f.DefaultValue;
                                        }
                                    }
                                }
                            }
                        });

                        // This is where we can inject an optional FusionID, or some other required identifier.
                        if (!string.IsNullOrEmpty(mapping.OptionalIDName) && mapping.OptionalID.HasValue)
                        {
                            targetObject.Add(mapping.OptionalIDName, mapping.OptionalID.Value.ToString());
                        }

                        // Add object to collection.
                        arr.Add(targetObject);

                        // Relation Load Logic.
                        relations.ForEach(r =>
                        {
                            try
                            {
                                var rm = obj[r.SourceField].ToObject<IgcRelationshipModel>();
                                if (rm.items == null)
                                {
                                    rm.items = new List<IgcModel>();
                                    rm.items.Add(obj[r.SourceField].ToObject<IgcModel>());
                                }

                                if (rm.items != null)
                                {
                                    var items = (
                                                from i in rm.items
                                                select i
                                                ).ToList();

                                    if (items.Count > 0)
                                    {
                                        var targets = relationTargets.Where(i => i.SynchedAssetTypeRelationItemID == r.ID).ToList();
                                        if (targets.Count == 1)
                                        {
                                            // If there is ONLY 1 target (most cases), then there is no need to loop through 
                                            // all the related items to find the appropriate target. 
                                            // Treat them all as the same target.
                                            var target = targets.First();
                                            if (items[0].Type.StartsWith(target.SourceAssetType))
                                            {
                                                relationships.AddRange(
                                                    items.Select(i => new D3sRelationshipModel
                                                    {
                                                        SubjectSourceID = r.IsSubject ? igcObjectSourceID : i.SourceID,
                                                        ObjectSourceID = r.IsSubject ? i.SourceID : igcObjectSourceID,
                                                        PredicateType = r.PredicateType,
                                                        IntersectTypeID = target.IntersectTypeID
                                                    })
                                                );
                                            }
                                        }
                                        else if (targets.Count > 1)
                                        {
                                            // If more than one target, loop through each related item to uncover its appropriate target.
                                            items.ForEach(ri =>
                                            {
                                                var target = targets.FirstOrDefault(i => ri.Type.StartsWith(i.SourceAssetType));
                                                if (target != null)
                                                {
                                                    relationships.Add(new D3sRelationshipModel
                                                    {
                                                        SubjectSourceID = r.IsSubject ? igcObjectSourceID : ri.SourceID,
                                                        ObjectSourceID = r.IsSubject ? ri.SourceID : igcObjectSourceID,
                                                        PredicateType = r.PredicateType,
                                                        IntersectTypeID = target.IntersectTypeID
                                                    });
                                                }
                                            });
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
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
                    catch (Exception ex)
                    {
                        if (!fieldErrors.ContainsKey("ParseError"))
                        {
                            fieldErrors.Add("ParseError", ex.GetFullExceptionData());
                        }
                    }
                }

                if (fieldErrors.Keys.Count > 0)
                {
                    CoreFunction.AITrackEvent(functionName, $"{mapping.SourceAssetTypeName}, Parse Asset", fieldErrors, companyID);
                }

                return root;
            };

            //First, get the type definition of the asset type, to pull enum values.
            var url = $"{setting.SourceUri}types/{mapping.SourceAssetTypeName}?showEditProperties=true";

            try
            {
                var igcType = GetFromApi<IgcTypeModel>(url, sourceAuthString);

                if (igcType != null)
                {
                    igcType.EditInfo.Properties.ForEach(p => {
                        if (p.Type.Name == "enum")
                        {
                            if (p.Type.Values != null)
                            {
                                enumFields.Add(p.Name);

                                enumValues.AddRange(p.Type.Values.Select(i => new EnumResolutionModel
                                {
                                    PropertyName = p.Name,
                                    Code = i.Code,
                                    DisplayValue = i.DisplayName
                                }));
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }

            url = $"{setting.SourceUri}search/";

            //checkForChangesOnly = true;

            if (checkForChangesOnly)
            {
                //Perform search using POST method.
                var postModel = new IgcPostSearchRequestModel {
                    sorts = new List<IgcPostSearchRequestSortModel>() {
                        new IgcPostSearchRequestSortModel { ascending = true, property = "modified_on" },
                        new IgcPostSearchRequestSortModel { ascending = true, property = "created_on" }
                    }
                };
                postModel.begin = 0;
                postModel.pageSize = 250;

                postModel.types.Add(mapping.SourceAssetTypeName);

                postModel.properties.AddRange(fields.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceField)).Select(i => i.SourceField));
                postModel.properties.AddRange(relations.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceField)).Select(i => i.SourceField));
                postModel.properties.AddRange(roles.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceIdField)).Select(i => i.SourceIdField));
                postModel.properties.AddRange(roles.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceNameField)).Select(i => i.SourceNameField));

                if (!postModel.properties.Contains("created_on")) postModel.properties.Add("created_on");
                if (!postModel.properties.Contains("modified_on")) postModel.properties.Add("modified_on");

                var min = ConvertDateToUnixTimeMilliseconds(mapping.LastSynchOn ?? new DateTime(1970, 1, 1, 0, 0, 0));
                var max = ConvertDateToUnixTimeMilliseconds(now);
                
                postModel.where.conditions.Add(new IgcPostSearchRequestBetweenConditionModel { min = min, max = max, property = "created_on" });
                postModel.where.conditions.Add(new IgcPostSearchRequestBetweenConditionModel { min = min, max = max, property = "modified_on" });

                if (mapping.LastSuccessfulCount.HasValue)
                {
                    postModel.begin = mapping.LastSuccessfulCount.Value + 1;
                }

                var shouldContinue = true;
                while (shouldContinue)
                {
                    try
                    {
                        var models = PostJsonToApiAsync<IgcDynamicArrayModels>(url, sourceAuthString, JsonConvert.SerializeObject(postModel)).Result;
                        if (models != null)
                        {
                            parse(models.items);
                            shouldContinue = (models.paging.numTotal > models.paging.end + 1);
                            postModel.begin = models.paging.end + 1;

                            if (arr.Count > 4999)
                            {
                                if (SendIncrementalSetToGovern(companyID, mapping, arr, relationships, ownershipTopModel, targetBaseUri, targetAuthString))
                                {
                                    mapping.LastSynchOn = currentParsedUnvalidatedDate;
                                    mapping.LastSuccessfulCount += arr.Count; //This line must be called before the array is re-initialized.
                                    try
                                    {
                                        company.OpenWithRetry(RetryPolicy.DefaultFixed);
                                        company.Execute("update integration.SynchedAssetType set LastSynchOn = null, LastSuccessfulCount = @cnt where ID = @id", new { id = mapping.ID, cnt = mapping.LastSuccessfulCount });
                                        company.Close();
                                    }
                                    catch (Exception ex)
                                    {
                                        CoreFunction.AITrackException(functionName, ex);
                                    }
                                }

                                // Re-initialize.
                                arr = new JArray();
                                relationships = new List<D3sRelationshipModel>();
                                ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex);
                        shouldContinue = false;
                        success = false;
                    }
                }
            }
            else
            {
                //Perform search using GET method.

                // Add the properties we are after for this IGC type.
                url += $"?pageSize=250&types={mapping.SourceAssetTypeName}";
                url += string.Concat(fields.Where(i => i.IncludeInPropertyRequest).Select(i => $"&properties={WebUtility.UrlEncode(i.SourceField)}"));
                url += string.Concat(relations.Where(i => i.IncludeInPropertyRequest).Select(i => $"&properties={WebUtility.UrlEncode(i.SourceField)}"));
                url += string.Concat(roles.Where(i => i.IncludeInPropertyRequest).Where(i => !string.IsNullOrEmpty(i.SourceIdField)).Select(i => $"&properties={WebUtility.UrlEncode(i.SourceIdField)}"));
                url += string.Concat(roles.Where(i => i.IncludeInPropertyRequest).Where(i => !string.IsNullOrEmpty(i.SourceNameField)).Select(i => $"&properties={WebUtility.UrlEncode(i.SourceNameField)}"));

                if (mapping.LastSuccessfulCount.HasValue)
                {
                    url += $"&begin={mapping.LastSuccessfulCount.Value + 1}";
                }

                while (!string.IsNullOrEmpty(url))
                {
                    try
                    {
                        var models = GetFromApi<IgcDynamicArrayModels>(url, sourceAuthString);
                        if (models != null)
                        {
                            parse(models.items);
                            url = models.paging.next;

                            if (arr.Count > 4999)
                            {
                                SendIncrementalSetToGovern(companyID, mapping, arr, relationships, ownershipTopModel, targetBaseUri, targetAuthString);
                                mapping.LastSuccessfulCount += arr.Count;//This line must be called before the array is re-initialized.

                                try
                                {
                                    company.OpenWithRetry(RetryPolicy.DefaultFixed);
                                    company.Execute("update integration.SynchedAssetType set LastSynchOn = null, LastSuccessfulCount = @cnt where ID = @id", new { id = mapping.ID, cnt = mapping.LastSuccessfulCount });
                                    company.Close();
                                }
                                catch (Exception ex)
                                {
                                    CoreFunction.AITrackException(functionName, ex);
                                }

                                // Re-initialize.
                                arr = new JArray();
                                relationships = new List<D3sRelationshipModel>();
                                ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex);
                        url = null;
                        success = false;
                    }
                }
            }

            if (SendIncrementalSetToGovern(companyID, mapping, arr, relationships, ownershipTopModel, targetBaseUri, targetAuthString))
            {
                //lastDateChecked = currentParsedUnvalidatedDate;
                mapping.LastSuccessfulCount = null;
                mapping.LastSynchOn = currentParsedUnvalidatedDate ?? now;
            }

            //if (!lastDateChecked.HasValue)
            //    lastDateChecked = now;

            return success;
        }

        public static bool SendIncrementalSetToGovern(int companyID, IntegrationAssetType mapping, JArray arr, List<D3sRelationshipModel> relationships, D3sOwnershipItemsModel ownershipTopModel, string targetBaseUri, string targetAuthString)
        {
            bool successfulPost = true;
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
                    successfulPost = false;
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

            return successfulPost;
        }
        
    }
}
