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
using System.Collections.Concurrent;
using d360.extensions.mail;
using System.Configuration;

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
                {
                    CoreFunction.AITrackTrace(functionName, $"Indexing asset type {assetTypeUid} for company {reindex.CompanyID}, origin: {reindex.Origin}", companyId: reindex.CompanyID);
                    indexer.IndexAssetType(assetTypeUid);
                }
            }
            else if (!string.IsNullOrEmpty(reindex.Category))
            {
				if (reindex.Category == "UpdateMapping")
				{
					CoreFunction.AITrackTrace(functionName, $"Updating mapping for company {reindex.CompanyID}", companyId: reindex.CompanyID);
					source.UpdateMappingIfExists(reindex.CompanyID);
				}

				if (reindex.Category == "Intersect" || reindex.Category == "Synonym")
                {
                    //Class "Predicate" is overloaded to be used for synonyms and intersects
                    reindex.Category = AssetTypeClass.Predicate.ToString();
                }
                if (SearchIndexer.IsIndexable(reindex.Category) || reindex.Category == AssetTypeClass.Predicate.ToString())
                {
                    string categoryLabel = reindex.Category == AssetTypeClass.Predicate.ToString() ? "Synonym" : reindex.Category;
                    LogReindexStart(categoryLabel, reindex.CompanyID);

                    AssetTypeClassInfo info = AssetTypeClassExtensions.GetAsList(AssetTypeClass.Generic).Where(c => c.Value == reindex.Category).FirstOrDefault();
                    if (info != null)
                    {
                        indexer.IndexAssetClass(info.ID);
                    }
                    LogReindexEnd(categoryLabel, reindex.CompanyID);
                }
            }
            else if (reindex.BatchUids != null && reindex.BatchUids.Any())
            {
                ConcurrentBag<Guid> uids = new ConcurrentBag<Guid>(reindex.BatchUids);
                if (reindex.BatchOperation == ReindexBatchOperation.Update)
                {
                    indexer.IndexAssets(uids);
                }
                else if (reindex.BatchOperation == ReindexBatchOperation.Delete)
                {
                    indexer.RemoveAssets(uids);
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

            int SuggestedIndexLimit = ElasticSearchSource.SuggestIndexLimit(companyConn);
            if (SuggestedIndexLimit > 1000)
            {
                source.IndexFieldLimit = SuggestedIndexLimit;
            }

            var (nGramMin, nGramMax) = GetNGramLimits(companyConn);
            if(nGramMin > 0)
            {
                source.NGramMin = nGramMin;
                source.NGramMax = nGramMax;
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
                AssetTypeClass.User,
                AssetTypeClass.SemanticType,
				AssetTypeClass.Predicate
            };

			classes.ForEach(cls => {
				indexer.CanCreatePendingDBLog(cls, null, true);
			});

            source.ClearIndex(CompanyID);

            classes.ForEach(cls => {
                LogReindexStart(cls == AssetTypeClass.Predicate ? "Synonym" : cls.ToString(), CompanyID);
                try
                {
                    indexer.IndexAssetClass(cls);
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, CompanyID);
                }

            });

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
            var _c = CoreFunction.GetCompaniesByCurrentSlot()
                .FirstOrDefault(x => x.CompanyID == companyID);

            var companyContext = JobDbContextCreator.CreateCompanyContext(
                new UriSecurityContextProvider
                {
                    CompanyID = companyID,
                    CompanyPrefix = _c.UrlPrefix,
                    ResourceID = 0,
                    IsAdministrator = true
                },
                new MandrillMailProvider
                {
                    ApiKey = ConfigurationManager.AppSettings[constants.MAIL_API_KEY],
                    SubAccount = ConfigurationManager.AppSettings[constants.MAIL_SUB_ACCOUNT]
                },
                new AzureQueueSource(),
                new DummyCachingProvider(),
                constants.COMMUNITY_DATABASE_CONNECTION);

            CompanyRebuildJobStatusState currentStatue = await companyContext.GetRebuildJobStatus(CompanyRebuildJobToken.SearchIndex, constants.V2_ENVIRONMENT_JOB_REBUILD_TIMEOUT_IN_HOURS);

            if(currentStatue != status)
                await companyContext.UpdateRebuildJobStatus(CompanyRebuildJobToken.SearchIndex, status, constants.V2_ENVIRONMENT_JOB_REBUILD_TIMEOUT_IN_HOURS);

        }

        private static void LogReindexStart(string typeName, int companyID)
        {
            CoreFunction.AITrackTrace(functionName, $"Starting {typeName} reindex for company {companyID}", companyId: companyID);
        }

        private static void LogReindexEnd(string typeName, int companyID)
        {
            CoreFunction.AITrackTrace(functionName, $"Completed {typeName} reindex for company {companyID}", companyId: companyID);
        }

        private static Tuple<byte, byte> GetNGramLimits(SqlConnection context)
        {
            float _ngram = context.Query<float>("SELECT Boost FROM dbo.SearchBoost WHERE Field = '_ngram'").FirstOrDefault();
            byte nGramMin = (byte)Math.Truncate(_ngram);
            byte nGramMax = (byte)(((decimal)_ngram % 1) * 100);
            return new Tuple<byte, byte>(nGramMin, nGramMax);
        }

        #endregion
    }
}
