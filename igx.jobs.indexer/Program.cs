using d360.core;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.extensions.search;
using d360.extensions.storage;
using d360.model;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.indexer
{
	class Program
    {
        static async Task Main()
        {
			var builder = new HostBuilder();
			builder
				.SetGovernConfiguration()
				.ConfigureWebJobs(c => {
					c.AddAzureStorageQueues();
				})
				.ConfigureGovernLogging()
				.ConfigureServices((context, services) => {
					services.AddScoped<IQueueSource, DummyQueueSource>();
					services.AddScoped<IStorageProvider, DummyStorageProvider>();
					services.AddScoped<ICachingProvider, DummyCachingProvider>();
					services.AddScoped<IMailProvider, DummyMailProvider>();
					services.AddScoped(s => {
						return new ElasticSearchSource
						{
							CommunityConnectionString = context.Configuration["CommunityContext"]
						};
					});
				});

			using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }

    public class Indexer : BaseWebJob
	{        
        const string FUNCTION_NAME = "Indexing_ReIndex";

		ICachingProvider Cache;
		IMailProvider Mail;
		IQueueSource Queue;
		ElasticSearchSource Search;

		public Indexer(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue, ElasticSearchSource search) : base(config)
		{
			Cache = cache;
			Mail = mail;
			Queue = queue;
			Search = search;
		}

		public async Task RunViaQueue([QueueTrigger("%SearchIndexQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, ILogger log)
        {
            ReindexModel reindex = JsonConvert.DeserializeObject<ReindexModel>(myQueueItem);

			var logProperties = new Dictionary<string, object> {
				{ "Function", FUNCTION_NAME },
				{ "CompanyID", reindex.CompanyID }
			};

			using (log.BeginScope(logProperties))
			{
				try
				{
					using (var company = CompanyConnectionUtils.GetCompanyConnection(reindex.CompanyID))
					{
						await ProcessRebuildRequest(Search, company, reindex, log);
					}
				}
				catch (Exception ex)
				{
					log.LogCritical(ex, "Critical error on ReIndexer web job.");
				}			
			}
		}

        public async Task ProcessRebuildRequest(ElasticSearchSource source, SqlConnection company, ReindexModel reindex, ILogger log)
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
					log.LogTrace($"Indexing asset type {assetTypeUid} for company {reindex.CompanyID}, origin: {reindex.Origin}");
					indexer.IndexAssetType(assetTypeUid);
                }
            }
            else if (!string.IsNullOrEmpty(reindex.Category))
            {
				if (reindex.Category == "UpdateMapping")
				{
					log.LogTrace($"Updating mapping for company {reindex.CompanyID}");
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

                    AssetTypeClassInfo info = AssetTypeClassExtensions.GetAsList(AssetTypeClass.Generic).Where(c => c.Value == reindex.Category).FirstOrDefault();
                    if (info != null)
                    {
                        indexer.IndexAssetClass(info.ID);
                    }
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
                await RebuildAllIndex(source, company, reindex.CompanyID, indexer, log);
            }
        }

        public async Task RebuildAllIndex(ElasticSearchSource source, SqlConnection companyConn, int CompanyID, SearchIndexer indexer, ILogger log)
        {
            await UpdateRebuildJobStatus(CompanyID, CompanyRebuildJobStatusState.Active, log);

            if (companyConn.State != System.Data.ConnectionState.Open)
            {
                companyConn.Open();
            }

			var sql = "select case " +
					  "	WHEN a.dist > 30000 THEN 30000 " +
					  "	WHEN a.total > 30000 THEN a.dist " +
					  "	ELSE a.total " +
					  "END " +
					  "FROM (" + 
					  "		select floor(count(1) * 2.4) AS total, floor(count(distinct [Name]) * 3.6) AS dist " +
					  "		from FieldType " +
					  "		where AssetTypeID is not null" +
					  ") a;";
			var SuggestedIndexLimit = companyConn.Query<int>(sql).First();
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
                try
                {
                    indexer.IndexAssetClass(cls);
                }
                catch (Exception ex)
                {
					log.LogError(ex, "Error indexing asset class");
                }

            });

            await LogCompanyReindexComplete(CompanyID, log);
            if (companyConn.State != System.Data.ConnectionState.Closed)
            {
                companyConn.Close();
            }
        }

        #region Supporting Functions

        private async Task LogCompanyReindexComplete(int companyID, ILogger log)
        {
            await UpdateRebuildJobStatus(companyID, CompanyRebuildJobStatusState.Inactive, log);
        }

        private async Task UpdateRebuildJobStatus(int companyID, CompanyRebuildJobStatusState status, ILogger log)
        {
            var _c = GetCompaniesByCurrentSlot().FirstOrDefault(x => x.CompanyID == companyID);

			var context = new UriSecurityContextProvider
			{
				CompanyID = companyID,
				CompanyPrefix = _c.UrlPrefix,
				ResourceID = 0,
				IsAdministrator = true
			};
			var community = new CommunityContext(Configuration["CommunityContext"], Cache, Queue, context);
			var company = new CompanyContext(community, Cache, Queue, Mail, context, log, true);

            CompanyRebuildJobStatusState currentStatue = await company.GetRebuildJobStatus(CompanyRebuildJobToken.SearchIndex, constants.V2_ENVIRONMENT_JOB_REBUILD_TIMEOUT_IN_HOURS);

            if(currentStatue != status)
                await company.UpdateRebuildJobStatus(CompanyRebuildJobToken.SearchIndex, status, constants.V2_ENVIRONMENT_JOB_REBUILD_TIMEOUT_IN_HOURS);
        }

        private Tuple<byte, byte> GetNGramLimits(SqlConnection context)
        {
            float _ngram = context.Query<float>("SELECT Boost FROM dbo.SearchBoost WHERE Field = '_ngram'").FirstOrDefault();
            byte nGramMin = (byte)Math.Truncate(_ngram);
            byte nGramMax = (byte)(((decimal)_ngram % 1) * 100);
            return new Tuple<byte, byte>(nGramMin, nGramMax);
        }

        #endregion
    }
}
