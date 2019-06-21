using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities;
using System.Linq.Expressions;
using System.Data.Entity.Infrastructure;
using Dapper;
using d360.core.queue;
using d360.extensions;
using Newtonsoft.Json;
using d360.core;

namespace d360.model.DataAccessLayer
{
    public class FusionRepository : IFusionRepository
    {
        internal ICompanyContext CompanyContext;
        internal IStorageProvider StorageProvider;
        internal IQueueSource QueueSource;

        public FusionRepository(ICompanyContext context, IQueueSource queueSource, IStorageProvider storageProvider)
        {
            this.CompanyContext = context;
            this.StorageProvider = storageProvider;
            this.QueueSource = queueSource;
        }

        public Asset GetFusionByUID(Guid guid)
        {
            if (guid == null || guid == Guid.Empty)
                return null;

            return CompanyContext.Assets.Include(at => at.AssetType).FirstOrDefault(x => x.Object == "Fusion" && x.uid == guid);
        }

        public bool HasFusionRules(int fusionId)
        {
            return CompanyContext.FusionRules.Any(x => x.FusionID == fusionId);
        }

        public async Task<ApiExecutionInfo> BulkDeleteFusionConfiguration(Guid assetUid, bool Cascade, ApiExecution execution)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = CompanyContext.CurrentCompanyID,
                CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = CompanyContext.CurrentResourceID,
                Action = ApiExecutionAction.DeleteAssets
            };

            // Save to storage container.
            StorageProvider.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(new AssetDeletes() { new AssetDelete() { Cascade = Cascade, Uid = assetUid } }));

            // Save to queue.
            await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

            // Save to the database.
            execution.ExecutionID = executionInfo.ExecutionID;
            CompanyContext.Add(execution);
            return executionInfo;
        }


    }
}
