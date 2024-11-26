using AngleSharp.Common;
using d360.core;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.extensions.info;
using d360.extensions.search;
using d360.model;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using repositories;
using repositories.azure;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.indexer
{
	public class Indexer : BaseWebJob
	{        
        const string FUNCTION_NAME = "Indexer";

		readonly ICachingProvider Cache;
		readonly IMailProvider Mail;
		readonly IQueueSource Queue;
		readonly ElasticSearchSource Search;

		public Indexer(IConfiguration config, ICommunity community, ICachingProvider cache, IMailProvider mail, IQueueSource queue, ElasticSearchSource search) : base(community, config)
		{
			Cache = cache;
			Mail = mail;
			Queue = queue;
			Search = search;
		}

		[FunctionName(FUNCTION_NAME)]
		public async Task RunViaQueue([QueueTrigger(constants.Queue.Search, Connection = constants.Setting.Storage)] string myQueueItem, ILogger log)
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
					var connectionString = Community.GetConnectionStringForTenant(reindex.CompanyID);
					var workspace = new Workspaces(new DapperConnectionProvider { ReadOnlyConnectionString = connectionString, ReadWriteConnectionString = connectionString });
					using (var company = new SqlConnection(connectionString))
					{
						await ProcessRebuildRequest(Search, company, workspace, reindex, log);
					}
				}
				catch (Exception ex)
				{
					log.LogCritical(ex, "Critical error on ReIndexer web job.");
				}			
			}
		}

        public async Task ProcessRebuildRequest(ElasticSearchSource source, SqlConnection company, Workspaces workspace, ReindexModel reindex, ILogger log)
        {
            SearchIndexer indexer = new SearchIndexer(company, reindex.CompanyID, source);
			if (reindex.AssetUid.HasValue)
			{
				Guid assetUid = reindex.AssetUid ?? Guid.Empty;
				if (assetUid != Guid.Empty)
				{
					indexer.IndexAsset(assetUid);
				}
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
			else if (reindex.BatchUids != null && reindex.BatchUids.Any())
			{
				if (reindex.BatchOperation == ReindexBatchOperation.Update)
				{
					indexer.IndexAssets(reindex.BatchUids);
				}
				else if (reindex.BatchOperation == ReindexBatchOperation.Delete)
				{
					indexer.RemoveAssets(reindex.BatchUids);
				}
			}
			else if (reindex.IntersectIDs != null && reindex.IntersectIDs.Any())
			{
				if (reindex.BatchOperation == ReindexBatchOperation.Update)
				{
					indexer.IndexIntersects(reindex.IntersectIDs);
				}
				else if (reindex.BatchOperation == ReindexBatchOperation.Delete)
				{
					indexer.RemoveIntersects(reindex.IntersectIDs);
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
					AssetTypeClassInfo info = AssetTypeClassExtensions.GetAsList(AssetTypeClass.Generic).Where(c => c.Value == reindex.Category).FirstOrDefault();
					if (info != null)
					{
						indexer.IndexAssetClass(info.ID);
					}
				}
			}
			else
			{
				int timeoutHours = int.Parse(Configuration["V2EnvironmentJobRebuildTimeoutInHours"]);
				await workspace.UpsertRebuildStatusAsync(CompanyRebuildJobToken.SearchIndex, CompanyRebuildJobStatusState.Active, timeoutHours);
				await RebuildAllIndex(source, company, reindex.CompanyID, indexer, log);
				await workspace.UpsertRebuildStatusAsync(CompanyRebuildJobToken.SearchIndex, CompanyRebuildJobStatusState.Inactive, timeoutHours);
			}
        }

        public async Task RebuildAllIndex(ElasticSearchSource source, SqlConnection companyConn, int CompanyID, SearchIndexer indexer, ILogger log)
        {
			await companyConn.OpenIfClosed();

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

			companyConn.CloseIfOpened();
        }

        private Tuple<byte, byte> GetNGramLimits(SqlConnection context)
        {
            float _ngram = context.Query<float>("SELECT Boost FROM dbo.SearchBoost WHERE Field = '_ngram'").FirstOrDefault();
            byte nGramMin = (byte)Math.Truncate(_ngram);
            byte nGramMax = (byte)(((decimal)_ngram % 1) * 100);
            return new Tuple<byte, byte>(nGramMin, nGramMax);
        }
    }
}
