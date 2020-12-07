using d360.core;
using d360.core.queue;
using d360.core.enums;
using d360.extensions.search;
using d360.extensions.queue;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using d360.core.entities;
using d360.extensions.info;
using d360.extensions.caching;
using d360.model;

namespace igx.jobs.indexer
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();

            /*
             * Timer execution disabled (GOV-10646 - Lower / Stop rebuild of search indexes every weekend)
             * To enable, uncomment the following line
             */
            //config.UseTimers();

            //We should only process one reindex queue item at a time
            config.Queues.BatchSize = 1;

#if DEBUG
            config.UseDevelopmentSettings();
#endif

            System.Net.ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    internal interface IPagedQuerySqlModel
    {
        long AssetID { get; set; }
    }

    internal class FieldSqlModel : IPagedQuerySqlModel
    {
        public long AssetID { get; set; }
        public string Name { get; set; }
        public string FormattedValue { get; set; }
    }

    internal class TagSqlModel : IPagedQuerySqlModel
    {
        public long AssetID { get; set; }
        public Guid AssetUID { get; set; }
        public Guid TagUID { get; set; }
        public string Value { get; set; }
    }

    internal class ResponsibilitySqlModel : IPagedQuerySqlModel
    {
        public long AssetID { get; set; }
        public string SecurityAsset { get; set; }
        public int SecurityAssetID { get; set; }
    }

    public static class Indexer
    {
        private static int _defaultQueryCommandTimeout = 180;
        const string functionName = "Indexing_ReIndex";
#if DEBUG
        const string timerSettings = "0 */15 * * * *";
#else
        const string timerSettings = "0 0 17 * * 6";
#endif

        public static void RunViaTimer([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.UpdateRebuildRequestByCurrentSlot(CompanyRebuildJobToken.SearchIndex);

                var queue = new AzureQueueSource();
                companies.ForEach(c =>
                {
                    queue.CreateMessage(Config.GetValue<string>("SearchIndexQueue"), new ReindexModel { CompanyID = c });
                });

            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }

            CoreFunction.AIFlush();
        }

        public static async Task RunViaQueue([QueueTrigger("%SearchIndexQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
        {
            var c = JsonConvert.DeserializeObject<ReindexModel>(myQueueItem);

            try
            {
                var source = new ElasticSearchSource();
                using (var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID))
                {
                    await ProcessCompany(source, company, c);
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }
        }

        public static async Task ProcessCompany(ElasticSearchSource source, SqlConnection company, ReindexModel c)
        {
            await UpdateRebuildJobStatus(c.CompanyID, CompanyRebuildJobStatusState.Active);

            company.Open();
            List<CompanySetting> settings = CompanyConnectionUtils.GetCompanySettings(c.CompanyID);
            bool fusionEnabled = (settings.Any(i => i.SettingID == 70) ? bool.Parse(settings.Single(i => i.SettingID == 70).Value) : true);

            int SuggestedIndexLimit = SuggestIndexLimit(company);
            if (SuggestedIndexLimit > 1000)
            {
                source.IndexFieldLimit = SuggestedIndexLimit;
            }

            List<AssetTypeClass> classes = new List<AssetTypeClass> {
                AssetTypeClass.BusinessAsset,
                AssetTypeClass.TechnicalAsset,
                AssetTypeClass.Model,
                AssetTypeClass.Policy,
                AssetTypeClass.Rule,
                AssetTypeClass.ReferenceItemType,
                AssetTypeClass.Group,
                AssetTypeClass.User
            };

            if(fusionEnabled)
            {
                classes.Add(AssetTypeClass.Fusion);
                classes.Add(AssetTypeClass.FusionAttribute);
            }
            
            SearchIndexer indexer = new SearchIndexer(company, c.CompanyID, source);
            source.ClearIndex(c.CompanyID);

            classes.ForEach(cls => {
                LogReindexStart(cls.ToString(), c.CompanyID);
                try
                {
                    indexer.IndexAssetClass(cls);
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                }

            });


            LogReindexStart("Artifact Synonyms", c.CompanyID);
            try
            {
                indexer.IndexObjectType("Intersect", false);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            LogReindexStart("Custom Synonyms", c.CompanyID);
            try
            {
                indexer.IndexObjectType("Synonym", false);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            await LogCompanyReindexComplete(c.CompanyID);
        }

        #region Supporting Functions

        private static async Task LogCompanyReindexComplete(int companyID)
        {
            CoreFunction.AITrackTrace(functionName, $"Completed reindex for company {companyID}", companyId: companyID);

            await UpdateRebuildJobStatus(companyID, CompanyRebuildJobStatusState.Inactive);
        }

        private static async Task UpdateRebuildJobStatus(int companyID, CompanyRebuildJobStatusState status)
        {
            #region Create EF connection

            var _c = CoreFunction.GetCompaniesByCurrentSlot()
                .FirstOrDefault(x => x.CompanyID == companyID);

            var sec = new UriSecurityContextProvider()
            {
                CompanyID = companyID,
                ResourceID = 0,
                CompanyPrefix = _c.UrlPrefix,
                IsAdministrator = true
            };
            var cache = new DummyCachingProvider();
            var queue = new AzureQueueSource();
            var community = new CommunityContext(cache, queue, sec);

            #endregion

            CompanyRebuildJobStatusState currentStatue = await community.GetRebuildJobStatus(CompanyRebuildJobToken.SearchIndex);

            if(currentStatue != status)
                await community.UpdateRebuildJobStatus(CompanyRebuildJobToken.SearchIndex, status);

        }

        private static void LogReindexStart(string typeName, int companyID)
        {
            CoreFunction.AITrackTrace(functionName, $"Starting {typeName} reindex for company {companyID}", companyId: companyID);
        }

        private static int SuggestIndexLimit(SqlConnection context) {
            /*
             * To estimate the limit of fields in the index, we count the number of field types and add 20%
             * We are not indexing all field types, and field types with the same name are mapped to the same elastic field
             * If the number of field types is too high, then count the distinct field names and add 80%.
             * Under no circumstance should we set limit higher than 30,000
             * https://www.elastic.co/guide/en/elasticsearch/reference/6.8/mapping.html#mapping-limit-settings
             */
            var sql = @"SELECT CASE
                            WHEN a.dist > 30000 THEN 30000
                            WHEN a.total > 30000 THEN a.dist
                            ELSE a.total
                        END
                        FROM (
                            SELECT FLOOR(COUNT(*) * 1.2) AS total,
                                    FLOOR(COUNT(DISTINCT [Name]) * 1.8) AS dist
                            FROM [dbo].[FieldType]
                        ) a;";
            return context.Query<int>(sql).FirstOrDefault();
        }

        #endregion
    }
}
