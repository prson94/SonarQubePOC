using d360.core;
using d360.core.entities;
using d360.core.enums.Workflow;
using d360.core.queue;
using d360.extensions;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.extensions.storage;
using d360.model;
using d360.utils.company;
using Dapper;
using igx.jobs;
using igx.jobs.igc;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
            config.Queues.BatchSize = 4;
            config.Queues.VisibilityTimeout = TimeSpan.FromDays(4);
            var host = new JobHost(config);
            host.RunAndBlock();
        }

    }

    public static class IgcIntegration
    {

#if DEBUG
        const string timerSettings = "0 */2 * * * *";//"*/5 * * * * *";
#else
        const string timerSettings = "0 */5 * * * *";
#endif

        //[Disable]
        public static void RunScheduleViaTimer([TimerTrigger(timerSettings)]TimerInfo myTimer, CancellationToken token, TextWriter log)
        {
            string functionName = "IGC_Integration_Schedule";
            CoreFunction.AppInsightsInstrumentationKey(CoreFunction.GetConfigValueByKey("IGC_APPINSIGHTS_INSTRUMENTATIONKEY"));

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
                            var IDs = new List<int>() { 1 }; //, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12
                            mappings = company.Filter<IntegrationAssetType>(i => i.Active && IDs.Contains(i.ID)).ToList(); // testing only.
#else
                            mappings = company.Filter<IntegrationAssetType>(i => i.Active).ToList();
#endif

                            #region Do cleanup of really old execution assets, getting rid of ones older than delete timeout.

                            SqlConnection cnn = null;

                            try
                            {
                                cnn = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID);
                                if (cnn.State != System.Data.ConnectionState.Open)
                                    cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                                cnn.Execute(@"
declare @dt datetime
set @dt = getutcdate()

delete  T
from	integration.ExecutionAsset T
		inner join integration.ExecutionAssetType EA on EA.ExecutionID = T.ExecutionID and EA.SynchedAssetTypeID = T.SynchedAssetTypeID
		inner join integration.SynchedAssetType A on A.ID = T.SynchedAssetTypeID
		inner join integration.Setting S on S.ID = A.IntegrationSettingID
where	EA.CompletedOn is null
		and EA.StartedOn < DATEADD(hh, -(coalesce(A.DeleteExecutionTimeoutHours, S.DeleteExecutionTimeoutHours)), @dt)

delete  T
from	integration.ExecutionAssetType T
		inner join integration.SynchedAssetType A on A.ID = T.SynchedAssetTypeID
		inner join integration.Setting S on S.ID = A.IntegrationSettingID
where	T.CompletedOn is null
		and T.StartedOn < DATEADD(hh, -(coalesce(A.DeleteExecutionTimeoutHours, S.DeleteExecutionTimeoutHours)), @dt)", new List<SqlParameter>());
                            }
                            catch (Exception cex)
                            {
                                log.WriteLine($"Unable to remove old execution asset types for company ({c.CompanyID}) due to the following error: {cex.GetFullExceptionData()}"); ;
                            }
                            finally
                            {
                                if (cnn != null)
                                    cnn.Dispose();
                            }

                            #endregion
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
			and T.ErrorMessage is null").ToList();

                                //            and T.ExecutionID > (select coalesce(max(ID)-10, 0) from integration.Execution)

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
            CoreFunction.AppInsightsInstrumentationKey(CoreFunction.GetConfigValueByKey("IGC_APPINSIGHTS_INSTRUMENTATIONKEY"));
            var queueModel = JsonConvert.DeserializeObject<IntegrationQueueModel>(myQueueItem);

            var engine = new IgcIntegrationEngine();
            engine.Log = log;
            engine.QueueModel = queueModel;
            engine.RunSingle();
        }
    }

    public enum PageDataClass
    {
        Fields,
        Relations,
        Responsibilities
    }

    public class PageBeginValueUpdatedEventArgs : EventArgs
    {
        public int Value { get; set; }
        public PageDataClass Class { get; set; }
    }

    public class PageErrorCapturedEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }

    public class IgcPageErrorModel
    {
        public string message { get; set; }
        public HttpStatusCode code { get; set; }
    }

    public class IgcIntegrationEngine
    {
        public event EventHandler<PageBeginValueUpdatedEventArgs> PageBeginValueUpdated;

        protected virtual void OnPageBeginValueUpdated(PageBeginValueUpdatedEventArgs e)
        {
            PageBeginValueUpdated?.Invoke(this, e);
        }

        public event EventHandler<PageErrorCapturedEventArgs> PageErrorCaptured;

        protected virtual void OnPageErrorCaptured(PageErrorCapturedEventArgs e)
        {
            PageErrorCaptured?.Invoke(this, e);
        }

        const string functionName = "IGC_Integration";

        #region Private Properties

        private IStorageProvider _storage = null;
        private IStorageProvider Storage
        {
            get
            {
                if (_storage == null)
                {
                    _storage = new AzureStorageProvider();
                }

                return _storage;
            }
        }

        private HttpClient _client = null;
        private HttpClient Client
        {
            get
            {
                if (_client == null)
                {
                    var handler = new HttpClientHandler { UseCookies = false }; //SslProtocols = System.Security.Authentication.SslProtocols.Tls, 
                    _client = new HttpClient(handler, false);
                    _client.Timeout = new TimeSpan(1, 0, 0);
                    _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                }

                return _client;
            }
        }

        private string AuthenticationHeaderValue { get; set; } = null;

        private string LogoutUri { get; set; } = null;

        #endregion

        #region Public Properties

        public IntegrationQueueModel QueueModel { get; set; }

        public TextWriter Log { get; set; }

        public IntegrationAssetType SynchedAssetType { get; set; }
        public IntegrationExecutionAssetType ExecutionAssetType { get; set; }
        public RetryLogModel ExecutionAssetTypeRetryLog { get; set; }

        public CommunityContext Community { get; set; }

        public CompanyContext Company { get; set; }

        #endregion

        public IgcIntegrationEngine()
        {
            PageBeginValueUpdated += IgcIntegrationEngine_PageBeginValueUpdated;
            PageErrorCaptured += IgcIntegrationEngine_PageErrorCaptured;
        }

        private void IgcIntegrationEngine_PageErrorCaptured(object sender, PageErrorCapturedEventArgs e)
        {
            try
            {
                string error = $"Status={e.StatusCode.ToString()}, Error={e.ErrorMessage}; ";
                if (!ExecutionAssetType.ErrorMessage.Contains(error))
                {
                    ExecutionAssetType.ErrorMessage += error;
                    Company.Update(ExecutionAssetType);
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                //Log.WriteLine(error);
            }
        }

        private void IgcIntegrationEngine_PageBeginValueUpdated(object sender, PageBeginValueUpdatedEventArgs e)
        {
            switch (e.Class)
            {
                case PageDataClass.Fields:
                    ExecutionAssetTypeRetryLog.Begins.Fields = e.Value;
                    break;
                case PageDataClass.Relations:
                    ExecutionAssetTypeRetryLog.Begins.Relations = e.Value;
                    break;
                case PageDataClass.Responsibilities:
                    ExecutionAssetTypeRetryLog.Begins.Responsibilities = e.Value;
                    break;
            }
            try
            {
                ExecutionAssetType.RetryLog = JsonConvert.SerializeObject(ExecutionAssetTypeRetryLog);
                Company.Update(ExecutionAssetType);
            }
            catch (Exception ex)
            {
            }
        }

        public void RunSingle()
        {
            var Caching = new DummyCachingProvider();
            var Queue = new AzureQueueSource();
            var Security = new UriSecurityContextProvider { IsAdministrator = true, ResourceID = 0 };
            Community = new CommunityContext(Caching, Queue, Security)
            {
                CurrentCompanyID = QueueModel.CompanyID,
                CurrentCompanyDomain = QueueModel.UrlPrefix
            };
            Security.CompanyID = QueueModel.CompanyID;
            Security.CompanyPrefix = QueueModel.UrlPrefix;
            Company = new CompanyContext(Community, Caching, Queue, Security, true);

            SynchedAssetType = Company.GetById<IntegrationAssetType>(QueueModel.SynchedAssetTypeID);
            ExecutionAssetType = Company.Filter<IntegrationExecutionAssetType>(i => i.ExecutionID == QueueModel.ExecutionID && i.SynchedAssetTypeID == QueueModel.SynchedAssetTypeID).Single();
            ExecutionAssetTypeRetryLog = JsonConvert.DeserializeObject<RetryLogModel>(ExecutionAssetType.RetryLog);

            var fields = Company.Filter<IntegrationAssetTypeFieldItem>(i => i.Active && i.SynchedAssetTypeID == QueueModel.SynchedAssetTypeID).ToList();
            var relations = Company.Filter<IntegrationAssetTypeRelationItem>(i => i.Active && i.SynchedAssetTypeID == QueueModel.SynchedAssetTypeID).ToList();
            var relationTargets = Company.Filter<IntegrationAssetTypeRelationItemTarget>(i => i.IntegrationAssetTypeRelationItem.Active && i.IntegrationAssetTypeRelationItem.SynchedAssetTypeID == QueueModel.SynchedAssetTypeID).ToList();
            var roles = Company.Filter<IntegrationAssetTypeRoleItem>(i => i.Active && i.SynchedAssetTypeID == QueueModel.SynchedAssetTypeID).ToList();

            #region Get global settings
            var setting = Company.GetById<IntegrationSetting>(QueueModel.IntegrationSettingID);
            string baseUri = setting.SourceUri;
            int defaultPageSize = setting.PageSize;
            int defaultRefreshInterval = setting.RefreshInterval;
            int defaultResourceID = setting.TargetResourceID;
            AuthenticationHeaderValue = $"Basic {Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(setting.SourceUser + ":" + setting.SourcePassword))}";
            setting = null;
            #endregion

            var now = DateTime.UtcNow;
            string url;


            #region Get type definition

            try
            {
                // First, get the type definition of the asset type, to pull enum values.
                url = $"{baseUri}types/{SynchedAssetType.SourceAssetTypeName}?showEditProperties=true";

                var igcType = GetFromApi<IgcTypeModel>(url).Result;

                if (igcType != null)
                {
                    var enumValues = new List<EnumResolutionModel>();

                    ExecutionAssetType.RawDefinition = JsonConvert.SerializeObject(igcType);

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
                        ExecutionAssetType.EnumFieldValues = JsonConvert.SerializeObject(enumValues);
                    }

                    Company.Update(ExecutionAssetType);
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
                LogoutUri = $"{baseUri}logout/";
                url = $"{baseUri}search/";

                // The raw sql connection to use for the specific company.
                var cnn = CompanyConnectionUtils.GetCompanyConnection(QueueModel.CompanyID);

                // Perform search using POST method.
                var postModel = new IgcPostSearchRequestModel();

                if (SynchedAssetType.AllowChangeDetection)
                {
                    postModel.sorts = new List<IgcPostSearchRequestSortModel>() {
                        new IgcPostSearchRequestSortModel { ascending = true, property = "modified_on" },
                        new IgcPostSearchRequestSortModel { ascending = true, property = "created_on" }
                    };
                }

                // Figure out page size
                var fieldPageSize = SynchedAssetType.FieldPageSize ?? defaultPageSize;
                var relationshipPageSize = SynchedAssetType.RelationshipPageSize ?? defaultPageSize;
                var ownershipPageSize = SynchedAssetType.OwnershipPageSize ?? defaultPageSize;

                postModel.types.Add(SynchedAssetType.SourceAssetTypeName);

                var errors = new List<string>();
                var errorBegins = new List<int?>();
                var hasRootError = false;   //Used to determine if we should run the stored proecdure called (Section=0) that actually deletes assets. If an error occured do not try to delete.

                #region Delta or Full Refresh

                var checkForChangesOnly = true;

                if (SynchedAssetType.AllowChangeDetection)
                {
                    // Continue to check if we should perform delta.
                    if (ExecutionAssetTypeRetryLog.LastRetryInError)
                    {
                        // If last retry was in error state, then pick up the refresh bool setting from the execution record as it was already set for the failed execution.
                        checkForChangesOnly = !ExecutionAssetType.IsFullRefresh;
                    }
                    else
                    {
                        var refreshInterval = SynchedAssetType.RefreshIntervalOverride ?? defaultRefreshInterval;

                        // If last refresh+interval > current time, then perform a delta instead, as you have not surpassed the refresh interval.
                        var lastFullRefreshDate = Company.Query<DateTime?>("select max(CompletedOn) from integration.ExecutionAssetType where SynchedAssetTypeID = @at and CompletedOn is not null and IsFullRefresh = 1", new { at = ExecutionAssetType.SynchedAssetTypeID }).SingleOrDefault();
                        checkForChangesOnly = lastFullRefreshDate.HasValue ? 
                            (lastFullRefreshDate.Value.AddHours(refreshInterval) > now) : 
                            false;
                    }
                }
                else
                {
                    // Delta checking not even allowed.
                    checkForChangesOnly = false;
                }

                var min = checkForChangesOnly ?
                    (ConvertDateToUnixTimeMilliseconds(SynchedAssetType.LastSynchOn ?? new DateTime(1970, 1, 1, 0, 0, 0))) :
                    0;
                var max = ConvertDateToUnixTimeMilliseconds(now);

#if DEBUG
                min = 0;
#endif

                if (SynchedAssetType.AllowChangeDetection)
                {
                    postModel.where.conditions.Add(new IgcPostSearchRequestBetweenConditionModel { min = min, max = max, property = "created_on" });
                    postModel.where.conditions.Add(new IgcPostSearchRequestBetweenConditionModel { min = min, max = max, property = "modified_on" });
                }

                //If starting from beginning, you are effectively doing a full refresh.
                if (min == 0)
                {
                    checkForChangesOnly = false;
                }

                ExecutionAssetType.IsFullRefresh = !checkForChangesOnly;

                #endregion

                Storage.CreateFolder($"igc-{Company.CurrentCompanyID}");
                var rootFolderName = $"{ExecutionAssetType.ExecutionID}.{ExecutionAssetType.SynchedAssetTypeID}"; // storage folder.

                Func<PageDataClass, bool> parsePostModel = delegate (PageDataClass c)
                {
                    int ps = 100;
                    int begin = 0;
                    List<string> selectFields = new List<string>();
                    switch (c)
                    {
                        case PageDataClass.Fields:
                            ps = fieldPageSize;
                            begin = ExecutionAssetTypeRetryLog.Begins.Fields;
                            selectFields = fields.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceField)).Select(i => i.SourceField).ToList();
                            break;
                        case PageDataClass.Relations:
                            ps = relationshipPageSize;
                            begin = ExecutionAssetTypeRetryLog.Begins.Relations;
                            selectFields = relations.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceField)).Select(i => i.SourceField).ToList();
                            break;
                        case PageDataClass.Responsibilities:
                            ps = ownershipPageSize;
                            begin = ExecutionAssetTypeRetryLog.Begins.Responsibilities;
                            selectFields = roles.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceIdField)).Select(i => i.SourceIdField).ToList();
                            break;
                    }
                    postModel.pageSize = ps;
                    postModel.begin = begin;
                    postModel.properties.Clear();
                    postModel.properties.AddRange(selectFields);
                    if (SynchedAssetType.AllowChangeDetection)
                    {
                        if (!postModel.properties.Contains("created_on")) postModel.properties.Add("created_on");
                        if (!postModel.properties.Contains("modified_on")) postModel.properties.Add("modified_on");
                    }

                    return true;
                };

                // Fields Request
                parsePostModel(PageDataClass.Fields);
                var igcReportedAssetCount = processAssetPages(postModel, Company.CurrentCompanyID, url, $"{rootFolderName}/fields", PageDataClass.Fields);
                ExecutionAssetType.CurrentSourceAssetCount = igcReportedAssetCount;
                Company.Update(ExecutionAssetType);

                // Relations request
                parsePostModel(PageDataClass.Relations);
                processAssetPages(postModel, Company.CurrentCompanyID, url, $"{rootFolderName}/relations", PageDataClass.Relations);
                
                // Ownership request
                parsePostModel(PageDataClass.Responsibilities);
                processAssetPages(postModel, Company.CurrentCompanyID, url, $"{rootFolderName}/owners", PageDataClass.Responsibilities);

                // Save to database staging tables.
                saveToEnvironmentDatabase(cnn, Company.CurrentCompanyID, ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID, Company.CurrentResourceID, !checkForChangesOnly);

                #region Load into the Field working table

                // Load JSON field data into execution Asset Field table
                cnn.Execute(
                    @"delete F from integration.ExecutionAssetField F inner join integration.ExecutionAsset A on A.ExecutionID = @ExecutionID and A.SynchedAssetTypeID = @SynchedAssetTypeID and A.[Uid] = F.[Uid]", 
                    new { ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID }, 
                    commandTimeout: 3600
                );

                // Load JSON field data into execution Asset Field table
                cnn.Execute(@"
insert into integration.ExecutionAssetField
	select	EA.Uid,
			1 as Section,
			RF.[key] as FieldName,
			RF.[value] as FieldValue
	from	integration.ExecutionAsset EA
			cross apply OPENJSON(EA.RawObject) RF	
	where	EA.ExecutionID = @ExecutionID and EA.SynchedAssetTypeID = @SynchedAssetTypeID
", new { ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID }, commandTimeout: 3600);

                // Load JSON relationship data into execution Asset Field table
                cnn.Execute(@"
insert into integration.ExecutionAssetField
	select	EA.Uid,
			2 as Section,
			RF.[key] as FieldName,
			RIF.items as FieldValue
	from	integration.ExecutionAsset EA
			cross apply OPENJSON(EA.RawRelationships) RF	
			inner join [integration].[SynchedAssetTypeRelationItem] R on R.SynchedAssetTypeID = EA.SynchedAssetTypeID and R.[SourceField] = RF.[key] COLLATE DATABASE_DEFAULT and RF.[key] is not null
			outer apply OPENJSON(RF.[value]) with (items nvarchar(max) '$.items' as json) RIF
	where	EA.ExecutionID = @ExecutionID and EA.SynchedAssetTypeID = @SynchedAssetTypeID
			and EA.RawRelationships is not null 
			and RIF.items <> '[]'
", new { ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID }, commandTimeout: 3600);

                // Load JSON responsibility data into execution Asset Field table
                cnn.Execute(@"
insert into integration.ExecutionAssetField
	select	EA.Uid,
			3 as Section,
			RF.[key] as FieldName,
			RF.[value] as FieldValue
	from	integration.ExecutionAsset EA
			cross apply OPENJSON(EA.RawResponsibilitites) RF	
			inner join [integration].[SynchedAssetTypeRoleItem] R on R.SynchedAssetTypeID = EA.SynchedAssetTypeID and R.SourceIdField = RF.[key] COLLATE DATABASE_DEFAULT
	where	EA.ExecutionID = @ExecutionID and EA.SynchedAssetTypeID = @SynchedAssetTypeID
			and EA.RawResponsibilitites is not null 
", new { ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID }, commandTimeout: 3600);

                #endregion  

                Func<int, DateTime> processStep = delegate (int section) {
                    DateTime start = DateTime.UtcNow;
                    try
                    {
                        Log.WriteLine($"Begin: Processing Section {section}");
                        if (section == 0)
                        {
                            if (!hasRootError && !checkForChangesOnly)
                            {
                                cnn.ProcessExecutionAssetType<dynamic>(ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID, SynchedAssetType.AssetTypeID, defaultResourceID, section);
                            }
                        }
                        else if (section == 2)
                        {
                            var relationshipActions = cnn.ProcessExecutionAssetType<RelationshipAction>(ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID, SynchedAssetType.AssetTypeID, defaultResourceID, 2);

                            #region Send Relationship Events

                            try
                            {
                                var queue = new AzureQueueSource();

                                var events = new List<EventInfo>();

                                if (SynchedAssetType.TriggerTopicMessage)
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
                                                CompanyID = Company.CurrentCompanyID,
                                                DomainPrefix = QueueModel.UrlPrefix,
                                                ResourceID = defaultResourceID,
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
                            }
                            catch (Exception wex)
                            {
                                Log.WriteLine($"Workflow error - Company: {QueueModel.CompanyID}, Exception: {wex.GetFullExceptionData()}");
                                CoreFunction.AITrackException(functionName, wex);
                            }

                            #endregion
                        }
                        else
                        {
                            cnn.ProcessExecutionAssetType<dynamic>(ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID, SynchedAssetType.AssetTypeID, defaultResourceID, section);
                        }
                    }
                    catch (Exception rex)
                    {
                        Log.WriteLine($"Error - Company: {QueueModel.CompanyID}, Exception: {rex.GetFullExceptionData()}");
                        CoreFunction.AITrackException(functionName, rex);
                    }
                    finally
                    {
                        DateTime end = DateTime.UtcNow;
                        Log.WriteLine($"End: Processing Section {section}. Took {end.Subtract(start).Minutes} minutes, {end.Subtract(start).Seconds} seconds.");
                    }

                    return DateTime.UtcNow;
                };

                processStep(0); // Section 0 : Asset
                processStep(1); // Section 1 : Fields
                processStep(2); // Section 2 : Relationships
                processStep(3); // Section 3 : Responsibilities
                processStep(4); // Section 4 : Capture metrics for this run

                // Set the last synch time so we can start the next delta check from this date.
                SynchedAssetType.LastSynchOn = now;
                ExecutionAssetTypeRetryLog.LastRetryInError = false;
            }
            catch (Exception oex)
            {
                try
                {
                    ExecutionAssetTypeRetryLog.LastRetryInError = true;
                    ExecutionAssetTypeRetryLog.RetryCount++;

                    ExecutionAssetType.ProcessedDelete = true;
                    ExecutionAssetType.ErrorMessage += oex.GetFullExceptionData();
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
                    if (!ExecutionAssetTypeRetryLog.LastRetryInError)
                    {
                        ExecutionAssetType.CompletedOn = DateTime.UtcNow;
                    }
                    if (ExecutionAssetTypeRetryLog.RetryCount > 10) // Only allow X retries.
                    {
                        ExecutionAssetType.CompletedOn = DateTime.UtcNow;
                    }

                    ExecutionAssetType.RetryLog = JsonConvert.SerializeObject(ExecutionAssetTypeRetryLog);
                    Company.Update(ExecutionAssetType);

                    if (ExecutionAssetTypeRetryLog.LastRetryInError && ExecutionAssetTypeRetryLog.RetryCount < 10)
                    {
                        Queue.CreateMessage(CoreFunction.GetConfigValueByKey("IntegrationQueue"), QueueModel);
                    }
                }
                catch (Exception cex)
                {
                    CoreFunction.AITrackException(functionName, cex);
                }
            }
        }

        #region Generic

        void saveToEnvironmentDatabase(SqlConnection cnn, int companyID, long executionID, int synchedAssetTypeID, int resourceID, bool fullRefresh)
        {
            var rootFolder = $"igc-{companyID}";
            var assetFolderName = $"{executionID}.{synchedAssetTypeID}"; // storage folder.
            var path = "";
            List<StorageFileInfo> pages = null;

            var list = new HashSet<IntegrationExecutionAsset>();

            path = $"{rootFolder}/{assetFolderName}/fields";
            pages = Storage.ListFiles(path);

            pages.ForEach(p => {
                IgcDynamicArrayModels page = JsonConvert.DeserializeObject<IgcDynamicArrayModels>(Storage.GetFileContentsAsString(path, p.Name));
                if (page.items.Count > 0)
                {
                    foreach (var obj in page.items.Children())
                    {
                        var sourceID = obj["_id"].Value<string>();

                        IntegrationExecutionAsset executionAsset = list.SingleOrDefault(i => i.SourceID == sourceID);
                        if (executionAsset == null)
                        {
                            executionAsset = new IntegrationExecutionAsset
                            {
                                ExecutionID = executionID,
                                SynchedAssetTypeID = synchedAssetTypeID,
                                SourceID = sourceID,
                                RawObject = JsonConvert.SerializeObject(obj, Formatting.None, new DecimalJsonConverter())
                            };
                            list.Add(executionAsset);
                        }
                    }
                }
            });

            path = $"{rootFolder}/{assetFolderName}/owners";
            pages = Storage.ListFiles(path);

            pages.ForEach(p => {
                IgcDynamicArrayModels page = JsonConvert.DeserializeObject<IgcDynamicArrayModels>(Storage.GetFileContentsAsString(path, p.Name));
                if (page.items.Count > 0)
                {
                    foreach (var obj in page.items.Children())
                    {
                        var sourceID = obj["_id"].Value<string>();

                        IntegrationExecutionAsset executionAsset = list.SingleOrDefault(i => i.SourceID == sourceID);

                        if (executionAsset != null)
                        {
                            executionAsset.RawResponsibilitites = JsonConvert.SerializeObject(obj, Formatting.None, new DecimalJsonConverter());
                        }
                        else
                        {
                            executionAsset = new IntegrationExecutionAsset
                            {
                                ExecutionID = executionID,
                                SynchedAssetTypeID = synchedAssetTypeID,
                                SourceID = sourceID,
                                RawResponsibilitites = JsonConvert.SerializeObject(obj, Formatting.None, new DecimalJsonConverter())
                            };
                            list.Add(executionAsset);
                        }
                    }
                }
            });

            path = $"{rootFolder}/{assetFolderName}/relations";
            pages = Storage.ListFiles(path);

            pages.ForEach(p => {
                IgcDynamicArrayModels page = JsonConvert.DeserializeObject<IgcDynamicArrayModels>(Storage.GetFileContentsAsString(path, p.Name));
                if (page.items.Count > 0)
                {
                    foreach (var obj in page.items.Children())
                    {
                        var sourceID = obj["_id"].Value<string>();

                        IntegrationExecutionAsset executionAsset = list.SingleOrDefault(i => i.SourceID == sourceID);

                        if (executionAsset != null)
                        {
                            executionAsset.RawRelationships = JsonConvert.SerializeObject(obj, Formatting.None, new DecimalJsonConverter());
                        }
                        else
                        {

                            executionAsset = new IntegrationExecutionAsset
                            {
                                ExecutionID = executionID,
                                SynchedAssetTypeID = synchedAssetTypeID,
                                SourceID = sourceID,
                                RawRelationships = JsonConvert.SerializeObject(obj, Formatting.None, new DecimalJsonConverter())
                            };
                            list.Add(executionAsset);
                        }
                    }
                }
            });

            #region Save to database via Bulk Insert

            var assetTable = new System.Data.DataTable();

            assetTable.Columns.Add("Uid", typeof(Guid));

            assetTable.Columns.Add("ExecutionID", typeof(long));
            assetTable.Columns.Add("SynchedAssetTypeID", typeof(int));
            assetTable.Columns.Add("SourceID", typeof(string));

            assetTable.Columns.Add("RawObject", typeof(string));
            assetTable.Columns.Add("RawRelationships", typeof(string));
            assetTable.Columns.Add("RawResponsibilitites", typeof(string));

            assetTable.Columns.Add("ErrorMessages", typeof(string));

            var loadedAssetIDs = new List<string>();
            foreach(var a in list)//list.ForEach(a =>
            {
                if (!loadedAssetIDs.Contains(a.SourceID))
                {
                    loadedAssetIDs.Add(a.SourceID);

                    var row = assetTable.NewRow();

                    row["Uid"] = a.Uid;

                    row["ExecutionID"] = a.ExecutionID;
                    row["SynchedAssetTypeID"] = a.SynchedAssetTypeID;
                    row["SourceID"] = a.SourceID;

                    row["RawObject"] = a.RawObject;
                    row["RawRelationships"] = a.RawRelationships;
                    row["RawResponsibilitites"] = a.RawResponsibilitites;

                    row["ErrorMessages"] = a.ErrorMessages;

                    assetTable.Rows.Add(row);
                }
            }//);

            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);


            using (var trans = cnn.BeginTransaction())
            {
                try
                {
                    if (fullRefresh)
                    {
                        cnn.Execute("delete integration.ExecutionAsset where SynchedAssetTypeID = @at", new { at = synchedAssetTypeID }, transaction: trans, commandTimeout: 3600);
                    }

                    using (var assetBulkCopy = new SqlBulkCopy(cnn, SqlBulkCopyOptions.Default, trans))
                    {
                        assetBulkCopy.BatchSize = 5000; //assetTable.Rows.Count;
                        assetBulkCopy.DestinationTableName = "[integration].[ExecutionAsset]";
                        assetBulkCopy.BulkCopyTimeout = 3600;

                        assetBulkCopy.ColumnMappings.Add("Uid", "Uid");

                        assetBulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        assetBulkCopy.ColumnMappings.Add("SynchedAssetTypeID", "SynchedAssetTypeID");
                        assetBulkCopy.ColumnMappings.Add("SourceID", "SourceID");

                        assetBulkCopy.ColumnMappings.Add("RawObject", "RawObject");
                        assetBulkCopy.ColumnMappings.Add("RawRelationships", "RawRelationships");
                        assetBulkCopy.ColumnMappings.Add("RawResponsibilitites", "RawResponsibilitites");

                        assetBulkCopy.ColumnMappings.Add("ErrorMessages", "ErrorMessages");

                        assetBulkCopy.WriteToServer(assetTable);

                        trans.Commit();
                    }
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw ex;
                }
            }

            #endregion
        }

        int processAssetPages(IgcPostSearchRequestModel postModel, int companyID, string url, string folderName, PageDataClass pageDataClass)
        {
            #region First remove any files that may be been there before.
            try
            {
                var itemsToRemove = Storage.ListFiles($"igc-{companyID}/{folderName}");
                itemsToRemove.ForEach(f => {
                    var fileNameBeginRawValue = f.Name.Replace(".json", "").Replace("_error", "");
                    int fileNameBeginValue;
                    if (int.TryParse(fileNameBeginRawValue, out fileNameBeginValue))
                    {
                        if (fileNameBeginValue >= postModel.begin)
                        {
                            Storage.DeleteFile($"igc-{companyID}/{folderName}", f.Name);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
            #endregion

            var igcCount = 0;
            var fShouldContinue = true;
            while (fShouldContinue)
            {
                try
                {
                    var models = PostJsonToApiAsync<IgcDynamicArrayModels>(url, JsonConvert.SerializeObject(postModel)).Result;
                    if (models != null)
                    {
                        if (igcCount == 0)
                        {
                            igcCount = models.paging.numTotal;
                        }
                        // serialize JSON directly to a file
                        Storage.CreateFile(
                            $"igc-{companyID}", 
                            $@"{folderName}/{postModel.begin}.json", 
                            JsonConvert.SerializeObject(models, Formatting.None, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
                        );
                        //Storage.CreateFile($"igc-{companyID}", $@"{postModel.begin}.json", JsonConvert.SerializeObject(models));
                        OnPageBeginValueUpdated(new PageBeginValueUpdatedEventArgs { Class = pageDataClass, Value = models.paging.end + 1 });
                        fShouldContinue = (models.paging.numTotal > models.paging.end + 1);
                        postModel.begin = models.paging.end + 1;
                    }
                    else
                    {
                        // Move onto next page.
                        postModel.begin = postModel.begin + postModel.pageSize;
                        if (postModel.begin >= igcCount)
                        {
                            fShouldContinue = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Storage.CreateFile($"igc-{companyID}", $@"{folderName}/{postModel.begin}_error.json", JsonConvert.SerializeObject(ex, Formatting.None, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));

                    // Move onto next page.
                    postModel.begin = postModel.begin + postModel.pageSize;
                    if (postModel.begin >= igcCount)
                    {
                        fShouldContinue = false;
                    }
                }
            }

            return igcCount;
        }

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

                // First, check to see if we got an error back.
                var errorModel = JsonConvert.DeserializeObject<IgcPageErrorModel>(jsonToReturn, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });

                var model = default(T);
                
                if ((int)errorModel.code > 0)
                {
                    // If error, then post event stating there was one.
                    OnPageErrorCaptured(new PageErrorCapturedEventArgs { ErrorMessage = errorModel.message, StatusCode = errorModel.code });
                }
                else
                {
                    // If no error, then continue to deserialize.
                    model = JsonConvert.DeserializeObject<T>(jsonToReturn, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
                }

                return model;
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

    /// <summary>
    /// GOV-5373: Removes trailing .0 on any inferred numbers.
    /// </summary>
    internal class DecimalJsonConverter : JsonConverter
    {
        public DecimalJsonConverter()
        {
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(decimal) || objectType == typeof(float) || objectType == typeof(double));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (DecimalJsonConverter.IsWholeValue(value))
            {
                writer.WriteRawValue(JsonConvert.ToString(Convert.ToInt64(value)));
            }
            else
            {
                writer.WriteRawValue(JsonConvert.ToString(value));
            }
        }

        private static bool IsWholeValue(object value)
        {
            if (value is decimal)
            {
                decimal decimalValue = (decimal)value;
                int precision = (Decimal.GetBits(decimalValue)[3] >> 16) & 0x000000FF;
                return precision == 0;
            }
            else if (value is float || value is double)
            {
                double doubleValue = (double)value;
                return doubleValue == Math.Truncate(doubleValue);
            }

            return false;
        }
    }
}
