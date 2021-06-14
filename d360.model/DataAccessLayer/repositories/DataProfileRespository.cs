using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core;
using d360.model.DataAccessLayer.repositories;
using Dapper;
using d360.core.exceptions;
using Newtonsoft.Json;
using d360.core.queue;
using d360.extensions;

namespace d360.model.DataAccessLayer
{
    public class DataProfileRepository : BaseRepository, IDataProfileRepository
    {
        internal ICompanyContext CompanyContext;
        internal ICommunityContext Community;
        internal IStorageProvider StorageProvider;
        internal IQueueSource QueueSource;

        public DataProfileRepository(ICompanyContext companyContext, ICommunityContext community, IStorageProvider storageProvider, IQueueSource queueSource)
            : base(companyContext)
        {
            this.CompanyContext = companyContext;
            this.Community = community;
            this.StorageProvider = storageProvider;
            this.QueueSource = queueSource;
        }

        public async Task<AssetDataProfilesApiViewModel> GetDataProfiles(Guid assetUid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();
            var results = new AssetDataProfilesApiViewModel();
            results.pageNum = CompanyContext.ParsePageNumber(queryParams, 1);
            results.pageSize = CompanyContext.ParsePageSize(queryParams);
            string offset = CompanyContext.ParsePageOffsetSql(results.pageNum, results.pageSize);
            string whereConditions = $@"Where                                            
                                            ADP.ProfileSetDate between @startDate and @endDate";

            bool includeTotal = true;

            var asset = CompanyContext.Filter<Asset>(o => o.uid == assetUid).FirstOrDefault();
            if (asset == null)
            {
                throw new GenericException(System.Net.HttpStatusCode.NotFound, "", "Asset with provided Uid does not exist.");
            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includetotal"))
            {
                bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includetotal").Value, out includeTotal);
            }

            bool includeChildAssets = false;

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includechildassets"))
            {
                bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includechildassets").Value, out includeChildAssets);
            }

            DateTime startDate = DateTime.UtcNow;
            DateTime endDate = DateTime.UtcNow;

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_startdate" || k.Key.ToLower() == "_enddate"))
            {
                if (queryParams.ToList().Any(k => k.Key.ToLower() == "_startdate"))
                {
                    DateTime.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_startdate").Value, out startDate);
                }

                if (queryParams.ToList().Any(k => k.Key.ToLower() == "_enddate"))
                {
                    DateTime.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_enddate").Value, out endDate);
                }
            }
            else
            {
                AssetDataProfile dataprofile = CompanyContext.AssetDataProfile.Where(x => x.AssetId == asset.ID).OrderByDescending(x => x.ProfileSetDate).FirstOrDefault();
                if(dataprofile != null)
                {
                    startDate = endDate = dataprofile.ProfileSetDate;
                }                
            }

            var descendantsSQL = $@"with descendants as (select @assetID as AssetID)";

            if (includeChildAssets)
            {
                descendantsSQL = $@"with descendants as (
	                                    select @assetID as AssetID
	                                    union all
	                                    select 
		                                    AAP.assetID
	                                    from 
		                                    descendants as d
		                                    inner join 
		                                    [utility].[ArtifactAssetParent] AAP on d.AssetID = AAP.ParentAssetID
                                    )";
            }
                        
            var dataProfileIdsSql = $@"
                                    drop table if exists #assetdataprofileids
                                    create table #assetdataprofileids (
	                                    id bigint
                                    );
                                    {descendantsSQL}
                                    insert into #assetdataprofileids
		                            select 
			                            ADP.id 
		                            from 
			                            descendants A
			                            inner join 
			                            AssetDataProfile ADP on adp.AssetID = A.AssetID	                                                                                
                                    {whereConditions}
		                            ";

            string dataProfileSQL = $@"select
	                            A.[Uid] as assetUid
	                            ,ADP.[ProfileSetDate]
                                ,ADP.[SampleCount]
                                ,ADP.[NullCount]
                                ,ADP.[BlankCount]
                                ,ADP.[MeanValue] as mean
                                ,ADP.[MinimumValue] as min
                                ,ADP.[MaximumValue] as max
                                ,ADP.[MinimumLength] as minLength
                                ,ADP.[MaximumLength] as maxLength
                                ,ADP.[StandardDeviation]
                                ,ADP.[Multiline]
                                ,ADP.[RegExp]
                                ,ADP.[Confidence]
                                ,ADP.[Type]
                                ,ADP.[TypeQualifier]                                
                                ,ADP.[LogicalType]
                                ,ADP.[LeadingWhiteSpace]
                                ,ADP.[LeadingZeroCount]
                                ,ADP.[TrailingWhiteSpace]
                                ,ADP.[MatchCount]
                                ,ADP.[OutlierCardinality]
	                            ,JSON_QUERY(outlierDetail.[value]) as outlierDetail
                                ,ADP.[KeyConfidence]
                                ,ADP.[DataSignature]
                                ,ADP.[StructureSignature]
                                ,ADP.[Cardinality]
	                            ,JSON_QUERY(cardinalityDetail.[value]) as cardinalityDetail
                                ,ADP.[ShapeCardinality] as shapesCardinality
	                            ,JSON_QUERY(shapesDetail.[value]) as shapesDetail
                                ,JSON_QUERY(replace(REPLACE(REPLACE(REPLACE(bottomK.value, '}}]',']'), '[{{','['), '""value"":',''), '}},{{',',')) as bottomK
								,JSON_QUERY(replace(REPLACE(REPLACE(REPLACE(topK.value, '}}]', ']'), '[{{', '['), '""value"":', ''), '}},{{', ',')) as topK                                
                                ,ADP.TotalCount
                                ,ADP.OutlierCount
                                ,ADP.DetectionLocale
                                ,ADP.FtaVersion
                                ,ADP.DecimalSeparator
                            from 
                                #assetdataprofileids ids
                                inner join 
                                AssetDataProfile ADP on ids.ID = ADP.ID	                            
	                            Inner Join 
	                            Asset A on A.ID = ADP.AssetID                            
                            outer apply (            
                            select  (
                                    select [key], [value] as Count
                                                        from AssetDataProfileSample
                                                        where
                                                            AssetDataProfileId = ADP.ID
                                                            and
                                                            lower(SampleType) = 'outlierdetail'
                                                        for json path
                                                        ) as [value]
                                                ) outlierDetail
                                outer apply (
                                                select  (
                                                        select [key], [value] as Count
                                                        from AssetDataProfileSample
                                                        where
                                                            AssetDataProfileId = ADP.ID
                                                            and
                                                            lower(SampleType) = 'cardinalitydetail'
                                                        for json path
                                                        ) as [value]
                                                ) cardinalityDetail
                                outer apply (
                                                select  (
                                                        select [key], [value] as Count
                                                        from AssetDataProfileSample
                                                        where
                                                            AssetDataProfileId = ADP.ID
                                                            and
                                                            lower(SampleType) = 'shapesdetail'
                                                        for json path
                                                        ) as [value]
                                                ) shapesDetail
                                outer apply (
                                                select  (
                                                        select [value]
                                                        from AssetDataProfileSample
                                                        where
                                                            AssetDataProfileId = ADP.ID
                                                            and
                                                            lower(SampleType) = 'bottomk'
                                                        for json path
                                                        ) as [value]
                                                ) bottomK
                                outer apply (
                                                select  (
                                                        select [value]
                                                        from AssetDataProfileSample
                                                        where
                                                            AssetDataProfileId = ADP.ID
                                                            and
                                                            lower(SampleType) = 'topk'
                                                        for json path
                                                        ) as [value]
                                                ) topK ";            

            dbArgs.Add("@startDate", startDate.Date);
            dbArgs.Add("@endDate", endDate.Date);
            dbArgs.Add("@assetId", asset.ID);

            string sql = $@"
                        {dataProfileIdsSql}
                        order by ADP.[ID]
			            {offset}
                        {dataProfileSQL}
                        for Json Path";

            var jsonStrings = await CompanyContext.QueryAsync<string>(sql, dbArgs, ApiTimeout);
            var json = string.Join("", jsonStrings);

            results.items = JsonConvert.DeserializeObject<List<DataProfileModel>>(string.IsNullOrEmpty(json) ? "[]" : json);            

            if (includeTotal)
            {
                var countSQL = $@"
                                {descendantsSQL}
                                select 
                                    COUNT(*)
                                from      
                                    descendants A
                                    inner join 
                                    AssetDataProfile ADP on adp.AssetID = A.AssetID	                                    
                                {whereConditions}";
                results.total = await CompanyContext.QueryFirstOrDefaultAsync<int>(countSQL, dbArgs, ApiTimeout);
            }
            else
            {
                results.total = null;
            }
            
            return results;
        }

        public List<DataProfileUpsertResponse> UpsertDataProfiles(List<DataProfileUpsertModel> DataProfileUpsertModels, ApiExecution execution, bool isInsert)
        {
            CompanyContext.Add(execution);

            List<DataProfileUpsertResponse> results = null;
            
            try
            {
                results = CompanyContext.UpsertDataProfiles(DataProfileUpsertModels, execution, isInsert);

                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }            

            return results;
        }

        public List<DataProfileDeleteResponse> DeleteDataProfiles(Asset asset, DateTime startDate, DateTime endDate, ApiExecution execution, bool cascade = false)
        {            
            CompanyContext.Add(execution);
            
            var assetDataProfileDeleteModel = new AssetDataProfileDeleteModel { AssetUid = asset.uid, StartDate = startDate, EndDate = endDate, Cascade = cascade };
            
            List<AssetDataProfileDeleteModel> models = new List<AssetDataProfileDeleteModel>();
            models.Add(assetDataProfileDeleteModel);

            List<DataProfileDeleteResponse> results = null;

            try
            {
                results = CompanyContext.DeleteDataProfiles(models, execution);

                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }

            return results;
        }

        public async Task<ApiExecutionInfo> PostBatchDataProfiles(List<DataProfileUpsertModel> models, ApiExecution execution)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = CompanyContext.CurrentCompanyID,
                CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = execution.ResourceID,
                Action = ApiExecutionAction.PostDataProfile,
            };

            return await CreateApiBatchJob(executionInfo, execution, models, StorageProvider, QueueSource).ConfigureAwait(false);
        }

        public async Task<ApiExecutionInfo> PutBatchDataProfiles(List<DataProfileUpsertModel> models, ApiExecution execution)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = CompanyContext.CurrentCompanyID,
                CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = execution.ResourceID,
                Action = ApiExecutionAction.PutDataProfile,
            };

            return await CreateApiBatchJob(executionInfo, execution, models, StorageProvider, QueueSource).ConfigureAwait(false);
        }

        public async Task<ApiExecutionInfo> DeleteBatchDataProfiles(List<AssetDataProfileDeleteModel> models, ApiExecution execution)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = CompanyContext.CurrentCompanyID,
                CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = execution.ResourceID,
                Action = ApiExecutionAction.DeleteDataProfile,
            };

            return await CreateApiBatchJob(executionInfo, execution, models, StorageProvider, QueueSource).ConfigureAwait(false);
        }
    }
}
