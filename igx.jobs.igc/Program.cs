using d360.core;
using d360.core.entities;
using d360.core.enums.Workflow;
using d360.core.queue;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.model;
using igx.jobs;
using igx.jobs.igc;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
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
#if DEBUG
        const string timerSettings = "*/5 * * * * *";
#else
        const string timerSettings = "0 */5 * * * *";
#endif

        private static HttpClient _client = null;
        public static HttpClient Client
        {
            get
            {
                if (_client == null)
                {
                    var handler = new HttpClientHandler { UseCookies = false };
                    _client = new HttpClient(handler,false);
                    _client.Timeout = new TimeSpan(1, 0, 0);
                    _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                }

                return _client;
            }
        }

        public static string AuthenticationHeaderValue { get; set; } = null;

        public static List<string> SessionCookies { get; set; } = null;

        public static string LogoutUri { get; set; } = null;

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var Caching = new DummyCachingProvider();
                var Queue = new AzureQueueSource();
                var Security = new UriSecurityContextProvider { IsAdministrator = true, ResourceID = 0 };
                var Community = new CommunityContext(Caching, Queue, Security);
#if DEBUG
                var companies = CoreFunction.GetCompaniesByCurrentSlot().Where(i => i.CompanyID == 126).ToList();
#else
                var companies = CoreFunction.GetCompaniesByCurrentSlot();
#endif

                companies.ForEach(async c =>
                {
                    try
                    {
                        Community.CurrentCompanyID = c.CompanyID;
                        Community.CurrentCompanyDomain = c.UrlPrefix;
                        Security.CompanyID = c.CompanyID;
                        Security.CompanyPrefix = c.UrlPrefix;
                        var company = new CompanyContext(Community, Caching, Queue, Security, true);

                        var settings = company.Table<IntegrationSetting>().ToList();

                        List<IntegrationAssetType> mappings = null;
                        List<IntegrationAssetTypeFieldItem> mappingFields = null;
                        List<IntegrationAssetTypeRelationItem> mappingRelations = null;
                        List<IntegrationAssetTypeRelationItemTarget> mappingRelationTargets = null;
                        List<IntegrationAssetTypeRoleItem> mappingRoles = null;

                        // Do this call in here so we do not incur the cost of four DB calls for every database unless we absolutely have to.
                        if (settings.Count > 0)
                        {
                            #if DEBUG
                                                            var IDs = new List<int>() { 1 };
                                                            mappings = company.Filter<IntegrationAssetType>(i => i.Active && IDs.Contains(i.ID)).ToList(); // testing only.
                            #else
                            mappings = company.Filter<IntegrationAssetType>(i => i.Active).ToList();
                            #endif
                            mappingFields = company.Filter<IntegrationAssetTypeFieldItem>(i => i.Active).ToList();
                            mappingRelations = company.Table<IntegrationAssetTypeRelationItem>().ToList();
                            mappingRelationTargets = company.Table<IntegrationAssetTypeRelationItemTarget>().ToList();
                            mappingRoles = company.Table<IntegrationAssetTypeRoleItem>().ToList();
                        }

                        foreach (var setting in settings)
                        {
                            AuthenticationHeaderValue = $"Basic {Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(setting.SourceUser + ":" + setting.SourcePassword))}";

                            var now = DateTime.UtcNow;
                            var delayStartUntil = setting.DelayStartUntil ?? now;

                            if (now >= delayStartUntil)
                            {
                                if (mappings.Any(i => i.Active && i.ToGovern && i.IntegrationSettingID == setting.ID && i.ObjectID.HasValue))
                                {
                                    var failedFullRefreshExecutions = company.Query<FailedExecutionModel>(@"
select		max(T.ExecutionID) as ExecutionID,
		count(1) as [CurrentCount],
		T.SynchedAssetTypeID
from		integration.ExecutionAssetType T
		left join integration.ExecutionAsset A on A.ExecutionID = T.ExecutionID and A.SynchedAssetTypeID = T.SynchedAssetTypeID
where		T.CompletedOn is null
		and T.IsFullRefresh = 1
group by	T.SynchedAssetTypeID
order by	T.SynchedAssetTypeID").ToList();

                                    var lastFullRefreshModels = company.Query<LastFullRefreshModel>(@"
select	A.ID,
	coalesce(max(E.StartedOn), '1/1/1970') as StartedOn
from	[integration].[SynchedAssetType] A
	left join [integration].[ExecutionAssetType] E on E.SynchedAssetTypeID = A.ID and A.IntegrationSettingID = @s and A.Active = 1 and E.IsFullRefresh = 1 and E.CompletedOn is not null
group by A.ID
order by A.ID
", new { s = setting.ID }).ToList();

                                    IntegrationExecution newExecution = null;
                                    bool shouldContinue = true;

                                    if (shouldContinue)
                                    {
                                        foreach (var item in mappings.Where(i => i.Active && i.ToGovern && i.IntegrationSettingID == setting.ID && i.ObjectID.HasValue))
                                        {
                                            if (!shouldContinue)
                                                break;

                                            var failedFullRefreshExecution = failedFullRefreshExecutions.SingleOrDefault(f => f.SynchedAssetTypeID == item.ID);
                                            IntegrationExecutionAssetType newAtExecution = null;
                                            IntegrationExecutionAssetType previousAtExecution = null;
                                            int currentCount = 0;

                                            if (failedFullRefreshExecution == null)
                                            {
                                                if (newExecution == null)
                                                {
                                                    // Initialize the new execution record on the first pass where we actually need a new one.
                                                    newExecution = new IntegrationExecution { StartedOn = now };
                                                    company.Add(newExecution);
                                                }

                                                newAtExecution = new IntegrationExecutionAssetType { StartedOn = now, SynchedAssetTypeID = item.ID, ExecutionID = newExecution.ID };
                                                company.Add(newAtExecution);
                                            }
                                            else
                                            {
                                                currentCount = failedFullRefreshExecution.CurrentCount;
                                                previousAtExecution = company.Filter<IntegrationExecutionAssetType>(f =>
                                                        f.ExecutionID == failedFullRefreshExecution.ExecutionID &&
                                                        f.SynchedAssetTypeID == failedFullRefreshExecution.SynchedAssetTypeID
                                                    ).Single();
                                                now = previousAtExecution.StartedOn; //Start off from the last marked date.
                                            }

                                            #region Figure out if we should perform a DELTA or a FULL REFRESH

                                            var performDelta = true;

                                            if (item.AllowChangeDetection)
                                            {
                                                // Continue to check if we should perform delta.

                                                if (currentCount > 0)
                                                {
                                                    // If > 0, that means that the last full refresh did not successfully complete. 
                                                    // Do not check for changes, do a full refresh again, starting where you left off.
                                                    performDelta = false;
                                                }
                                                else
                                                {
                                                    // Get the start date of the last full refresh.
                                                    var lastFullRefreshModel = lastFullRefreshModels.Single(i => i.ID == item.ID);

                                                    var refreshInterval = (item.RefreshIntervalOverride.HasValue) ?
                                                        item.RefreshIntervalOverride.Value :
                                                        setting.RefreshInterval;

                                                    // If last refresh+interval > current time, then perform a delta instead, as you have not surpassed the refresh interval.
                                                    performDelta = (lastFullRefreshModel.StartedOn.AddHours(refreshInterval) > DateTime.UtcNow);
                                                }
                                            }
                                            else
                                            {
                                                // Delta checking not even allowed.
                                                performDelta = false;
                                            }

                                            #endregion

                                            var fields = mappingFields.Where(i => i.SynchedAssetTypeID == item.ID).ToList();
                                            var relations = mappingRelations.Where(i => i.SynchedAssetTypeID == item.ID).ToList();
                                            var relationIDs = relations.Select(i => i.ID).ToList();
                                            var relationTargets = mappingRelationTargets.Where(i => relationIDs.Contains(i.SynchedAssetTypeRelationItemID)).ToList();
                                            var roles = mappingRoles.Where(i => i.SynchedAssetTypeID == item.ID).ToList();

                                            var success = false;

                                            switch (setting.IntegrationSystem)
                                            {
                                                case d360.core.enums.IntegrationSystem.IGC:
                                                    LogoutUri = $"{setting.SourceUri}logout/";

                                                    log.WriteLine($"Begin processing IGC asset {item.SourceAssetTypeName}...");
                                                    try
                                                    {
                                                        success = IGC_LoadAssetsByMappingType(
                                                            setting,
                                                            performDelta,
                                                            now,
                                                            previousAtExecution ?? newAtExecution,
                                                            item,
                                                            fields,
                                                            relations,
                                                            relationTargets,
                                                            roles,
                                                            company,
                                                            c,
                                                            currentCount);
                                                    }
                                                    catch (HttpRequestException rex)
                                                    {
                                                        shouldContinue = false;
                                                        success = false;
                                                        log.WriteLine($"Failed processing: {rex.GetFullExceptionData()}...");
                                                        if (rex.Message.Contains("403 (Forbidden)"))
                                                        {
                                                            setting.DelayStartUntil = DateTime.UtcNow.AddMinutes(31);
                                                            company.Update(setting);
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        shouldContinue = false;
                                                        success = false;
                                                        log.WriteLine($"Failed processing: {ex.GetFullExceptionData()}...");
                                                    }

                                                    if (success)
                                                        log.WriteLine($"Complete processing IGC asset {item.SourceAssetTypeName}...");
                                                    else
                                                        log.WriteLine($"Failed on processing IGC asset {item.SourceAssetTypeName}...");
                                                    break;
                                            }

                                            #region Mark execution asset type record as complete or not.

                                            if (success)
                                            {
                                                if (newAtExecution != null)
                                                {
                                                    var newExecutionAssetCount = company.Count<IntegrationExecutionAsset>(i => i.ExecutionID == newAtExecution.ExecutionID && i.SynchedAssetTypeID == newAtExecution.SynchedAssetTypeID);
                                                    if (newExecutionAssetCount == newAtExecution.CurrentSourceAssetCount)
                                                    {
                                                        newAtExecution.CompletedOn = DateTime.UtcNow;
                                                        company.Update(newAtExecution);
                                                    }
                                                }
                                                else
                                                {
                                                    if (previousAtExecution != null)
                                                    {
                                                        var previousExecutionAssetCount = company.Count<IntegrationExecutionAsset>(i => i.ExecutionID == previousAtExecution.ExecutionID && i.SynchedAssetTypeID == previousAtExecution.SynchedAssetTypeID);
                                                        if (previousExecutionAssetCount == previousAtExecution.CurrentSourceAssetCount)
                                                        {
                                                            previousAtExecution.CompletedOn = DateTime.UtcNow;
                                                            company.Update(previousAtExecution);
                                                        }
                                                    }
                                                }
                                                company.Update(item);
                                            }

                                            #endregion
                                        }

                                        if (newExecution != null)
                                        {
                                            log.WriteLine($"Completing execution ID {newExecution.ID}.");
                                            newExecution.CompletedOn = DateTime.UtcNow;
                                            company.Update(newExecution);
                                        }

                                        #region Process unresolved relationships and process workflows around them

                                        try
                                        {
                                            log.WriteLine($"Begin processing unresolved relationships.");
                                            var unResolvedRelationResults = company.ProcessUnresolvedRelationships();
                                            if (unResolvedRelationResults != null)
                                            {
                                                var events = new List<EventInfo>();
                                                foreach (var r in unResolvedRelationResults)
                                                {
                                                    if (r.Action == "INSERT" && r.IntersectID != null)
                                                    {
                                                        events.Add(new EventInfo
                                                        {
                                                            CompanyID = c.CompanyID,
                                                            DomainPrefix = c.UrlPrefix,
                                                            ResourceID = setting.TargetResourceID,
                                                            Action = ChangeType.Add,
                                                            Object = new EventObjectInfo
                                                            {
                                                                Object = SystemObjects.Intersect,
                                                                ObjectType = SystemObjects.IntersectType,
                                                                ObjectID = r.IntersectID,
                                                                ObjectTypeID = r.IntersectTypeID
                                                            }
                                                        });
                                                        if (events.Count > 50)
                                                        {
                                                            Queue.CreateTopicMessages(events);
                                                            events.Clear();
                                                        }
                                                    }
                                                }

                                                if (events.Count > 0)
                                                {
                                                    Queue.CreateTopicMessages(events);
                                                }
                                            }
                                            log.WriteLine($"End processing unresolved relationships.");
                                        }
                                        catch (Exception ex)
                                        {
                                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                                        }

                                        #endregion
                                    }
                                }

                            }
                        }

                        #region Process deletes

                        if (settings.Count > 0)
                        {
                            try
                            {
                                log.WriteLine($"Begin processing deletions.");
                                company.ProcessIntegrationAssetDeletions();
                                log.WriteLine($"End processing deletions.");
                            }
                            catch (Exception ex)
                            {
                                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                            }
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.WriteLine($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                    }
                });

            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.WriteLine($"General Exception: {ex.GetFullExceptionData()}");
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

        internal async static Task<List<string>> GetCookiesFromApi(string uri, string authorization)
        {
            var cleanUri = new Uri(uri);
            if (cleanUri.Port != 80 && cleanUri.Port != 443)
            {
                uri = uri.Replace($":{cleanUri.Port}", "");
            }

            List<string> cookies = null;

            using (var client = new HttpClient())
            {
                client.Timeout = new TimeSpan(0, 10, 0);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Remove("Authorization");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);
                var response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                cookies = (from c in response.Headers.Where(c => c.Key == "Set-Cookie")
                            from cv in c.Value
                            select cv
                            )
                            .ToList();

                response.Dispose();
                client.Dispose();
            }

            return cookies;
        }

        internal async static Task<T> GetFromApi<T>(string uri)
        {
            var cleanUri = new Uri(uri);
            if (cleanUri.Port != 80 && cleanUri.Port != 443)
            {
                uri = uri.Replace($":{cleanUri.Port}", "");
            }

            var jsonRaw = "";

            List<string> cookies = null;

            try
            {
                Client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AuthenticationHeaderValue);

                var response = await Client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                jsonRaw = await response.Content.ReadAsStringAsync();

                cookies = (from c in response.Headers.Where(c => c.Key == "Set-Cookie")
                           from cv in c.Value
                           select cv
                ).ToList();
                Client.DefaultRequestHeaders.Remove("Authorization");
                Client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookies);

                response.Dispose();
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string>();
                properties.Add("Uri", uri);
                CoreFunction.AITrackException(functionName, ex, null, properties);
                throw ex;
            }
            finally
            {
                if (!string.IsNullOrEmpty(LogoutUri))
                {
                    var logout = Client.GetAsync(LogoutUri).Result;
                    Client.DefaultRequestHeaders.Remove("Cookie");
                }
            }

            return JsonConvert.DeserializeObject<T>(jsonRaw, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
        }

        static async Task<T> PostJsonToApiAsync<T>(string uri, string requestBody)
        {
            var jsonToReturn = "";
            List<string> cookies = null;

            try
            {
                Client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AuthenticationHeaderValue);
                var response = await Client.PostAsync(uri, new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                jsonToReturn = await response.Content.ReadAsStringAsync();

                cookies = (from c in response.Headers.Where(c => c.Key == "Set-Cookie")
                           from cv in c.Value
                           select cv
                ).ToList();
                Client.DefaultRequestHeaders.Remove("Authorization");
                Client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookies);

                response.Dispose();

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
            finally
            {
                if (!string.IsNullOrEmpty(LogoutUri))
                {
                    var logout = Client.GetAsync(LogoutUri).Result;
                    Client.DefaultRequestHeaders.Remove("Cookie");
                }
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
        /// <param name="mapping">The high-level asset-to-asset mapping.</param>
        /// <param name="fields">The asset field mappings.</param>
        /// <param name="relations">The asset relationship mappings.</param>
        /// <param name="roles">The asset role mappings.</param>
        /// <returns>An asynchronous boolean to indicate whether the process was successful or not.</returns>
        public static bool IGC_LoadAssetsByMappingType(
            IntegrationSetting setting, 
            bool checkForChangesOnly, 
            DateTime now, 
            IntegrationExecutionAssetType execution,
            IntegrationAssetType mapping, 
            List<IntegrationAssetTypeFieldItem> fields, 
            List<IntegrationAssetTypeRelationItem> relations,
            List<IntegrationAssetTypeRelationItemTarget> relationTargets,
            List<IntegrationAssetTypeRoleItem> roles, 
            CompanyContext company,
            CompanyWithDatabaseServerSettings cs,
            int previouslyProcessCount = 0)
        {
            var success = false; // By default, this has not successfully been processed yet.

            var igcData = new IgcDynamicArrayModels();
            var assets = new BulkAssetImport();
            var relationships = new BulkRelationshipImport();
            var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

            DateTime? currentParsedUnvalidatedDate = null;

            var enumFields = new List<string>();
            var enumValues = new List<EnumResolutionModel>();

            Func<JArray, bool, List<IntegrationExecutionAsset>, JArray> parse = delegate (JArray root, bool initialLogOnly, List<IntegrationExecutionAsset>  executionAssets)
            {
                var fieldErrors = new Dictionary<string, string>();

                if (!initialLogOnly)
                {
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
                }


                foreach (var obj in root.Children())
                {
                    var executionAsset = new IntegrationExecutionAsset();

                    try
                    {
                        var igcObjectSourceID = obj["_id"].Value<string>();

                        // Create the execution asset records.
                        executionAsset.ExecutionID = execution.ExecutionID;
                        executionAsset.SynchedAssetTypeID = execution.SynchedAssetTypeID;
                        executionAsset.SourceID = igcObjectSourceID;
                        executionAsset.RawObject = obj.ToString(Formatting.None);
                        executionAsset.ErrorMessages = "";

                        if (!initialLogOnly)
                        {
                            // Field Load Logic.
                            var targetObject = new Dictionary<string, string>(); //JObject();
                            fields.ForEach(f =>
                            {
                                if (f.ParentContextPosition.HasValue)
                                {
                                    // There is a hierarchy here, and we need to resolve it.
                                    try
                                    {
                                        if (obj[f.SourceField] != null)
                                        {
                                            var context = (obj[f.SourceField] as JArray); // obj[f.SourceField].Cast<List<GenericIgcContextModel>>().FirstOrDefault();
                                            if (context != null)
                                            {
                                                if (!targetObject.ContainsKey(f.TargetField))
                                                {
                                                    if (context.Count > 0)
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
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        if (!fieldErrors.ContainsKey($"{f.SourceField}"))
                                        {
                                            executionAsset.ErrorMessages += $"{f.SourceField}: {ex.GetFullExceptionData()}; ";
                                            fieldErrors.Add($"{f.SourceField}", ex.GetFullExceptionData());
                                        }
                                    }
                                }
                                else
                                {
                                    if (f.IsArray)
                                    {
                                        // If there is not already a target field with this name that is populated.
                                        if (!targetObject.ContainsKey(f.TargetField))
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
                                                    executionAsset.ErrorMessages += $"{f.SourceField}: {ex.GetFullExceptionData()}; ";
                                                    //targetObject.Add(f.TargetField, $"ERROR: {ex.Message}");
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
                                                        executionAsset.ErrorMessages += $"{f.SourceField}: {ex.GetFullExceptionData()}; ";
                                                        //targetObject.Add(f.TargetField, $"ERROR: {ex.Message}");
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
                                                        executionAsset.ErrorMessages += $"{f.SourceField}: {ex.GetFullExceptionData()}; ";
                                                        //targetObject.Add(f.TargetField, $"ERROR: {ex.Message}");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            if (!targetObject.ContainsKey(f.TargetField))
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
                                                    if (string.IsNullOrEmpty(f.ArrayValueFieldName))
                                                    {
                                                        targetObject.Add(f.TargetField, obj[f.SourceField].Value<string>());
                                                    }
                                                    else
                                                    {
                                                        // Treat this as a JObject and get the field value.
                                                        targetObject.Add(f.TargetField, obj[f.SourceField][f.ArrayValueFieldName].Value<string>());
                                                    }
                                                
                                                }
                                            
                                            }

                                            // Set default value if empty and there is a default value to be used.
                                            if (string.IsNullOrEmpty(targetObject[f.TargetField]) && !string.IsNullOrEmpty(f.DefaultValue))
                                            {
                                                targetObject[f.TargetField] = f.DefaultValue;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            if (!fieldErrors.ContainsKey($"{f.SourceField}"))
                                            {
                                                executionAsset.ErrorMessages += $"{f.SourceField}: {ex.GetFullExceptionData()}; ";
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
                            assets.Add(targetObject);

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
                                                        items.Select(i => new RelationshipImportRequest
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
                                                        relationships.Add(new RelationshipImportRequest
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
                                    executionAsset.ErrorMessages += $"{r.SourceField}: {ex.GetFullExceptionData()}; ";
                                }
                            });

                            // Role Load Logic.
                            roles.ForEach(r => {
                                try
                                {
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
                                }
                                catch (Exception ex)
                                {
                                    executionAsset.ErrorMessages += $"{r.SourceNameField}: {ex.GetFullExceptionData()}; ";
                                }
                            });
                        }

                        executionAssets.Add(executionAsset);
                    }
                    catch (Exception ex)
                    {
                        if (!fieldErrors.ContainsKey("ParseError"))
                        {
                            executionAsset.ErrorMessages += $"ParseError: {ex.GetFullExceptionData()}; ";
                            fieldErrors.Add("ParseError", ex.GetFullExceptionData());
                        }
                    }
                }

                if (fieldErrors.Keys.Count > 0)
                {
                    execution.ErrorMessage += string.Join("; ", fieldErrors.Select(o => $"{o.Key}: {o.Value}"));
                    //CoreFunction.AITrackEvent(functionName, $"{mapping.SourceAssetTypeName}, Parse Asset", fieldErrors, cs.CompanyID);
                }

                return root;
            };

            //First, get the type definition of the asset type, to pull enum values.
            var url = $"{setting.SourceUri}types/{mapping.SourceAssetTypeName}?showEditProperties=true";

            try
            {
                var igcType = GetFromApi<IgcTypeModel>(url).Result;

                if (igcType != null)
                {
                    igcType.EditInfo.Properties.ForEach(p =>
                    {
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
            catch (HttpRequestException rex)
            {
                if (rex.Message.Contains("403 (Forbidden)"))
                {
                    throw rex;
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }

            try
            {
                url = $"{setting.SourceUri}search/";

                //The raw sql connection to use for the specific company.
                var companyConnnectionString = $"Server={cs.Server};Database=D3S_{cs.CompanyID};User ID={cs.Username};Password={cs.Password}";
                var cnn = new SqlConnection(companyConnnectionString);

                //Perform search using POST method.
                var postModel = new IgcPostSearchRequestModel
                {
                    begin = previouslyProcessCount,
                    sorts = new List<IgcPostSearchRequestSortModel>() {
                        new IgcPostSearchRequestSortModel { ascending = true, property = "modified_on" },
                        new IgcPostSearchRequestSortModel { ascending = true, property = "created_on" }
                    }
                };

                #region Figure out page size

                var pageSize = 500;
                if (mapping.PageSize.HasValue)
                    pageSize = mapping.PageSize.Value;
                else
                    pageSize = setting.PageSize;
                postModel.pageSize = pageSize;

                #endregion

                postModel.types.Add(mapping.SourceAssetTypeName);

                postModel.properties.AddRange(fields.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceField)).Select(i => i.SourceField));
                postModel.properties.AddRange(relations.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceField)).Select(i => i.SourceField));
                postModel.properties.AddRange(roles.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceIdField)).Select(i => i.SourceIdField));
                postModel.properties.AddRange(roles.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceNameField)).Select(i => i.SourceNameField));

                if (!postModel.properties.Contains("created_on")) postModel.properties.Add("created_on");
                if (!postModel.properties.Contains("modified_on")) postModel.properties.Add("modified_on");

                var min = checkForChangesOnly ?
                    (ConvertDateToUnixTimeMilliseconds(mapping.LastSynchOn ?? new DateTime(1970, 1, 1, 0, 0, 0))) :
                    0;
                var max = ConvertDateToUnixTimeMilliseconds(now);

                postModel.where.conditions.Add(new IgcPostSearchRequestBetweenConditionModel { min = min, max = max, property = "created_on" });
                postModel.where.conditions.Add(new IgcPostSearchRequestBetweenConditionModel { min = min, max = max, property = "modified_on" });

                execution.IsFullRefresh = !checkForChangesOnly;

                var shouldContinue = true;
                var hasError = false;
                while (shouldContinue)
                {
                    try
                    {
                        var models = PostJsonToApiAsync<IgcDynamicArrayModels>(url, JsonConvert.SerializeObject(postModel)).Result;

                        if (models != null)
                        {
                            // Write the IGC total if we have not already done so.
                            if (execution.CurrentSourceAssetCount <= 0)
                            {
                                execution.CurrentSourceAssetCount = models.paging.numTotal;
                                execution.CurrentTargetAssetCount = 0;
                                company.Update(execution);
                            }

                            if (models.items.Count > 0)
                            {
                                // Create the execution asset records.
                                var executionAssets = new List<IntegrationExecutionAsset>();
                                parse(models.items, true, executionAssets);
                                cnn.BulkExecutionAssetLoad(setting.TargetResourceID, executionAssets);

                                // Now parse the data fully.
                                executionAssets = new List<IntegrationExecutionAsset>();
                                parse(models.items, false, executionAssets);
                                cnn.BulkExecutionAssetLoad(setting.TargetResourceID, executionAssets);

                                var executionRelationItems = relationships.Select(i => new IntegrationExecutionRelationItem { ExecutionID = execution.ExecutionID, IntersectTypeID = i.IntersectTypeID, SubjectSourceID = i.SubjectSourceID, ObjectSourceID = i.ObjectSourceID }).ToList();
                                cnn.BulkExecutionRelationItemLoad(setting.TargetResourceID, executionRelationItems);
                            }

                            // Should we do this again, since we have not completed the paged dataset.
                            shouldContinue = (models.paging.numTotal > models.paging.end + 1);
                            postModel.begin = models.paging.end + 1;

                            SendIncrementalSetToGovern(cnn, cs.CompanyID, cs.UrlPrefix, setting.TargetResourceID, mapping, assets, relationships, ownershipTopModel);

                            // Re-initialize.
                            assets = new BulkAssetImport();
                            relationships = new BulkRelationshipImport();
                            ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };
                        }
                    }
                    catch (Exception ex)
                    {
                        shouldContinue = false;
                        success = false;
                        hasError = true;
                        if (ex.Message.Contains("403 (Forbidden)"))
                        {
                            throw ex;
                        }
                        else
                        {
                            execution.ErrorMessage = ex.GetFullExceptionData();
                            CoreFunction.AITrackException(functionName, ex);
                        }
                    }
                }

                if (assets.Count > 0)
                {
                    success = SendIncrementalSetToGovern(cnn, cs.CompanyID, cs.UrlPrefix, setting.TargetResourceID, mapping, assets, relationships, ownershipTopModel);
                }
                else
                {
                    success = true; // Nothing left to post.
                }

                if (success & !hasError)
                {
                    mapping.LastSynchOn = now;
                }
            }
            catch (Exception oex)
            {
                try
                {
                    execution.ErrorMessage += oex.GetFullExceptionData();
                    company.Update(execution);
                }
                catch (Exception cex)
                {
                    CoreFunction.AITrackException(functionName, cex);
                }
                if (oex.Message.Contains("403 (Forbidden)"))
                {
                    throw oex;
                }
            }

            return success;
        }

        public static bool SendIncrementalSetToGovern(
            SqlConnection cnn, 
            int companyID, string companyDomain, int resourceID, 
            IntegrationAssetType mapping, BulkAssetImport assets, BulkRelationshipImport relationships, D3sOwnershipItemsModel owners)
        {
            bool successfulPost = true;

            var queue = new AzureQueueSource();

            // If any items to send to server.
            if (assets.Count > 0)
            {
                try
                {
                    var assetResults = cnn.BulkAssetsImport(resourceID, (SystemObjects)Enum.Parse(typeof(SystemObjects), mapping.Object), mapping.ObjectID.Value, assets);

                    if (assetResults != null)
                    {
                        var assetErrorMessage = string.Join(". ", assetResults.Where(r => !r.Success).Select(r => $"{r.SourceID} : {r.Message}"));
                        if (!string.IsNullOrEmpty(assetErrorMessage))
                        {
                            CoreFunction.AITrackEvent(functionName, "Bulk Import Assets", new Dictionary<string, string>() { { "Error", assetErrorMessage } }, companyID);
                        }
                        
                        #region Cycle through the return results from the database, and update the results collection to send back to the caller.

                        var events = new List<EventInfo>();
                        var sObject = (SystemObjects)Enum.Parse(typeof(SystemObjects), mapping.Object.Replace("Type", ""));
                        var sObjectType = (SystemObjects)Enum.Parse(typeof(SystemObjects), mapping.Object);
                        assetResults.ForEach(d =>
                        {
                            if (string.IsNullOrEmpty(d.Message))
                            {
                                if (mapping.TriggerTopicMessage && d.Success)
                                {
                                    events.Add(new EventInfo
                                    {
                                        CompanyID = companyID,
                                        DomainPrefix = companyDomain,
                                        ResourceID = resourceID,
                                        Action = d.IsNew ? ChangeType.Add : ChangeType.Update,
                                        Object = new EventObjectInfo { Object = sObject, ObjectType = sObjectType, ObjectID = d.ObjectID, ObjectTypeID = mapping.ObjectID.Value }
                                    });

                                    if (events.Count > 50)
                                    {
                                        queue.CreateTopicMessages(events);
                                        events.Clear();
                                    }
                                }
                            }
                        });

                        if (mapping.TriggerTopicMessage && events.Count > 0)
                        {
                            queue.CreateTopicMessages(events);
                        }

                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    successfulPost = false;
                    CoreFunction.AITrackException(functionName, ex);
                }
            }

            // If any owners to send to server.
            if (owners.Items.Count > 0)
            {
                var uniqueUsers = owners.Items
                    .Where(i => !string.IsNullOrEmpty(i.UserFullName) && !string.IsNullOrEmpty(i.UserId))
                    .Select(i => new { i.UserFullName, i.UserId })
                    .Distinct()
                    .ToList();

                // Populate the UserIDs that are missing, based can be looked up by user's full name. TODO: Confirm this logic, as it may not be correct if two or more user's have the same name.
                foreach (var item in owners.Items.Where(i => string.IsNullOrEmpty(i.UserId)))
                {
                    var match = uniqueUsers.FirstOrDefault(i => i.UserFullName == item.UserFullName);
                    if (match != null)
                    {
                        item.UserId = match.UserId;
                    }
                }

                //Now, remove any users whose internal ID cannot be resolved.
                owners.Items.RemoveAll(i => string.IsNullOrEmpty(i.UserId));

                try
                {
                    if (owners.Items.Count > 0)
                    {
                        var ownerImport = new BulkOwnerImport { UserIdFieldName = owners.UserIdFieldName };
                        ownerImport.Items = owners.Items.Select(i => new OwnerImportRequest { RoleName = i.RoleName, SourceID = i.SourceID, UserId = i.UserId }).ToList();
                        var ownerResults = cnn.BulkOwnersImport(resourceID, ownerImport);
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                }
            }

            // If any relationships to send to server.
            if (relationships.Count > 0)
            {
                try
                {
                    var relationshipResults = cnn.BulkRelationshipsImport(resourceID, relationships);
                    if (relationshipResults != null)
                    {
                        var events = new List<EventInfo>();
                        relationshipResults.ForEach(r => {
                            if (mapping.TriggerTopicMessage && r.IsNew == true && r.IntersectID != null)
                            {
                                events.Add(new EventInfo
                                {
                                    CompanyID = companyID,
                                    DomainPrefix = companyDomain,
                                    ResourceID = resourceID,
                                    Action = ChangeType.Add,
                                    Object = new EventObjectInfo { Object = SystemObjects.Intersect, ObjectType = SystemObjects.IntersectType, ObjectID = r.IntersectID, ObjectTypeID = r.IntersectTypeID }
                                });
                                if (events.Count > 50)
                                {
                                    queue.CreateTopicMessages(events);
                                    events.Clear();
                                }
                            }
                        });

                        if (mapping.TriggerTopicMessage && events.Count > 0)
                        {
                            queue.CreateTopicMessages(events);
                        }
                    }
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
