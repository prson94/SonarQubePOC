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
using Microsoft.Extensions.Hosting;

namespace igx.jobs.indexer
{
    class Program
    {
        static async Task Main()
        {
            var builder = CoreFunction.JobHostConfigBuilder();
            builder.ConfigureWebJobs(c =>
            {
                c.AddAzureStorageCoreServices()
                .AddAzureStorage();
                //.AddTimers(); //GOV-10646 No longer indexing on a schedule. Keeping schedule code in case decision is reversed.
            });

            using (var host = builder.Build())
            {
                await host.RunAsync();
            }
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
            ReindexModel reindex = JsonConvert.DeserializeObject<ReindexModel>(myQueueItem);

            try
            {
                var source = new ElasticSearchSource();
                using (var company = CompanyConnectionUtils.GetCompanyConnection(reindex.CompanyID))
                {
                    await ProcessRebuildRequest(source, company, reindex);
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, reindex.CompanyID);
            }
        }

        public static async Task ProcessRebuildRequest(ElasticSearchSource source, SqlConnection company, ReindexModel reindex)
        {
            SearchIndexer indexer = new SearchIndexer(company, reindex.CompanyID, source);
            if (reindex.AssetUid.HasValue)
            {
                Guid assetUid = reindex.AssetUid ?? Guid.Empty;
                if (assetUid != Guid.Empty)
                    indexer.IndexAsset(assetUid);
            }
            else if (reindex.AssetTypeUid.HasValue)
            {
                Guid assetTypeUid = reindex.AssetTypeUid ?? Guid.Empty;
                if (assetTypeUid != Guid.Empty)
                    indexer.IndexAssetType(assetTypeUid);
            }
            else if (!string.IsNullOrEmpty(reindex.Category))
            {
                if (SearchIndexer.IsIndexable(reindex.Category) || reindex.Category == AssetTypeClass.Predicate.ToString())
                {
                    string cat = reindex.Category == AssetTypeClass.Predicate.ToString() ? "Synonym" : reindex.Category;
                    LogReindexStart(cat, reindex.CompanyID);

                    AssetTypeClassInfo info = AssetTypeClassExtensions.GetAsList(AssetTypeClass.Generic).Where(c => c.Value == reindex.Category).FirstOrDefault();
                    if (info != null)
                    {
                        indexer.IndexAssetClass(info.ID);
                    }
                    else if (reindex.Category == "Intersect" || reindex.Category == "Synonym")
                    {
                        //Synonyms and Intersects are the same category
                        indexer.IndexObjectType("Intersect", true);
                        indexer.IndexObjectType("Synonym", false);
                    }
                    LogReindexEnd(reindex.Category, reindex.CompanyID);

                }
            }
            else
            {
                await RebuildAllIndex(source, company, reindex.CompanyID, indexer);
            }
        }

        public static async Task RebuildAllIndex(ElasticSearchSource source, SqlConnection companyConn, int CompanyID, SearchIndexer indexer)
        {
            await UpdateRebuildJobStatus(CompanyID, CompanyRebuildJobStatusState.Active);

            if (companyConn.State != System.Data.ConnectionState.Open)
            {
                companyConn.Open();
            }

            int SuggestedIndexLimit = SuggestIndexLimit(companyConn);
            if (SuggestedIndexLimit > 1000)
            {
                source.IndexFieldLimit = SuggestedIndexLimit;
            }

            List<AssetTypeClass> classes = new List<AssetTypeClass> {
                AssetTypeClass.BusinessAsset,
                AssetTypeClass.TechnicalAsset,
                AssetTypeClass.Diagram,
                AssetTypeClass.Model,
                AssetTypeClass.Policy,
                AssetTypeClass.Rule,
                AssetTypeClass.ReferenceItemType,
                AssetTypeClass.Group,
                AssetTypeClass.User
            };

            source.ClearIndex(CompanyID);

            classes.ForEach(cls => {
                LogReindexStart(cls.ToString(), CompanyID);
                try
                {
                    indexer.IndexAssetClass(cls);
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, CompanyID);
                }

            });

            LogReindexStart("Synonyms", CompanyID);
            try
            {
                indexer.IndexAssetClass(AssetTypeClass.Predicate);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, CompanyID);
            }

            await LogCompanyReindexComplete(CompanyID);
            if (companyConn.State != System.Data.ConnectionState.Closed)
            {
                companyConn.Close();
            }
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

        private static void LogReindexEnd(string typeName, int companyID)
        {
            CoreFunction.AITrackTrace(functionName, $"Completed {typeName} reindex for company {companyID}", companyId: companyID);
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
