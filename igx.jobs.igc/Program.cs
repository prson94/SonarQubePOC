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
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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
#if DEBUG
            config.UseDevelopmentSettings();
#endif
            var host = new JobHost(config);
            host.RunAndBlock();
        }

    }

    public static class IgcIntegration
    {
#if DEBUG
        [Disable]
#endif
        public static void RunScheduleViaTimer([TimerTrigger("0 */5 * * * *", RunOnStartup = true)]TimerInfo myTimer, CancellationToken token, TextWriter log)
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
                var companies = CoreFunction.GetCompaniesByCurrentSlot().Where(i => i.CompanyID == 122).ToList();
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
update  T
set     T.CompletedOn = getutcdate()
from    integration.ExecutionAssetType T
        cross apply openjson(T.RetryLog) with (RetryCount int '$.RetryCount') R 
where   T.CompletedOn is null
        and R.RetryCount >= 10");

                                cnn.Execute(@"
declare @dt datetime
set @dt = getutcdate()

update  T
set     T.CompletedOn = getutcdate()
from	integration.ExecutionAssetType T
		inner join integration.SynchedAssetType A on A.ID = T.SynchedAssetTypeID
		inner join integration.Setting S on S.ID = A.IntegrationSettingID
where	T.CompletedOn is null
		and T.StartedOn < DATEADD(hh, -(coalesce(A.DeleteExecutionTimeoutHours, S.DeleteExecutionTimeoutHours)), @dt)", new List<SqlParameter>());
                            }
                            catch (Exception cex)
                            {
                                log.WriteLine($"Unable to remove old execution asset types for company ({c.CompanyID}) due to the following error: {cex.GetFullExceptionData(false)}"); ;
                            }
                            finally
                            {
                                if (cnn != null)
                                    cnn.Dispose();
                            }

                            #endregion
                        }

                        foreach (var setting in settings)
                        {
                            var now = DateTime.UtcNow;

                            if (mappings.Any(i => i.Active && i.IntegrationSettingID == setting.ID && i.ObjectID.HasValue))
                            {
                                var assetsToAvoid = company.Query<int>(@"
select SynchedAssetTypeID as ID from integration.ExecutionAssetType where CompletedOn is null
union
select	distinct
		S.ID 
from	integration.SynchedAssetType S
		inner join integration.ExecutionAssetType E on E.SynchedAssetTypeID = S.ID and E.CompletedOn > DATEADD(hh, -coalesce(S.RefreshIntervalOverride,1), getutcdate())
where	[AllowChangeDetection] = 0").ToList();

                                if (assetsToAvoid.Count > 0)
                                {
                                    log.WriteLine($"Avoiding assets: {string.Join(", ", assetsToAvoid)}");
                                }

                                long executionID = 0;

                                if (mappings.Any(i => !assetsToAvoid.Contains(i.ID)))
                                {
                                    if (executionID == 0)
                                    {
                                        executionID = company.Query<long>("select NEXT VALUE for integration.Execution_Seq").Single();
                                    }

                                    foreach (var item in mappings.Where(i => i.IntegrationSettingID == setting.ID && !assetsToAvoid.Contains(i.ID) && i.ObjectID.HasValue))
                                    {
                                        var atExecution = new IntegrationExecutionAssetType { StartedOn = now, SynchedAssetTypeID = item.ID, ExecutionID = executionID };
                                        company.Add(atExecution);
                                        var queueModel = new IntegrationQueueModel
                                        {
                                            CompanyID = c.CompanyID,
                                            ExecutionID = executionID,
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

                            // See if you need to requeue based on DelayUntil flag.
                            var delayedExecutions = company.Filter<IntegrationExecutionAssetType>(i => i.SynchedAssetType.IntegrationSettingID == setting.ID && i.DelayUntil <= DateTime.UtcNow).ToList();

                            if (delayedExecutions.Count > 0)
                            {
                                foreach (var delayedExecution in delayedExecutions)
                                {
                                    delayedExecution.DelayUntil = null;
                                }
                                company.SaveChanges();

                                foreach (var item in delayedExecutions)
                                {
                                    var queueModel = new IntegrationQueueModel
                                    {
                                        CompanyID = c.CompanyID,
                                        ExecutionID = item.ExecutionID,
                                        IntegrationSettingID = setting.ID,
                                        SynchedAssetTypeID = item.SynchedAssetTypeID,
                                        To = QueueAction.Integration,
                                        UrlPrefix = c.UrlPrefix
                                    };
                                    Queue.CreateMessage(CoreFunction.GetConfigValueByKey("IntegrationQueue"), queueModel);
                                    log.WriteLine($"Queued PREVIOUSLY DELAYED execution {item.ExecutionID}, asset {item.SynchedAssetTypeID}, full refresh {(item.IsFullRefresh ? "Yes" : "No")}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.WriteLine($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData(false)}]");
                    }
                });

            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.WriteLine($"General Exception: {ex.GetFullExceptionData(false)}");
            }

            CoreFunction.AIFlush();
        }

#if DEBUG
        public static void RunViaQueue([TimerTrigger("0 */5 * * * *", RunOnStartup = true)]TimerInfo myTimer, CancellationToken token, TextWriter log)
#else
        public static void RunViaQueue([QueueTrigger("%IntegrationQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
#endif
        {
            CoreFunction.AppInsightsInstrumentationKey(CoreFunction.GetConfigValueByKey("IGC_APPINSIGHTS_INSTRUMENTATIONKEY"));
#if DEBUG
            var queueModel = new IntegrationQueueModel { CompanyID = 1, ExecutionID = 50003, IntegrationSettingID = 1, SynchedAssetTypeID = 1, To = QueueAction.Integration, UrlPrefix = "integration.eng" };
#else
            var queueModel = JsonConvert.DeserializeObject<IntegrationQueueModel>(myQueueItem);
#endif
            var engine = new IgcIntegrationEngine();
            engine.Log = log;
            engine.QueueModel = queueModel;
            engine.RunSingle();
        }
    }

    public class IgcIntegrationEngine
    {
        #region Events

        public event EventHandler<PageBeginValueUpdatedEventArgs> PageBeginValueUpdated;
        protected virtual void OnPageBeginValueUpdated(PageBeginValueUpdatedEventArgs e)
        {
            PageBeginValueUpdated?.Invoke(this, e);
        }

        public event EventHandler<PageProcessedInGovernUpdatedEventArgs> PageProcessedInGovernUpdated;
        protected virtual void OnPageProcessedInGovernUpdated(PageProcessedInGovernUpdatedEventArgs e)
        {
            PageProcessedInGovernUpdated?.Invoke(this, e);
        }

        public event EventHandler<PageErrorCapturedEventArgs> PageErrorCaptured;
        protected virtual void OnPageErrorCaptured(PageErrorCapturedEventArgs e)
        {
            PageErrorCaptured?.Invoke(this, e);
        }

        public event EventHandler<StepStartedEventArgs> StepStarted;
        protected virtual void OnStepStarted(StepStartedEventArgs e)
        {
            StepStarted?.Invoke(this, e);
        }

        public event EventHandler<StepCompletedEventArgs> StepCompleted;
        protected virtual void OnStepCompleted(StepCompletedEventArgs e)
        {
            StepCompleted?.Invoke(this, e);
        }

        public event EventHandler<RelationshipBreakdownModelsUpdatedEventArgs> RelationshipBreakdownModelsUpdated;
        protected virtual void OnRelationshipBreakdownModelsUpdated(RelationshipBreakdownModelsUpdatedEventArgs e)
        {
            RelationshipBreakdownModelsUpdated?.Invoke(this, e);
        }

        public event EventHandler<ResponsibilityBreakdownModelsUpdatedEventArgs> ResponsibilityBreakdownModelsUpdated;
        protected virtual void OnResponsibilityBreakdownModelsUpdated(ResponsibilityBreakdownModelsUpdatedEventArgs e)
        {
            ResponsibilityBreakdownModelsUpdated?.Invoke(this, e);
        }

        public event EventHandler<EventArgs> PageBreakdownsUpdated;
        protected virtual void OnPageBreakdowns(EventArgs e)
        {
            PageBreakdownsUpdated?.Invoke(this, e);
        }

        #endregion

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
        public List<StepExecutionTime> StepExecutionTimes { get; set; }
        public IGCAssetRelationshipBreakdownModels RelationshipBreakdownModels { get; set; }
        public IGCAssetResponsibilityBreakdownModels ResponsibilityBreakdownModels { get; set; }
        public List<RelationshipTargetComparisonModel> RelationshipTargetComparisons { get; set; }


        public CommunityContext Community { get; set; }

        public CompanyContext Company { get; set; }

        public int DefaultResourceID { get; set; }

        public bool AutoTrustServerCertificate { get; set; }
        public bool RemoveUriPortOnConnect { get; set; }

        #endregion

        #region ctor

        public IgcIntegrationEngine()
        {
            PageBeginValueUpdated += IgcIntegrationEngine_PageBeginValueUpdated;
            PageProcessedInGovernUpdated += IgcIntegrationEngine_PageProcessedInGovernUpdated;
            PageErrorCaptured += IgcIntegrationEngine_PageErrorCaptured;
            StepStarted += IgcIntegrationEngine_StepStarted;
            StepCompleted += IgcIntegrationEngine_StepCompleted;
            RelationshipBreakdownModelsUpdated += IgcIntegrationEngine_RelationshipBreakdownModelsUpdated;
            ResponsibilityBreakdownModelsUpdated += IgcIntegrationEngine_ResponsibilityBreakdownModelsUpdated;
            PageBreakdownsUpdated += IgcIntegrationEngine_PageBreakdownsUpdated;
        }

        #endregion

        #region Event Handlers

        private void IgcIntegrationEngine_PageErrorCaptured(object sender, PageErrorCapturedEventArgs e)
        {
            try
            {
                if (e != null)
                {
                    string error = $"Status={e.StatusCode}";
                    if (!string.IsNullOrEmpty(e.ErrorMessage))
                        error += $", Error={e.ErrorMessage}; ";
                    if (ExecutionAssetType.ErrorMessage == null)
                    {
                        ExecutionAssetType.ErrorMessage = "";
                    }
                    if (!ExecutionAssetType.ErrorMessage.Contains(error))
                    {
                        ExecutionAssetType.ErrorMessage += error;
                        Company.Update(ExecutionAssetType);
                    }
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
            catch
            {
            }
        }

        private void IgcIntegrationEngine_PageProcessedInGovernUpdated(object sender, PageProcessedInGovernUpdatedEventArgs e)
        {
            switch (e.Class)
            {
                case PageDataClass.Fields:
                    ExecutionAssetTypeRetryLog.Begins.ProcessedFieldsPage = e.Value;
                    break;
                case PageDataClass.Relations:
                    ExecutionAssetTypeRetryLog.Begins.ProcessedRelationsPage = e.Value;
                    break;
                case PageDataClass.Responsibilities:
                    ExecutionAssetTypeRetryLog.Begins.ProcessedResponsibilitiesPage = e.Value;
                    break;
            }
            try
            {
                ExecutionAssetType.RetryLog = JsonConvert.SerializeObject(ExecutionAssetTypeRetryLog);
                Company.Update(ExecutionAssetType);
            }
            catch
            {
            }
        }

        private void IgcIntegrationEngine_StepStarted(object sender, StepStartedEventArgs e)
        {
            try
            {
                var execStep = StepExecutionTimes.FirstOrDefault(i => i.Step == e.Step);
                if (execStep == null)
                {
                    StepExecutionTimes.Add(new StepExecutionTime { Step = e.Step, StartedOn = DateTime.UtcNow, CompletedOn = DateTime.UtcNow });
                }
                else
                {
                    execStep.StartedOn = DateTime.UtcNow;
                }
                ExecutionAssetType.StepExecutionTimes = JsonConvert.SerializeObject(StepExecutionTimes);
                Company.Update(ExecutionAssetType);
            }
            catch(Exception ex)
            {
            }
        }

        private void IgcIntegrationEngine_StepCompleted(object sender, StepCompletedEventArgs e)
        {
            ExecutionAssetTypeRetryLog.LastStepCompleted = e.Step;
            try
            {
                var execStep = StepExecutionTimes.FirstOrDefault(i => i.Step == e.Step);
                if (execStep == null)
                {
                    StepExecutionTimes.Add(new StepExecutionTime { Step = e.Step, StartedOn = DateTime.UtcNow, CompletedOn = DateTime.UtcNow });
                }
                else
                {
                    execStep.CompletedOn = DateTime.UtcNow;
                }
                ExecutionAssetType.StepExecutionTimes = JsonConvert.SerializeObject(StepExecutionTimes);
                ExecutionAssetType.RetryLog = JsonConvert.SerializeObject(ExecutionAssetTypeRetryLog);
                Company.Update(ExecutionAssetType);
            }
            catch (Exception ex)
            {
            }
        }

        private void IgcIntegrationEngine_RelationshipBreakdownModelsUpdated(object sender, RelationshipBreakdownModelsUpdatedEventArgs e)
        {
            foreach (var model in e.Updates)
            {
                if (RelationshipBreakdownModels.Any(i => i.FieldName == model.FieldName && i.AssetTypeName == model.AssetTypeName && i.IntersectTypeID == model.IntersectTypeID))
                {
                    RelationshipBreakdownModels.Single(i => i.FieldName == model.FieldName && i.AssetTypeName == model.AssetTypeName && i.IntersectTypeID == model.IntersectTypeID).Count += model.Count;
                }
                else
                {
                    RelationshipBreakdownModels.Add(model);
                }
            }
        }

        private void IgcIntegrationEngine_ResponsibilityBreakdownModelsUpdated(object sender, ResponsibilityBreakdownModelsUpdatedEventArgs e)
        {
            if (ResponsibilityBreakdownModels.Any(i => i.Role == e.Update.Role))
            {
                ResponsibilityBreakdownModels.Single(i => i.Role == e.Update.Role).Count += e.Update.Count;
            }
            else
            {
                ResponsibilityBreakdownModels.Add(e.Update);
            }
        }

        private void IgcIntegrationEngine_PageBreakdownsUpdated(object sender, EventArgs e)
        {
            try
            {
                ExecutionAssetType.IGCAssetRelationshipBreakdown = JsonConvert.SerializeObject(RelationshipBreakdownModels);
                ExecutionAssetType.IGCAssetResponsibilityBreakdown = JsonConvert.SerializeObject(ResponsibilityBreakdownModels);
                Company.Update(ExecutionAssetType);
            }
            catch
            {
            }
        }


        #endregion

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

            #region Set common properties that we will work with in many methods

            SynchedAssetType = Company.GetById<IntegrationAssetType>(QueueModel.SynchedAssetTypeID);

            writeLogEntry("Getting common data", 0);

            ExecutionAssetType = Company.Filter<IntegrationExecutionAssetType>(i => i.ExecutionID == QueueModel.ExecutionID && i.SynchedAssetTypeID == QueueModel.SynchedAssetTypeID).Single();
            ExecutionAssetTypeRetryLog = JsonConvert.DeserializeObject<RetryLogModel>(ExecutionAssetType.RetryLog);
            StepExecutionTimes = JsonConvert.DeserializeObject<List<StepExecutionTime>>(ExecutionAssetType.StepExecutionTimes ?? "[]");
            RelationshipBreakdownModels = string.IsNullOrEmpty(ExecutionAssetType.IGCAssetRelationshipBreakdown) ? new IGCAssetRelationshipBreakdownModels() : JsonConvert.DeserializeObject<IGCAssetRelationshipBreakdownModels>(ExecutionAssetType.IGCAssetRelationshipBreakdown);
            ResponsibilityBreakdownModels = string.IsNullOrEmpty(ExecutionAssetType.IGCAssetResponsibilityBreakdown) ? new IGCAssetResponsibilityBreakdownModels() : JsonConvert.DeserializeObject<IGCAssetResponsibilityBreakdownModels>(ExecutionAssetType.IGCAssetResponsibilityBreakdown);

            var Fields = Company.Filter<IntegrationAssetTypeFieldItem>(i => i.Active && i.SynchedAssetTypeID == QueueModel.SynchedAssetTypeID).ToList();
            var Relations = Company.Filter<IntegrationAssetTypeRelationItem>(i => i.Active && i.SynchedAssetTypeID == QueueModel.SynchedAssetTypeID).ToList();
            var RelationTargets = Company.Filter<IntegrationAssetTypeRelationItemTarget>(i => i.IntegrationAssetTypeRelationItem.Active && i.IntegrationAssetTypeRelationItem.SynchedAssetTypeID == QueueModel.SynchedAssetTypeID).ToList();
            var Roles = Company.Filter<IntegrationAssetTypeRoleItem>(i => i.Active && i.SynchedAssetTypeID == QueueModel.SynchedAssetTypeID).ToList();

            RelationshipTargetComparisons = (
                                            from rel in Relations
                                            join tar in RelationTargets on rel.ID equals tar.SynchedAssetTypeRelationItemID
                                            select new RelationshipTargetComparisonModel
                                            {
                                                SourceField = rel.SourceField,
                                                SourceAssetType = tar.SourceAssetType,
                                                IntersectTypeID = tar.IntersectTypeID
                                            }).ToList();

            #endregion

            // Reset the error message value to NULL (to avoid alerting in DQ+), b/c this could be a restart.
            ExecutionAssetType.ErrorMessage = null;

            #region Get global settings

            var setting = Company.GetById<IntegrationSetting>(QueueModel.IntegrationSettingID);
            string baseUri = setting.SourceUri;
            int defaultPageSize = SynchedAssetType.PageSize ?? setting.PageSize;
            int defaultRefreshInterval = setting.RefreshInterval;
            DefaultResourceID = setting.TargetResourceID;
            AutoTrustServerCertificate = setting.AutoTrustServerCertificate;
            RemoveUriPortOnConnect = setting.RemoveUriPortOnConnect;
            AuthenticationHeaderValue = $"Basic {Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(setting.SourceUser + ":" + setting.SourcePassword))}";
            setting = null;

            LogoutUri = $"{baseUri}logout/";

            #endregion

            var now = DateTime.UtcNow;
            string url;
            int currentStep = 1;
            bool fatalError = false;

            // Create common client before connecting to any URI.
            createHttpClient();

            #region Get type definition: step (1)

            if (ExecutionAssetTypeRetryLog.LastStepCompleted < currentStep)
            {
                try
                {
                    OnStepStarted(new StepStartedEventArgs { Step = currentStep });

                    writeLogEntry($"Getting type definition for: {SynchedAssetType.SourceAssetTypeName}", currentStep);

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

                    OnStepCompleted(new StepCompletedEventArgs { Step = currentStep });
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
                    writeLogEntry(ex.GetFullExceptionData(true), currentStep, true);
                }
            }

            #endregion

            try
            {
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
                //var hasRootError = false;   //Used to determine if we should run the stored proecdure called (Section=0) that actually deletes assets. If an error occured do not try to delete.

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

                var rootFolderName = $"{Company.CurrentCompanyID}/{ExecutionAssetType.SynchedAssetTypeID}/{ExecutionAssetType.ExecutionID}"; // storage folder.

                #region Get data from IGC itself.

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
                            selectFields = Fields.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceField)).Select(i => i.SourceField).ToList();
                            break;
                        case PageDataClass.Relations:
                            ps = relationshipPageSize;
                            begin = ExecutionAssetTypeRetryLog.Begins.Relations;
                            selectFields = Relations.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceField)).Select(i => i.SourceField).ToList();
                            break;
                        case PageDataClass.Responsibilities:
                            ps = ownershipPageSize;
                            begin = ExecutionAssetTypeRetryLog.Begins.Responsibilities;
                            selectFields = Roles.Where(i => i.IncludeInPropertyRequest && !string.IsNullOrEmpty(i.SourceIdField)).Select(i => i.SourceIdField).ToList();
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

                // Fields Request : step (2)
                currentStep = 2;
                if (ExecutionAssetTypeRetryLog.LastStepCompleted < currentStep)
                {
                    OnStepStarted(new StepStartedEventArgs { Step = currentStep });
                    writeLogEntry($"BEGIN: Getting field data", currentStep);
                    parsePostModel(PageDataClass.Fields);
                    var igcReportedAssetCount = DownloadAndSavePageFromIgc(postModel, Company.CurrentCompanyID, url, $"{rootFolderName}/fields", PageDataClass.Fields);
                    ExecutionAssetType.CurrentSourceAssetCount = igcReportedAssetCount;
                    Company.Update(ExecutionAssetType);
                    writeLogEntry($"END: Getting field data", currentStep);
                    OnStepCompleted(new StepCompletedEventArgs { Step = currentStep });
                }

                // Relations Request : step (3)
                currentStep = 3;
                if (ExecutionAssetTypeRetryLog.LastStepCompleted < currentStep)
                {
                    OnStepStarted(new StepStartedEventArgs { Step = currentStep });
                    writeLogEntry($"BEGIN: Getting relationship data", currentStep);
                    parsePostModel(PageDataClass.Relations);
                    DownloadAndSavePageFromIgc(postModel, Company.CurrentCompanyID, url, $"{rootFolderName}/relations", PageDataClass.Relations);
                    writeLogEntry($"END: Getting relationship data", currentStep);
                    OnStepCompleted(new StepCompletedEventArgs { Step = currentStep });
                }

                // Ownership Request : step (4)
                currentStep = 4;
                if (ExecutionAssetTypeRetryLog.LastStepCompleted < currentStep)
                {
                    OnStepStarted(new StepStartedEventArgs { Step = currentStep });
                    writeLogEntry($"BEGIN: Getting responsibility data", currentStep);
                    parsePostModel(PageDataClass.Responsibilities);
                    DownloadAndSavePageFromIgc(postModel, Company.CurrentCompanyID, url, $"{rootFolderName}/owners", PageDataClass.Responsibilities);
                    writeLogEntry($"END: Getting responsibility data", currentStep);
                    OnStepCompleted(new StepCompletedEventArgs { Step = currentStep });
                }

                #endregion

                #region  Common set of variables we will use on next 3 steps below.

                int requestNumber = 0;
                int totalPageCount = 0;
                string path = "";
                List<StorageFileInfo> pages = null;
                var procedureCommand = "exec integration.ProcessExecutionAssetType @ExecutionID, @SynchedAssetTypeID, @requestNumber, @AssetTypeID, @r, @section";

                #endregion

                #region Field Data

                currentStep = 5;
                if (ExecutionAssetTypeRetryLog.LastStepCompleted < currentStep)
                {
                    OnStepStarted(new StepStartedEventArgs { Step = currentStep });

                    path = $"igc/{rootFolderName}/fields";
                    pages = Storage.ListFiles(path);
                    requestNumber = 1;
                    totalPageCount = pages.Count;

                    // Clear the execution fields to start.
                    if (totalPageCount > 0 && !ExecutionAssetTypeRetryLog.Begins.ProcessedFieldsPage.HasValue)
                    {
                        writeLogEntry($"Clear out ExecutionAssetField to start processing field data", currentStep);
                        cnn.Execute(@"delete integration.ExecutionAssetField where SynchedAssetTypeID = @SynchedAssetTypeID and Section = @section", new { ExecutionAssetType.SynchedAssetTypeID, section = 1 }, commandTimeout: 3600);
                        writeLogEntry($"Parsing {totalPageCount} page(s) of field data", currentStep);
                    }

                    foreach (var p in pages)
                    {
                        if ((ExecutionAssetTypeRetryLog.Begins.ProcessedFieldsPage.HasValue) ? requestNumber > ExecutionAssetTypeRetryLog.Begins.ProcessedFieldsPage.Value : true)
                        {
                            var json = Storage.GetFileContentsAsString(path, p.Name, Encoding.UTF8);
                            if (p.Name.Contains("_error"))
                            {
                                ParsePageSavedException(json);
                                fatalError = true;
                            }
                            else
                            {
                                var page = JsonConvert.DeserializeObject<IgcDynamicArrayModels>(json);

                                if (page.items.Count > 0)
                                {
                                    var hasChanges = ParseAndSaveAssetsOnIgcPage(cnn, page, 1, requestNumber, Fields.Select(i => i.SourceField).ToList());          // step (5)

                                    if (hasChanges)
                                    {
                                        if (cnn.State != System.Data.ConnectionState.Open)
                                            cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                                        // Section 0 : Asset
                                        writeLogEntry($"Executing section 0 of procedure for {SynchedAssetType.SourceAssetTypeName} - request number {requestNumber}", currentStep);
                                        cnn.Query<dynamic>(
                                            procedureCommand,
                                            new { ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID, requestNumber, SynchedAssetType.AssetTypeID, r = DefaultResourceID, section = 0 },
                                            commandTimeout: 3600);

                                        // Section 1 : Fields
                                        writeLogEntry($"Executing section 1 of procedure for {SynchedAssetType.SourceAssetTypeName} - request number {requestNumber}", currentStep);
                                        cnn.Query<dynamic>(
                                            procedureCommand,
                                            new { ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID, requestNumber, SynchedAssetType.AssetTypeID, r = DefaultResourceID, section = 1 },
                                            commandTimeout: 3600);
                                    }
                                }
                            }

                            OnPageProcessedInGovernUpdated(new PageProcessedInGovernUpdatedEventArgs { Class = PageDataClass.Fields, Value = requestNumber });
                        }
                        
                        requestNumber++;
                    }

                    OnStepCompleted(new StepCompletedEventArgs { Step = currentStep });
                }

                #endregion

                #region Relationship Data

                currentStep = 6;
                if (ExecutionAssetTypeRetryLog.LastStepCompleted < currentStep)
                {
                    OnStepStarted(new StepStartedEventArgs { Step = currentStep });

                    path = $"igc/{rootFolderName}/relations";
                    pages = Storage.ListFiles(path);
                    requestNumber = 1;
                    totalPageCount = pages.Count;

                    // Clear the execution fields to start.
                    if (totalPageCount > 0 && !ExecutionAssetTypeRetryLog.Begins.ProcessedRelationsPage.HasValue)
                    {
                        writeLogEntry($"Clear out ExecutionAssetField to start processing relationship data", currentStep);
                        cnn.Execute(@"delete integration.ExecutionAssetField where SynchedAssetTypeID = @SynchedAssetTypeID and Section = @section", new { ExecutionAssetType.SynchedAssetTypeID, section = 2 }, commandTimeout: 3600);
                        writeLogEntry($"Parsing {totalPageCount} page(s) of relationship data", currentStep);
                    }

                    foreach (var p in pages)
                    {
                        if ((ExecutionAssetTypeRetryLog.Begins.ProcessedRelationsPage.HasValue) ? requestNumber > ExecutionAssetTypeRetryLog.Begins.ProcessedRelationsPage.Value : true)
                        {
                            var json = Storage.GetFileContentsAsString(path, p.Name, Encoding.UTF8);
                            if (p.Name.Contains("_error"))
                            {
                                ParsePageSavedException(json);
                                fatalError = true;
                            }
                            else
                            {
                                var page = JsonConvert.DeserializeObject<IgcDynamicArrayModels>(json);
                                if (page.items.Count > 0)
                                {
                                    var hasChanges = ParseAndSaveAssetsOnIgcPage(cnn, page, 2, requestNumber, Relations.Select(i => i.SourceField).ToList());          // step (5)

                                    if (hasChanges)
                                    {
                                        if (cnn.State != System.Data.ConnectionState.Open)
                                            cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                                        // Section 2 : Relationships
                                        writeLogEntry($"Executing section 2 of procedure for {SynchedAssetType.SourceAssetTypeName} - request number {requestNumber}", currentStep);
                                        var relationshipActions = cnn.Query<RelationshipAction>(
                                            procedureCommand,
                                            new { ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID, requestNumber, SynchedAssetType.AssetTypeID, r = DefaultResourceID, section = 2 },
                                            commandTimeout: 14400);

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
                                                            ResourceID = DefaultResourceID,
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
                                            writeLogEntry($"Workflow error - Company: {QueueModel.CompanyID}, Exception: {wex.GetFullExceptionData(false)}", currentStep);
                                        }

                                        #endregion
                                    }
                                }
                            }

                            OnPageProcessedInGovernUpdated(new PageProcessedInGovernUpdatedEventArgs { Class = PageDataClass.Relations, Value = requestNumber });
                        }
                        
                        requestNumber++;
                    }

                    OnStepCompleted(new StepCompletedEventArgs { Step = currentStep });
                }

                #endregion

                #region Responsibility Data

                currentStep = 7;
                if (ExecutionAssetTypeRetryLog.LastStepCompleted < currentStep)
                {
                    OnStepStarted(new StepStartedEventArgs { Step = currentStep });

                    path = $"igc/{rootFolderName}/owners";
                    pages = Storage.ListFiles(path);
                    requestNumber = 1;
                    totalPageCount = pages.Count;

                    // Clear the execution fields to start.
                    if (totalPageCount > 0 && !ExecutionAssetTypeRetryLog.Begins.ProcessedResponsibilitiesPage.HasValue)
                    {
                        writeLogEntry($"Clear out ExecutionAssetField to start processing responsibility data", currentStep);
                        cnn.Execute(@"delete integration.ExecutionAssetField where SynchedAssetTypeID = @SynchedAssetTypeID and Section = @section", new { ExecutionAssetType.SynchedAssetTypeID, section = 3 }, commandTimeout: 3600);
                        writeLogEntry($"Parsing {totalPageCount} page(s) of responsibility data", currentStep);
                    }

                    foreach (var p in pages)
                    {
                        if ((ExecutionAssetTypeRetryLog.Begins.ProcessedResponsibilitiesPage.HasValue) ? requestNumber > ExecutionAssetTypeRetryLog.Begins.ProcessedResponsibilitiesPage.Value : true)
                        {
                            var json = Storage.GetFileContentsAsString(path, p.Name, Encoding.UTF8);
                            if (p.Name.Contains("_error"))
                            {
                                ParsePageSavedException(json);
                                fatalError = true;
                            }
                            else
                            {
                                var page = JsonConvert.DeserializeObject<IgcDynamicArrayModels>(json);

                                if (page.items != null)
                                {
                                    if (page.items.Count > 0)
                                    {
                                        var hasChanges = ParseAndSaveAssetsOnIgcPage(cnn, page, 3, requestNumber, Roles.Select(i => i.SourceIdField).ToList());

                                        if (hasChanges)
                                        {
                                            if (cnn.State != System.Data.ConnectionState.Open)
                                                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                                            // Section 3 : Fields
                                            writeLogEntry($"Executing section 3 of procedure for {SynchedAssetType.SourceAssetTypeName} - request number {requestNumber}", currentStep);
                                            cnn.Query<dynamic>(
                                                procedureCommand,
                                                new { ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID, requestNumber, SynchedAssetType.AssetTypeID, r = DefaultResourceID, section = 3 },
                                                commandTimeout: 7200);
                                        }
                                    }
                                }
                            }

                            OnPageProcessedInGovernUpdated(new PageProcessedInGovernUpdatedEventArgs { Class = PageDataClass.Responsibilities, Value = requestNumber });
                        }
                        
                        requestNumber++;
                    }

                    OnStepCompleted(new StepCompletedEventArgs { Step = currentStep });
                }

                #endregion

                #region Metrics

                currentStep = 8;
                if (ExecutionAssetTypeRetryLog.LastStepCompleted < currentStep)
                {
                    OnStepStarted(new StepStartedEventArgs { Step = currentStep });

                    Log.WriteLine($"Process metrics");

                    if (cnn.State != System.Data.ConnectionState.Open)
                        cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                    // Gather metric on this run
                    writeLogEntry($"Executing section 4 of procedure for {SynchedAssetType.SourceAssetTypeName} - request number {requestNumber}", currentStep);
                    cnn.Query<dynamic>(
                        procedureCommand,
                        new { ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID, requestNumber, SynchedAssetType.AssetTypeID, r = DefaultResourceID, section = 4 },
                        commandTimeout: 3600);

                    OnStepCompleted(new StepCompletedEventArgs { Step = currentStep });
                }

                #endregion

                #region Perform asset deletions, if full refresh

                currentStep = 9;
                if (ExecutionAssetTypeRetryLog.LastStepCompleted < currentStep && !checkForChangesOnly && !fatalError)
                {
                    OnStepStarted(new StepStartedEventArgs { Step = currentStep });

                    writeLogEntry($"Process deletions", currentStep);

                    // You can do a delete based on asset hash missing attributes.
                    if (cnn.State != System.Data.ConnectionState.Open)
                        cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                    writeLogEntry($"Executing section 5 of procedure for {SynchedAssetType.SourceAssetTypeName} - request number {requestNumber}", currentStep);
                    cnn.Query<dynamic>(
                        procedureCommand,
                        new { ExecutionAssetType.ExecutionID, ExecutionAssetType.SynchedAssetTypeID, requestNumber, SynchedAssetType.AssetTypeID, r = DefaultResourceID, section = 5 },
                        commandTimeout: 3600);

                    OnStepCompleted(new StepCompletedEventArgs { Step = currentStep });
                }

                #endregion

                // Clear out execution asset json table.
                currentStep = 10;
                if (ExecutionAssetTypeRetryLog.LastStepCompleted < currentStep)
                {
                    OnStepStarted(new StepStartedEventArgs { Step = currentStep });

                    writeLogEntry($"Do final clearing of execution data like fields", currentStep);

                    if (cnn.State != System.Data.ConnectionState.Open)
                        cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
                    cnn.Execute("delete integration.ExecutionAssetField where SynchedAssetTypeID = @at", new { at = ExecutionAssetType.SynchedAssetTypeID }, commandTimeout: 7200);

                    OnStepCompleted(new StepCompletedEventArgs { Step = currentStep });
                }

                // Clean up asset hash table.
                currentStep = 11;
                if (ExecutionAssetTypeRetryLog.LastStepCompleted < currentStep)
                {
                    OnStepStarted(new StepStartedEventArgs { Step = currentStep });

                    writeLogEntry($"Do final clearing of execution data like hash", currentStep);

                    if (cnn.State != System.Data.ConnectionState.Open)
                        cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
                    cnn.Execute("update integration.AssetHash set RequestNumber = null, [Action] = null, UpdateHash = null where SynchedAssetTypeID = @at", new { at = ExecutionAssetType.SynchedAssetTypeID }, commandTimeout: 7200);

                    OnStepCompleted(new StepCompletedEventArgs { Step = currentStep });
                }

                // Set the last synch time so we can start the next delta check from this date.
                SynchedAssetType.LastSynchOn = now;
                ExecutionAssetTypeRetryLog.LastRetryInError = false;
            }
            catch (Exception oex)
            {
                try
                {
                    writeLogEntry($"Do final clearing of execution data like hash", 99);

                    ExecutionAssetTypeRetryLog.LastRetryInError = true;
                    ExecutionAssetTypeRetryLog.RetryCount++;

                    ExecutionAssetType.ProcessedDelete = true;
                    ExecutionAssetType.ErrorMessage += oex.GetFullExceptionData(false);
                }
                catch (Exception cex)
                {
                    writeLogEntry(cex.GetFullExceptionData(false), 99);
                    CoreFunction.AITrackException(functionName, cex);
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

                    if (!string.IsNullOrEmpty(ExecutionAssetType.ErrorMessage))
                    {
                        if (
                            (
                            ExecutionAssetType.ErrorMessage.Contains("403 (Forbidden)") ||
                            ExecutionAssetType.ErrorMessage.Contains("Unexpected character encountered while parsing value: <. Path ''")
                            )
                            && !ExecutionAssetType.CompletedOn.HasValue
                           )
                        {
                            ExecutionAssetType.DelayUntil = DateTime.UtcNow.AddMinutes(30);
                        }
                    }

                    Company.Update(ExecutionAssetType);

                    if (ExecutionAssetTypeRetryLog.LastRetryInError && ExecutionAssetTypeRetryLog.RetryCount < 10 && !ExecutionAssetType.DelayUntil.HasValue)
                    {
                        Queue.CreateMessage(CoreFunction.GetConfigValueByKey("IntegrationQueue"), QueueModel);
                    }
                }
                catch (Exception cex)
                {
                    writeLogEntry(cex.GetFullExceptionData(false), 99);
                    CoreFunction.AITrackException(functionName, cex);
                }
            }
        }

        #region Utils

        void createHttpClient()
        {
            if (_client == null)
            {
                var handler = new HttpClientHandler { UseCookies = false }; //SslProtocols = System.Security.Authentication.SslProtocols.Tls, 
                if (AutoTrustServerCertificate)
                {
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                }
                _client = new HttpClient(handler, false);
                _client.Timeout = new TimeSpan(1, 0, 0);
                _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            }
        }

        void writeLogEntry(string message, int step, bool isError = false)
        {
            if (SynchedAssetType.EnableAppInsightsVerboseLogging)
            {
                var properties = new Dictionary<string, string>() { { "Step", $"{step}" } };
                if (ExecutionAssetType != null)
                {
                    properties.Add("ExecutionID", ExecutionAssetType.ExecutionID.ToString());
                    properties.Add("SynchedAssetTypeID", ExecutionAssetType.SynchedAssetTypeID.ToString());
                }
                if (isError)
                    CoreFunction.AITrackException("IGC", new ApplicationException(message), Company.CurrentCompanyID, properties);
                else
                    CoreFunction.AITrackTrace("IGC", message, properties, Company.CurrentCompanyID);
            }
            else
            {
                Log.WriteLine(message);
            }
        }

        string CalculateHash(string input)
        {
            MD5 md5 = MD5.Create();

            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);

            byte[] hash = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        long ConvertDateToUnixTimeMilliseconds(DateTime? date = null)
        {
            long epoch = 0;

            if (!date.HasValue)
                date = DateTime.UtcNow;

            epoch = (long)(date.Value.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;

            return epoch;
        }

        int DownloadAndSavePageFromIgc(IgcPostSearchRequestModel postModel, int companyID, string url, string folderName, PageDataClass pageDataClass)
        {
            #region First remove any files that may be been there before.
            try
            {
                var itemsToRemove = Storage.ListFiles($"igc/{folderName}");
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
                    var models = PostJsonToApiAsync<IgcDynamicArrayModels>(url, JsonConvert.SerializeObject(postModel), folderName, postModel.begin).Result;
                    if (models != null)
                    {
                        if (igcCount == 0)
                        {
                            igcCount = models.paging.numTotal;
                        }
                        // serialize JSON directly to a file
                        Storage.CreateFile(
                            $"igc",
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
                    Storage.CreateFile($"igc", $@"{folderName}/{postModel.begin}_error.json", JsonConvert.SerializeObject(ex, Formatting.None, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));

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

        async Task<T> GetFromApi<T>(string uri)
        {
            var cleanUri = new Uri(uri);
            if (cleanUri.Port != 80 && cleanUri.Port != 443 && RemoveUriPortOnConnect)
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

        bool ParseAndSaveAssetsOnIgcPage(SqlConnection cnn, IgcDynamicArrayModels page, short section, int requestNumber, List<string> fieldsToKeep)
        {
            bool hasChanges = false;

            List<string> propertiesToRemove = null;
            var hashModels = new List<AssetHashModel>();
            var pageAssets = new Dictionary<string, JObject>();

            if (page.items.Count > 0)
            {
                foreach (var i in page.items.Children())
                {
                    var sourceID = i["_id"].Value<string>();

                    if (!pageAssets.ContainsKey(sourceID))
                    {
                        JObject obj = (JObject)i;

                        // Remove fields we do not want.
                        if (propertiesToRemove == null)
                        {
                            propertiesToRemove = obj.Properties().Where(pr => !fieldsToKeep.Contains(pr.Name)).Select(pr => pr.Name).ToList();
                            propertiesToRemove.Add("_id"); // No need for this as we are storing it on each record.
                        }

                        if (propertiesToRemove != null)
                        {
                            propertiesToRemove.ForEach(pr => { obj.Remove(pr); });
                        }

                        foreach (var prop in obj.Properties())
                        {
                            if (fieldsToKeep.Contains(prop.Name))
                            {
                                switch (section)
                                {
                                    case 2:
                                        #region
                                        if (prop.Value.ToString() != "{}" && !string.IsNullOrEmpty(prop.Value.ToString()))
                                        {
                                            var relationshipCollection = JsonConvert.DeserializeObject<IgcRelationshipCollection>(prop.Value.ToString());
                                            if (relationshipCollection.items != null && relationshipCollection.paging != null)
                                            {
                                                if (relationshipCollection.paging.numTotal > 0)
                                                {
                                                    OnRelationshipBreakdownModelsUpdated(new RelationshipBreakdownModelsUpdatedEventArgs
                                                    {
                                                        Updates = (
                                                                    from rci in relationshipCollection.items
                                                                    join rtc in RelationshipTargetComparisons on rci._type equals rtc.SourceAssetType
                                                                    where rtc.SourceField == prop.Name
                                                                    group rtc by new { rtc.SourceAssetType, rtc.IntersectTypeID } into g
                                                                    select new IGCAssetRelationshipBreakdownModel
                                                                    {
                                                                        AssetTypeName = g.Key.SourceAssetType,
                                                                        FieldName = prop.Name,
                                                                        IntersectTypeID = g.Key.IntersectTypeID,
                                                                        Count = g.Count()
                                                                    }).ToList()
                                                    });
                                                }
                                            }
                                            else
                                            {
                                                var relationshipItem = JsonConvert.DeserializeObject<GenericIgcContextModel>(prop.Value.ToString());
                                                if (relationshipItem != null)
                                                {
                                                    var rtc = RelationshipTargetComparisons.FirstOrDefault(r => r.SourceField == prop.Name);
                                                    if (rtc != null)
                                                    {
                                                        OnRelationshipBreakdownModelsUpdated(new RelationshipBreakdownModelsUpdatedEventArgs
                                                        {
                                                            Updates = new List<IGCAssetRelationshipBreakdownModel>() {
                                                                new IGCAssetRelationshipBreakdownModel { AssetTypeName = relationshipItem._type, Count = 1, FieldName = prop.Name, IntersectTypeID = rtc.IntersectTypeID }
                                                            }
                                                        });
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                        break;
                                    case 3:
                                        #region
                                        if (!string.IsNullOrEmpty(prop.Value.ToString()))
                                        {
                                            OnResponsibilityBreakdownModelsUpdated(new ResponsibilityBreakdownModelsUpdatedEventArgs
                                            {
                                                Update = new IGCAssetResponsibilityBreakdownModel
                                                {
                                                    Role = prop.Name,
                                                    Count = 1
                                                }
                                            });
                                        }
                                        #endregion
                                        break;
                                }

                            }
                        }

                        var json = JsonConvert.SerializeObject(obj, Formatting.None, new DecimalJsonConverter());

                        var assetHashModel = new AssetHashModel
                        {
                            SynchedAssetTypeID = ExecutionAssetType.SynchedAssetTypeID,
                            Section = section,
                            RequestNumber = requestNumber,
                            SourceID = sourceID,
                            Hash = CalculateHash(json)
                        };
                        hashModels.Add(assetHashModel);
                        pageAssets.Add(sourceID, obj);
                    }
                }

                switch (section)
                {
                    case 2:
                    case 3:
                        OnPageBreakdowns(new EventArgs());
                        break;
                }
            }

            #region See which assets have been changed, based on hashes.

            var tbl = new System.Data.DataTable();

            tbl.Columns.Add("SynchedAssetTypeID", typeof(int));
            tbl.Columns.Add("SourceID", typeof(string));
            tbl.Columns.Add("Section", typeof(short));
            tbl.Columns.Add("RequestNumber", typeof(int));
            tbl.Columns.Add("Hash", typeof(string));

            // Load rows to send to database.
            hashModels.ForEach(h =>
            {
                var row = tbl.NewRow();

                row["SynchedAssetTypeID"] = h.SynchedAssetTypeID;
                row["SourceID"] = h.SourceID;
                row["Section"] = h.Section;
                row["RequestNumber"] = h.RequestNumber;
                row["Hash"] = h.Hash;

                tbl.Rows.Add(row);
            });

            if (cnn.State != System.Data.ConnectionState.Open)
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

            var p = new DynamicParameters();
            p.Add("@SynchedAssetTypeID", ExecutionAssetType.SynchedAssetTypeID);
            p.Add("@Section", section, System.Data.DbType.Int16, System.Data.ParameterDirection.Input);
            p.Add("@RequestNumber", requestNumber, System.Data.DbType.Int32, System.Data.ParameterDirection.Input);
            p.Add("@Hashes", tbl.AsTableValuedParameter());

            hashModels = cnn.Query<AssetHashModel>("integration.MergeAssetHashes", p, commandTimeout: 3600, commandType: System.Data.CommandType.StoredProcedure).ToList();

            hasChanges = (hashModels.Count > 0);

            tbl.Dispose();
            cnn.Close();

            #endregion

            #region Load the fields for changed assets

            var fieldTbl = new System.Data.DataTable();

            fieldTbl.Columns.Add("SynchedAssetTypeID", typeof(int));
            fieldTbl.Columns.Add("Section", typeof(short));
            fieldTbl.Columns.Add("SourceID", typeof(string));
            fieldTbl.Columns.Add("FieldName", typeof(string));
            fieldTbl.Columns.Add("FieldValue", typeof(string));

            var errorDictionary = new Dictionary<string, string>();

            hashModels.ForEach(h =>
            {
                if (pageAssets.ContainsKey(h.SourceID))
                {
                    var pageAsset = pageAssets[h.SourceID];
                    if (pageAsset != null)
                    {
                        foreach (var pr in pageAsset.Properties())
                        {
                            if (pr != null)
                            {
                                if (!string.IsNullOrEmpty(pr.Name))
                                {
                                    try
                                    {
                                        var fieldRow = fieldTbl.NewRow();

                                        fieldRow["SynchedAssetTypeID"] = ExecutionAssetType.SynchedAssetTypeID;
                                        fieldRow["Section"] = section;
                                        fieldRow["SourceID"] = h.SourceID;
                                        fieldRow["FieldName"] = pr.Name;

                                        switch (section)
                                        {
                                            case 1:     // Fields
                                                fieldRow["FieldValue"] = pr.Value;
                                                break;
                                            case 2:     // Relationships
                                                if (pr.Value is JObject)
                                                {
                                                    var items = (pr.Value as JObject).Property("items");
                                                    if (items != null)
                                                    {
                                                        fieldRow["FieldValue"] = items.Value.ToString(Formatting.None);
                                                    }
                                                    else
                                                    {
                                                        fieldRow["FieldValue"] = pr.Value.ToString(Formatting.None);
                                                    }
                                                }
                                                else
                                                {
                                                    fieldRow["FieldValue"] = pr.Value;//.ToString(Formatting.None);
                                                }
                                                break;
                                            case 3:     // Responsibilities
                                                fieldRow["FieldValue"] = pr.Value;//.ToString(Formatting.None);
                                                break;
                                        }

                                        fieldTbl.Rows.Add(fieldRow);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ex.TargetSite != null)
                                        {
                                            if (!errorDictionary.ContainsKey(ex.TargetSite.Name))
                                            {
                                                errorDictionary.Add(ex.TargetSite.Name, ex.GetFullExceptionData(false));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });

            if (errorDictionary.Count > 0)
            {
                foreach (var key in errorDictionary.Keys)
                {
                    ExecutionAssetType.ErrorMessage += errorDictionary[key];
                }
            }

            // If data in fields datatable, push to the server.
            if (fieldTbl.Rows.Count > 0)
            {
                if (cnn.State != System.Data.ConnectionState.Open)
                    cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                using (var bulkCopy = new SqlBulkCopy(cnn))
                {
                    bulkCopy.BatchSize = 5000;
                    bulkCopy.DestinationTableName = "integration.ExecutionAssetField";
                    bulkCopy.BulkCopyTimeout = 3600;

                    bulkCopy.ColumnMappings.Add("SynchedAssetTypeID", "SynchedAssetTypeID");
                    bulkCopy.ColumnMappings.Add("Section", "Section");
                    bulkCopy.ColumnMappings.Add("SourceID", "SourceID");
                    bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
                    bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");

                    bulkCopy.WriteToServer(fieldTbl);
                }

                // Then merge the field data above into integration.AssetField table.
                cnn.Execute(@"
merge	[integration].[AssetField] T
using	(
        select *
        from    [integration].[ExecutionAssetField] 
        where   SynchedAssetTypeID = @SynchedAssetTypeID and Section = @section
        ) S
on		(T.SynchedAssetTypeID = S.SynchedAssetTypeID and T.[Section] = S.[Section] and T.[SourceID] = S.[SourceID] and T.[FieldName] = S.[FieldName])
when	matched then
update	set
		T.FieldValue = S.FieldValue
when	not matched then
insert	(SynchedAssetTypeID, Section, SourceID, FieldName, FieldValue)
values	(S.SynchedAssetTypeID, S.Section, S.SourceID, S.FieldName, S.FieldValue);
delete integration.ExecutionAssetField where SynchedAssetTypeID = @SynchedAssetTypeID and Section = @section;
", new { ExecutionAssetType.SynchedAssetTypeID, section }, commandTimeout: 6000, commandType: System.Data.CommandType.Text);

            }

            fieldTbl.Dispose();
            cnn.Close();

            #endregion

            return hasChanges;
        }

        void ParsePageSavedException(string json)
        {
            try
            {
                var pageException = JsonConvert.DeserializeObject<IgcException>(json);
                var pageExceptionMessage = pageException.GetErrorMessage();
                pageException = null;
                ExecutionAssetType.ErrorMessage += pageExceptionMessage;
            }
            catch (Exception pageConvertEx)
            {
                ExecutionAssetType.ErrorMessage += pageConvertEx.GetFullExceptionData(false);
            }
        }

        async Task<T> PostJsonToApiAsync<T>(string uri, string requestBody, string folderName, int? begin)
        {
            var jsonToReturn = "";
            List<string> cookies = null;

            try
            {
                Client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", AuthenticationHeaderValue);

                using (var response = await Client.PostAsync(uri, new StringContent(requestBody, Encoding.UTF8, "application/json")))
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    var rdr = new StreamReader(stream, Encoding.UTF8);
                    jsonToReturn = rdr.ReadToEnd();
                    rdr.Dispose();
                    stream.Dispose();
                    //byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                    //byte[] convertedBytes = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, bytes);
                    //jsonToReturn = Encoding.Unicode.GetString(convertedBytes);


                    //jsonToReturn = await response.Content.ReadAsStringAsync();

                    cookies = (from c in response.Headers.Where(c => c.Key == "Set-Cookie")
                               from cv in c.Value
                               select cv
                    ).ToList();
                    Client.DefaultRequestHeaders.Remove("Authorization");
                    Client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookies);
                }

                var model = default(T);

                try
                {
                    // First, check to see if we got an error back.
                    var errorModel = JsonConvert.DeserializeObject<IgcPageErrorModel>(jsonToReturn, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });

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

                    if (errorModel == null && model == null)
                    {
                        OnPageErrorCaptured(new PageErrorCapturedEventArgs { ErrorMessage = "IGC HTML Error recieved", StatusCode = System.Net.HttpStatusCode.InternalServerError });
                        Storage.CreateFile($"igc", $@"{folderName}/{begin ?? 0}_error.html", jsonToReturn, "text/html");
                    }
                }
                catch
                {
                    OnPageErrorCaptured(new PageErrorCapturedEventArgs { ErrorMessage = "IGC HTML Error recieved", StatusCode = System.Net.HttpStatusCode.InternalServerError });
                    Storage.CreateFile($"igc", $@"{folderName}/{begin ?? 0}_error.html", jsonToReturn, "text/html");
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
}