using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.core.queue;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;

using Dapper;

using Newtonsoft.Json;

namespace d360.model.DataAccessLayer
{
    public class CrossReferencesRepository : BaseRepository, ICrossReferencesRepository
    {
        private readonly ICompanyContext CompanyContext;
        internal IQueueSource QueueSource;
        internal IStorageProvider StorageProvider;

        public CrossReferencesRepository(ICompanyContext compCtx, IQueueSource queueSource, IStorageProvider storageProvider) : base(compCtx)
        {
            CompanyContext = compCtx;
            QueueSource = queueSource;
            StorageProvider = storageProvider;
        }

        public async Task<IEnumerable<AssetCrossReference>> GetCrossReferences(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();
            var sql = "select uid, DataSource, Type, ExternalID, FieldHash from [dbo].[AssetCrossReference]";
            List<string> queryFilters = new List<string>();

            if (queryParams.ToList().Any(q => q.Key.ToLower() == "_assetuid"))
            {
                Guid assetUid = new Guid();

                var assetUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_assetuid").Value;
                if (Guid.TryParse(assetUidString, out assetUid))
                {
                    dbArgs.Add("@assetuid", assetUid);
                    queryFilters.Add($"[UID] = @assetuid");
                }
            }

            if (queryParams.ToList().Any(q => q.Key.ToLower() == "_externalid"))
            {
                var externalId = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_externalid").Value;
                dbArgs.Add("@externalid", externalId);
                queryFilters.Add($"[ExternalID] = @externalid");
            }

            if (queryParams.ToList().Any(q => q.Key.ToLower() == "_datasource"))
            {
                var ds = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_datasource").Value;
                dbArgs.Add("@datasource", ds);
                queryFilters.Add($"[DataSource] = @datasource");
            }

            if (queryParams.ToList().Any(q => q.Key.ToLower() == "_type"))
            {
                var ty = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_type").Value;
                dbArgs.Add("@type", ty);
                queryFilters.Add($"[type] = @type");
            }

            if (queryFilters.Count > 0)
            {
                sql += " where " + string.Join(" and ", queryFilters);
            }

            var assetCrossReferences = await CompanyContext.QueryAsync<AssetCrossReference>(sql, dbArgs, ApiTimeout);
            
            return assetCrossReferences;
        }

        public async Task<IEnumerable<AssetCrossReference>> GetByAssetUid(string assetUid)
        {
            return await CompanyContext.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where uid = @assetUid", new { assetUid }, ApiTimeout);
        }

        public async Task<IEnumerable<AssetCrossReference>> GetCrossReferenceByTypeId(string type, string externalId)
        {
            return await CompanyContext.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where [type] = @type and [ExternalID] = @externalId", new { type = new DbString { Value = type, IsFixedLength = true, Length = 50, IsAnsi = true }, externalId }, ApiTimeout);
        }

        public async Task<IEnumerable<AssetCrossReference>> GetCrossReferenceByType(string type)
        {
            return await CompanyContext.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where [type] = @type", new { type = new DbString { Value = type, IsFixedLength = true, Length = 50, IsAnsi = true } }, ApiTimeout);
        }

        public async Task<IEnumerable<AssetCrossReference>> GetCrossReferenceByDataSource(string dataSource)
        {
            return await CompanyContext.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where [datasource] = @dataSource", new { dataSource = new DbString { Value = dataSource, IsFixedLength = true, Length = 250, IsAnsi = true } }, ApiTimeout);
        }

        public async Task<int> CreateNewCrossReference(AssetCrossReference model)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("insert into assetcrossreference (uid,DataSource,Type,ExternalID,FieldHash) values(@u,@d,@t,@e,@f)", new { u = model.uid, d = model.DataSource, t = model.Type, f = model.FieldHash, e = model.ExternalID });
        }

        public IEnumerable<AssetCrossReferenceResult> PostBulkCrossReference(List<AssetCrossReference> models, ApiExecution execution)
        {
            CompanyContext.Add(execution);
            List<AssetCrossReferenceResult> results = null;
            
            try
            {
                results = CompanyContext.ImportCrossReferences(execution, models);


                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }

            return results;
        }

        public async Task<int> PutCrossReference(Guid uid, string dataSource, string type, AssetCrossReference model)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("update assetcrossreference set FieldHash = @fh where uid = @uid and DataSource = @ds and [Type] = @t", new { fh = model.FieldHash, uid = uid, ds = dataSource, t = type });
        }

        public async Task<int> PutCrossReference(Guid uid, AssetCrossReference model)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("update assetcrossreference set FieldHash = @fh where uid = @uid and DataSource = @ds and [Type] = @t", new { fh = model.FieldHash, uid = uid, ds = model.DataSource, t = model.Type });
        }

        public async Task<int> DeleteCrossReferenceByUid(Guid uid)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("delete assetcrossreference where uid = @uid", new { uid = uid });
        }

        public async Task<int> DeleteCrossReferenceByDataSource(string dataSource, string type, int timeout = 90)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("delete assetcrossreference where datasource = @d and [type] = @t", new { d = dataSource, t = type }, commandTimeout: timeout);
        }

        public async Task<int> DeleteCrossReferenceByDataSource(string dataSource, int timeout = 90)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("delete assetcrossreference where [datasource] = @d", new { d = dataSource }, commandTimeout: timeout);
        }

        public async Task<int> DeleteCrossReferenceByType(string type, int timeout = 90)
        {
            return await CompanyContext.Database.Connection.ExecuteAsync("delete assetcrossreference where [type] = @t", new { t = type }, commandTimeout: timeout);
        }

        public async Task<bool> XrefExists(AssetCrossReference model)
        {
            return await CompanyContext.Database.Connection.QuerySingleAsync<bool>(@"if exists (select 1 from assetcrossreference where uid = @u and [type] = @t and datasource = @d and externalid = @e)
                        begin
                            select 1;
                                end
                        else 
                        begin
                            select 0;
                                end", new { u = model.uid, t = model.Type, d = model.DataSource, e = model.ExternalID });

        }

        public async Task<ApiExecutionInfo> PostBatchCrossReference(List<AssetCrossReference> crossReferences, ApiExecution execution, bool sendWorkflowEvents = true)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = CompanyContext.CurrentCompanyID,
                CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = CompanyContext.CurrentResourceID,
                Action = ApiExecutionAction.PostCrossReferences,
                SendWorkflowEvents = sendWorkflowEvents
            };

            // Save to storage container.
            await StorageProvider.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(crossReferences));

            // Save to the database.
            execution.ExecutionID = executionInfo.ExecutionID;

            CompanyContext.Add(execution);

            // Save to queue.
            if (!await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo))
            {
                throw new Exception(AZURE_QUEUE_INSERTION_FAILURE_MESSAGE);
            }

            return executionInfo;
        }

        public BulkAssetCrossReferenceResult GetExecutionStatus(ApiExecution execution)
        {
            BulkAssetCrossReferenceResult bulkResult = new BulkAssetCrossReferenceResult
            {
                Total = execution.Total,
                Processed = execution.Processed,
                Error = execution.Error,
                CompletedOn = execution.CompletedOn,
                StartedOn = execution.StartedOn
            };

            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = CompanyContext.CurrentCompanyID,
                CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
                ExecutionID = execution.ExecutionID,
                ResourceID = CompanyContext.CurrentResourceID,
                Action = ApiExecutionAction.PostCrossReferences

            };

            try
            {
                var resultsJson = StorageProvider.GetFileContentsAsString(executionInfo.StorageFolder, executionInfo.ResponseFileName);
                bulkResult.Results = JsonConvert.DeserializeObject<List<AssetCrossReferenceResult>>(resultsJson);
            }
            catch
            {

            }

            return bulkResult;
        }
    }
}
