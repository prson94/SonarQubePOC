using d360.core;
using d360.core.entities;
using d360.core.enums.Workflow;
using d360.core.queue;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.model;
using d360.utils.company;
using igx.jobs;
using igx.jobs.igc;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
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
            config.Queues.BatchSize = 5;
            config.Queues.VisibilityTimeout = TimeSpan.FromDays(4);
            var host = new JobHost(config);
            host.RunAndBlock();
        }

    }

    public static class IgcIntegration
    {
        
#if DEBUG
        const string timerSettings = "*/5 * * * * *";
#else
        const string timerSettings = "0 */5 * * * *";
#endif

        public static void RunScheduleViaTimer([TimerTrigger(timerSettings)]TimerInfo myTimer, CancellationToken token, TextWriter log)
        {
            string functionName = "IGC_Integration_Schedule";

            try
            {
                var Caching = new DummyCachingProvider();
                var Queue = new AzureQueueSource();
                var Security = new UriSecurityContextProvider { IsAdministrator = true, ResourceID = 0 };
                var Community = new CommunityContext(Caching, Queue, Security);
#if DEBUG
                var companies = CoreFunction.GetCompaniesByCurrentSlot().Where(i => i.CompanyID == 120).ToList();
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

                        // Do this call in here so we do not incur the cost of four DB calls for every database unless we absolutely have to.
                        if (settings.Count > 0)
                        {
#if DEBUG
                            //var IDs = new List<int>() { 16 }; //, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12
                            mappings = company.Filter<IntegrationAssetType>(i => i.Active).ToList();// && IDs.Contains(i.ID)).ToList(); // testing only.
#else
                            mappings = company.Filter<IntegrationAssetType>(i => i.Active).ToList();
#endif
                        }

                        var executionIDs = new List<ExecutionAssetType>(); //To loop through to synch deletions.
                    
                        foreach (var setting in settings)
                        {
                            var now = DateTime.UtcNow;

                            if (mappings.Any(i => i.Active && i.IntegrationSettingID == setting.ID && i.ObjectID.HasValue))
                            {
                                var assetsToAvoid = company.Query<int>(@"
select		T.SynchedAssetTypeID
from		integration.ExecutionAssetType T
where		T.CompletedOn is null
			and T.ErrorMessage is null
            and T.ExecutionID > (select coalesce(max(ID)-10, 0) from integration.Execution)").ToList();

                                if (assetsToAvoid.Count > 0)
                                {
                                    log.WriteLine($"Avoiding assets: {string.Join(", ", assetsToAvoid)}");
                                }

                                if (mappings.Any(i => !assetsToAvoid.Contains(i.ID)))
                                {
                                    var execution = new IntegrationExecution { StartedOn = now };
                                    company.Add(execution);

                                    log.WriteLine($"Creating execution {execution.ID}");

                                    foreach (var item in mappings.Where(i => i.IntegrationSettingID == setting.ID && !assetsToAvoid.Contains(i.ID) && i.ObjectID.HasValue))
                                    {
                                        var atExecution = new IntegrationExecutionAssetType { StartedOn = now, SynchedAssetTypeID = item.ID, ExecutionID = execution.ID };
                                        company.Add(atExecution);
                                        var queueModel = new IntegrationQueueModel {
                                            CompanyID = c.CompanyID,
                                            ExecutionID = execution.ID,
                                            IntegrationSettingID = setting.ID,
                                            SynchedAssetTypeID = item.ID,
                                            To = QueueAction.Integration,
                                            UrlPrefix = c.UrlPrefix
                                        };
                                        Queue.CreateMessage(CoreFunction.GetConfigValueByKey("IntegrationQueue"), queueModel);
                                        log.WriteLine($"Queued execution {atExecution.ExecutionID}, asset {atExecution.SynchedAssetTypeID}, full refresh {(atExecution.IsFullRefresh ? "Yes" : "No")}");
                                    }
                                }
                            }
                        }
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

        public static void RunViaQueue([QueueTrigger("%IntegrationQueue%"), StorageAccount("MainStorageAccount")] string myQueueItem, TextWriter log)
        {
            var queueModel = JsonConvert.DeserializeObject<IntegrationQueueModel>(myQueueItem);

            var engine = new IgcIntegrationEngine();
            engine.RunSingle(queueModel, log);
        }
    }

    public class IgcIntegrationEngine
    {
        const string functionName = "IGC_Integration";

        private HttpClient _client = null;
        public HttpClient Client
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

        public string AuthenticationHeaderValue { get; set; } = null;

        public string LogoutUri { get; set; } = null;

        public void RunSingle(IntegrationQueueModel model, TextWriter log)
        {
            var Caching = new DummyCachingProvider();
            var Queue = new AzureQueueSource();
            var Security = new UriSecurityContextProvider { IsAdministrator = true, ResourceID = 0 };
            var Community = new CommunityContext(Caching, Queue, Security);

            Community.CurrentCompanyID = model.CompanyID;
            Community.CurrentCompanyDomain = model.UrlPrefix;
            Security.CompanyID = model.CompanyID;
            Security.CompanyPrefix = model.UrlPrefix;
            var company = new CompanyContext(Community, Caching, Queue, Security, true);

            var synchedAssetType = company.GetById<IntegrationAssetType>(model.SynchedAssetTypeID);
            var executionAssetType = company.Filter<IntegrationExecutionAssetType>(i => i.ExecutionID == model.ExecutionID && i.SynchedAssetTypeID == model.SynchedAssetTypeID).Single();
            var setting = company.GetById<IntegrationSetting>(model.IntegrationSettingID);
            var fields = company.Filter<IntegrationAssetTypeFieldItem>(i => i.Active && i.SynchedAssetTypeID == model.SynchedAssetTypeID).ToList();
            var relations = company.Filter<IntegrationAssetTypeRelationItem>(i => i.Active && i.SynchedAssetTypeID == model.SynchedAssetTypeID).ToList();
            var relationTargets = company.Filter<IntegrationAssetTypeRelationItemTarget>(i => i.IntegrationAssetTypeRelationItem.Active && i.IntegrationAssetTypeRelationItem.SynchedAssetTypeID == model.SynchedAssetTypeID).ToList();
            var roles = company.Filter<IntegrationAssetTypeRoleItem>(i => i.Active && i.SynchedAssetTypeID == model.SynchedAssetTypeID).ToList();

            var now = DateTime.UtcNow;
            string url;

            AuthenticationHeaderValue = $"Basic {Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(setting.SourceUser + ":" + setting.SourcePassword))}";

            #region Get type definition

            try
            {
                // First, get the type definition of the asset type, to pull enum values.
                url = $"{setting.SourceUri}types/{synchedAssetType.SourceAssetTypeName}?showEditProperties=true";

                var igcType = GetFromApi<IgcTypeModel>(url).Result;

                if (igcType != null)
                {
                    var enumValues = new List<EnumResolutionModel>();

                    executionAssetType.RawDefinition = JsonConvert.SerializeObject(igcType);

                    igcType.EditInfo.Properties.ForEach(p =>
                    {
                        if (p.Type.Name == "enum")
                        {
                            if (p.Type.Values != null)
                            {
                                enumValues.AddRange(p.Type.Values.Select(i => new EnumResolutionModel
                                {
                                    PropertyName = p.Name,
                                    Code = i.Code,
                                    DisplayValue = i.DisplayName
                                }));
                            }
                        }
                    });

                    if (enumValues.Count > 0)
                    {
                        executionAssetType.EnumFieldValues = JsonConvert.SerializeObject(enumValues);
                    }

                    company.Update(executionAssetType);
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

            #endregion

            try
            {
                url = $"{setting.SourceUri}search/";

                // The raw sql connection to use for the specific company.
                var cnn = CompanyConnectionUtils.GetCompanyConnection(model.CompanyID);

                // Perform search using POST method.
                var postModel = new IgcPostSearchRequestModel();

                if (synchedAssetType.AllowChangeDetection)
                {
                    postModel.sorts = new List<IgcPostSearchRequestSortModel>() {
                        new IgcPostSearchRequestSortModel { ascending = true, property = "modified_on" },
                        new IgcPostSearchRequestSortModel { ascending = true, property = "created_on" }
                    };
                }

                #region Figure out page size

                var fieldPageSize = setting.PageSize;
                var relationshipPageSize = setting.PageSize;
                var ownershipPageSize = setting.PageSize;

                if (synchedAssetType.FieldPageSize.HasValue)
                    fieldPageSize = synchedAssetType.FieldPageSize.Value;

                if (synchedAssetType.RelationshipPageSize.HasValue)
                    relationshipPageSize = synchedAssetType.RelationshipPageSize.Value;

                if (synchedAssetType.OwnershipPageSize.HasValue)
                    ownershipPageSize = synchedAssetType.OwnershipPageSize.Value;

                #endregion

                postModel.types.Add(synchedAssetType.SourceAssetTypeName);

                int currentCount = 0;
                var errors = new List<string>();
                var errorBegins = new List<int?>();
                var hasRootError = false;   //Used to determine if we should run the stored proecdure called (Section=0) that actually deletes assets. If an error occured do not try to delete.

                #region Delta or Full Refresh

                var checkForChangesOnly = true;

                if (synchedAssetType.AllowChangeDetection)
                {
                    // Continue to check if we should perform delta.

                    var lastFullRefreshModel = company.Query<dynamic>(@"
select		E.SynchedAssetTypeID,
			E.ExecutionID,
			E.StartedOn,
			C.[Count]
from		(
			select		E.SynchedAssetTypeID,
						max(E.ExecutionID) as ExecutionID,
						max(E.StartedOn) as StartedOn
			from		integration.ExecutionAssetType E 
			where		E.SynchedAssetTypeID = @a 
						and E.IsFullRefresh = 1 
						and E.CompletedOn is not null
			group by	E.SynchedAssetTypeID
			) E 
			cross apply (
				select	count(1) as [Count] 
				from	integration.ExecutionAsset A
                        inner join integration.ExecutionAssetType IE on 
                            IE.ExecutionID = A.ExecutionID 
                            and IE.SynchedAssetTypeID = A.SynchedAssetTypeID 
                            and IE.ErrorMessage is not null 
                            and IE.ErrorMessage <> ''
				where	A.ExecutionID = E.ExecutionID 
						and A.SynchedAssetTypeID = E.SynchedAssetTypeID
			) C", new { a = model.SynchedAssetTypeID }).SingleOrDefault();

                    if (lastFullRefreshModel != null)
                    {
                        currentCount = lastFullRefreshModel.Count;

                        if (currentCount > 0)
                        {
                            // If > 0, that means that the last full refresh did not successfully complete. 
                            // Do not check for changes, do a full refresh again, starting where you left off.
                            checkForChangesOnly = false;
                        }
                        else
                        {
                            var refreshInterval = (synchedAssetType.RefreshIntervalOverride.HasValue) ?
                                synchedAssetType.RefreshIntervalOverride.Value :
                                setting.RefreshInterval;

                            // If last refresh+interval > current time, then perform a delta instead, as you have not surpassed the refresh interval.
                            checkForChangesOnly = (lastFullRefreshModel.StartedOn.AddHours(refreshInterval) > now);
                        }
                    }
                    else
                    {
                        checkForChangesOnly = false;
                    }
                }
                else
                {
                    // Delta checking not even allowed.
                    checkForChangesOnly = false;
                }

                var min = checkForChangesOnly ?
                    (ConvertDateToUnixTimeMilliseconds(synchedAssetType.LastSynchOn ?? new DateTime(1970, 1, 1, 0, 0, 0))) :
                    0;
                var max = ConvertDateToUnixTimeMilliseconds(now);

#if DEBUG
                min = 0;
#endif

                if (synchedAssetType.AllowChangeDetection)
                {
                    postModel.where.conditions.Add(new IgcPostSearchRequestBetweenConditionModel { min = min, max = max, property = "created_on" });
                    postModel.where.conditions.Add(new IgcPostSearchRequestBetweenConditionModel { min = min, max = max, property = "modified_on" });
                }

                //If starting from beginning, you are effectively doing a full refresh.
                if (min == 0)
                {
                    checkForChangesOnly = false;
                }

                executionAssetType.IsFullRefresh = !checkForChangesOnly;

                #endregion

                var shouldContinue = true;

                #region Fields Request

                postModel.pageSize = fieldPageSize;
                postModel.begin = currentCount;
                postModel.properties.Clear();
                postModel.properties.AddRange(fields.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceField)).Select(i => i.SourceField));
                if (synchedAssetType.AllowChangeDetection)
                {
                    if (!postModel.properties.Contains("created_on")) postModel.properties.Add("created_on");
                    if (!postModel.properties.Contains("modified_on")) postModel.properties.Add("modified_on");
                }

                while (shouldContinue)
                {
                    try
                    {
                        log.WriteLine($"Company: {model.CompanyID}, Fields Begin Value: {postModel.begin}");

                        var models = PostJsonToApiAsync<IgcDynamicArrayModels>(url, JsonConvert.SerializeObject(postModel)).Result;

                        if (models != null)
                        {
                            // Write the IGC total if we have not already done so.
                            if (executionAssetType.CurrentSourceAssetCount <= 0)
                            {
                                executionAssetType.CurrentSourceAssetCount = (models.paging != null) ? models.paging.numTotal : 0;
                                executionAssetType.CurrentTargetAssetCount = 0;
                                company.Update(executionAssetType);
                            }

                            if (models.items.Count > 0)
                            {
                                var list = new List<IntegrationExecutionAsset>();
                                //parse(models.items, false, list);
                                foreach (var obj in models.items.Children())
                                {
                                    var executionAsset = new IntegrationExecutionAsset
                                    {
                                        ExecutionID = executionAssetType.ExecutionID,
                                        SynchedAssetTypeID = executionAssetType.SynchedAssetTypeID,
                                        SourceID = obj["_id"].Value<string>(),
                                        RawObject = obj.ToString(Formatting.None)
                                    };
                                    list.Add(executionAsset);
                                }

                                cnn.BulkExecutionAssetLoad(setting.TargetResourceID, list);
                            }

                            // Should we do this again, since we have not completed the paged dataset.
                            shouldContinue = (models.paging.numTotal > models.paging.end + 1);
                            postModel.begin = models.paging.end + 1;
                        }
                    }
                    catch (Exception postEx)
                    {
                        hasRootError = true;
                        string errorMessage;

                        if (postEx.Message.Contains("Unexpected character encountered while parsing value: <. Path"))
                            errorMessage = $"Encountered IGC HTML generic error page";
                        else
                            errorMessage = $"{postEx.GetFullExceptionData()}";

                        errorBegins.Add(postModel.begin);
                        if (!errors.Contains(errorMessage))
                        {
                            errors.Add(errorMessage);
                        }

                        // Move onto next page.
                        postModel.begin = postModel.begin + postModel.pageSize;
                        if (postModel.begin >= executionAssetType.CurrentSourceAssetCount)
                        {
                            shouldContinue = false;
                        }
                    }
                }
                if (errors.Count > 0)
                {
                    executionAssetType.ErrorMessage += "Field Requests: " + string.Join("; ", errors);
                    executionAssetType.ErrorMessage += ". Begin values: " + string.Join("; ", errorBegins);
                    errors.Clear();
                    errorBegins.Clear();
                }

                #endregion

                #region Relations request

                postModel.pageSize = relationshipPageSize;
                postModel.begin = currentCount;
                postModel.properties.Clear();
                postModel.properties.AddRange(relations.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceField)).Select(i => i.SourceField));
                if (synchedAssetType.AllowChangeDetection)
                {
                    if (!postModel.properties.Contains("created_on")) postModel.properties.Add("created_on");
                    if (!postModel.properties.Contains("modified_on")) postModel.properties.Add("modified_on");
                }

                shouldContinue = true;

                while (shouldContinue)
                {
                    try
                    {
                        log.WriteLine($"Company: {model.CompanyID}, Relations Begin Value: {postModel.begin}");

                        var models = PostJsonToApiAsync<IgcDynamicArrayModels>(url, JsonConvert.SerializeObject(postModel)).Result;

                        if (models != null)
                        {
                            if (models.items.Count > 0)
                            {
                                var list = new List<IntegrationExecutionAsset>();
                                foreach (var obj in models.items.Children())
                                {
                                    var executionAsset = new IntegrationExecutionAsset
                                    {
                                        ExecutionID = executionAssetType.ExecutionID,
                                        SynchedAssetTypeID = executionAssetType.SynchedAssetTypeID,
                                        SourceID = obj["_id"].Value<string>(),
                                        RawObject = obj.ToString(Formatting.None)
                                    };
                                    list.Add(executionAsset);
                                }
                                cnn.BulkExecutionAssetLoad(setting.TargetResourceID, list, "RawRelationships");
                            }

                            // Should we do this again, since we have not completed the paged dataset.
                            shouldContinue = (models.paging.numTotal > models.paging.end + 1);
                            postModel.begin = models.paging.end + 1;
                        }
                    }
                    catch (Exception postEx)
                    {
                        string errorMessage;

                        if (postEx.Message.Contains("Unexpected character encountered while parsing value: <. Path"))
                            errorMessage = $"Encountered IGC HTML generic error page";
                        else
                            errorMessage = $"{postEx.GetFullExceptionData()}";

                        errorBegins.Add(postModel.begin);
                        if (!errors.Contains(errorMessage))
                        {
                            errors.Add(errorMessage);
                        }
                        postModel.begin = postModel.begin + postModel.pageSize;
                        if (postModel.begin >= executionAssetType.CurrentSourceAssetCount)
                        {
                            shouldContinue = false;
                        }
                    }
                }
                if (errors.Count > 0)
                {
                    executionAssetType.ErrorMessage += "Relation Requests: " + string.Join("; ", errors);
                    executionAssetType.ErrorMessage += ". Begin values: " + string.Join("; ", errorBegins);
                    errors.Clear();
                    errorBegins.Clear();
                }

                #endregion

                #region Ownership request

                postModel.pageSize = ownershipPageSize;
                postModel.begin = currentCount;
                postModel.properties.Clear();
                postModel.properties.AddRange(roles.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceIdField)).Select(i => i.SourceIdField));
                //postModel.properties.AddRange(roles.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceNameField)).Select(i => i.SourceNameField));
                if (synchedAssetType.AllowChangeDetection)
                {
                    if (!postModel.properties.Contains("created_on")) postModel.properties.Add("created_on");
                    if (!postModel.properties.Contains("modified_on")) postModel.properties.Add("modified_on");
                }

                shouldContinue = true;

                while (shouldContinue)
                {
                    try
                    {
                        log.WriteLine($"Company: {model.CompanyID}, Ownership Begin Value: {postModel.begin}");

                        var models = PostJsonToApiAsync<IgcDynamicArrayModels>(url, JsonConvert.SerializeObject(postModel)).Result;

                        if (models != null)
                        {
                            if (models.items.Count > 0)
                            {
                                var list = new List<IntegrationExecutionAsset>();
                                foreach (var obj in models.items.Children())
                                {
                                    var executionAsset = new IntegrationExecutionAsset
                                    {
                                        ExecutionID = executionAssetType.ExecutionID,
                                        SynchedAssetTypeID = executionAssetType.SynchedAssetTypeID,
                                        SourceID = obj["_id"].Value<string>(),
                                        RawObject = obj.ToString(Formatting.None)
                                    };
                                    list.Add(executionAsset);
                                }
                                cnn.BulkExecutionAssetLoad(setting.TargetResourceID, list, "RawResponsibilitites");
                            }

                            // Should we do this again, since we have not completed the paged dataset.
                            shouldContinue = (models.paging.numTotal > models.paging.end + 1);
                            postModel.begin = models.paging.end + 1;
                        }
                    }
                    catch (Exception postEx)
                    {
                        string errorMessage;

                        if (postEx.Message.Contains("Unexpected character encountered while parsing value: <. Path"))
                            errorMessage = $"Encountered IGC HTML generic error page";
                        else
                            errorMessage = $"{postEx.GetFullExceptionData()}";

                        errorBegins.Add(postModel.begin);
                        if (!errors.Contains(errorMessage))
                        {
                            errors.Add(errorMessage);
                        }
                        postModel.begin = postModel.begin + postModel.pageSize;
                        if (postModel.begin >= executionAssetType.CurrentSourceAssetCount)
                        {
                            shouldContinue = false;
                        }
                    }
                }
                if (errors.Count > 0)
                {
                    executionAssetType.ErrorMessage += "Ownership Requests: " + string.Join("; ", errors);
                    executionAssetType.ErrorMessage += ". Begin values: " + string.Join("; ", errorBegins);
                    errors.Clear();
                    errorBegins.Clear();
                }

                #endregion

                DateTime start;
                DateTime end;

                // Section 0 : Asset
                if (!hasRootError && !checkForChangesOnly)
                {
                    log.WriteLine($"Begin: Processing Section 0");
                    start = DateTime.UtcNow;
                    cnn.ProcessExecutionAssetType<dynamic>(executionAssetType.ExecutionID, executionAssetType.SynchedAssetTypeID, synchedAssetType.AssetTypeID, setting.TargetResourceID, 0);
                    end = DateTime.UtcNow;
                    log.WriteLine($"End: Processing Section 0. Took {end.Subtract(start).Minutes} minutes, {end.Subtract(start).Seconds} seconds.");
                }

                // Section 1 : Fields
                log.WriteLine($"Begin: Processing Section 1");
                start = DateTime.UtcNow;
                cnn.ProcessExecutionAssetType<dynamic>(executionAssetType.ExecutionID, executionAssetType.SynchedAssetTypeID, synchedAssetType.AssetTypeID, setting.TargetResourceID, 1);
                end = DateTime.UtcNow;
                log.WriteLine($"End: Processing Section 1. Took {end.Subtract(start).Minutes} minutes, {end.Subtract(start).Seconds} seconds.");

                // Section 2 : Relationships
                log.WriteLine($"Begin: Processing Section 2");
                start = DateTime.UtcNow;
                var relationshipActions = cnn.ProcessExecutionAssetType<RelationshipAction>(executionAssetType.ExecutionID, executionAssetType.SynchedAssetTypeID, synchedAssetType.AssetTypeID, setting.TargetResourceID, 2);
                end = DateTime.UtcNow;
                log.WriteLine($"End: Processing Section 2. Took {end.Subtract(start).Minutes} minutes, {end.Subtract(start).Seconds} seconds.");

                #region Send Relationship Events

                var queue = new AzureQueueSource();

                var events = new List<EventInfo>();

                if (synchedAssetType.TriggerTopicMessage)
                {
                    int lastIntersectID = 0;

                    foreach (var relationshipAction in relationshipActions)
                    {
                        var changeType = ChangeType.Add;

                        switch (relationshipAction.Action)
                        {
                            case "D":
                                changeType = ChangeType.Delete;
                                break;
                            case "U":
                                changeType = ChangeType.Update;
                                break;
                        }

                        // Check to make sure we do not send multiple workflows out for the same intersect ID.
                        if (relationshipAction.IntersectID != lastIntersectID)
                        {
                            events.Add(new EventInfo
                            {
                                CompanyID = company.CurrentCompanyID,
                                DomainPrefix = model.UrlPrefix,
                                ResourceID = setting.TargetResourceID,
                                Action = changeType,
                                Object = new EventObjectInfo
                                {
                                    Object = SystemObjects.Intersect,
                                    ObjectType = SystemObjects.IntersectType,
                                    ObjectID = relationshipAction.IntersectID,
                                    ObjectTypeID = relationshipAction.IntersectTypeID
                                }
                            });
                        }

                        if (events.Count > 50)
                        {
                            queue.CreateTopicMessages(events);
                            events.Clear();
                        }

                        lastIntersectID = relationshipAction.IntersectID;
                    }
                }

                if (events.Count > 0)
                {
                    queue.CreateTopicMessages(events);
                    events.Clear();
                }

                #endregion

                // Section 3 : Responsibilities
                log.WriteLine($"Begin: Processing Section 3");
                start = DateTime.UtcNow;
                cnn.ProcessExecutionAssetType<dynamic>(executionAssetType.ExecutionID, executionAssetType.SynchedAssetTypeID, synchedAssetType.AssetTypeID, setting.TargetResourceID, 3);
                end = DateTime.UtcNow;
                log.WriteLine($"End: Processing Section 3. Took {end.Subtract(start).Minutes} minutes, {end.Subtract(start).Seconds} seconds.");

                // Set the last synch time so we can start the next delta check from this date.
                synchedAssetType.LastSynchOn = now;
            }
            catch (Exception oex)
            {
                try
                {
                    executionAssetType.ProcessedDelete = true;
                    executionAssetType.ErrorMessage += oex.GetFullExceptionData();
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
            finally
            {
                try
                {
                    executionAssetType.CompletedOn = DateTime.UtcNow;
                    company.Update(executionAssetType);
                }
                catch (Exception cex)
                {
                    CoreFunction.AITrackException(functionName, cex);
                }
            }
        }

        #region Generic

        long ConvertDateToUnixTimeMilliseconds(DateTime? date = null)
        {
            long epoch = 0;

            if (!date.HasValue)
                date = DateTime.UtcNow;

            epoch = (long)(date.Value.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;

            return epoch;
        }

        async Task<T> GetFromApi<T>(string uri)
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

                using (var response = await Client.GetAsync(uri))
                {
                    jsonRaw = await response.Content.ReadAsStringAsync();

                    cookies = (from c in response.Headers.Where(c => c.Key == "Set-Cookie")
                               from cv in c.Value
                               select cv
                    ).ToList();
                    Client.DefaultRequestHeaders.Remove("Authorization");
                    Client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookies);
                }
                
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string>();
                properties.Add("Uri", uri);
                properties.Add("Response", jsonRaw);
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

        async Task<T> PostJsonToApiAsync<T>(string uri, string requestBody)
        {
            var jsonToReturn = "";
            List<string> cookies = null;

            try
            {
                Client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AuthenticationHeaderValue);

                using (var response = await Client.PostAsync(uri, new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json")))
                {
                    jsonToReturn = await response.Content.ReadAsStringAsync();

                    cookies = (from c in response.Headers.Where(c => c.Key == "Set-Cookie")
                               from cv in c.Value
                               select cv
                    ).ToList();
                    Client.DefaultRequestHeaders.Remove("Authorization");
                    Client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookies);
                }

                return JsonConvert.DeserializeObject<T>(jsonToReturn, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string>();
                properties.Add("Uri", uri);
                properties.Add("Request Body", requestBody);
                properties.Add("Response", jsonToReturn);
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
    }
}
