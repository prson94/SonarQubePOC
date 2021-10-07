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
using d360.core.enums;
using d360.model.helpers.filters;
using d360.core.resources;

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
                if (dataprofile != null)
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
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
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
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
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

        public async Task<AssetDataProfilesMatchingAssetsApiViewModel> GetMatchingAssets(Guid assetUid, string similarType, IEnumerable<KeyValuePair<string, string>> queryParams, bool onlyTotal = false)
        {

            var dbArgs = new DynamicParameters();
            var results = new AssetDataProfilesMatchingAssetsApiViewModel();
            results.pageNum = CompanyContext.ParsePageNumber(queryParams, 1);
            results.pageSize = CompanyContext.ParsePageSize(queryParams);

            bool includeTotal = true;

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includetotal"))
            {
                bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includetotal").Value, out includeTotal);
            }            

            var sql = BuildMatchAssetsSQL(assetUid, similarType, queryParams, dbArgs, onlyTotal);

            if (onlyTotal)
            {
                results.total = await CompanyContext.QueryFirstOrDefaultAsync<int?>(sql, dbArgs, ApiTimeout);
                return results;
            }

            var multiQuery = await CompanyContext.QueryMultipleAsync(sql, dbArgs, ApiTimeout);
            results.items = multiQuery.Read<AssetDataProfileMatchingAssetsModel>().ToList();

            if (includeTotal)
            {
                results.total = multiQuery.Read<int?>().FirstOrDefault();
            }
            else
            {
                results.total = null;
            }

            return results;
        }

        public async Task<IEnumerable<DataProfileExportModel>> GetMatchedAssetsForExport(Guid assetUid, string similarType, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();

            var sql = BuildMatchAssetsSQL(assetUid, similarType, queryParams, dbArgs, isExport: true);
            return await CompanyContext.QueryAsync<DataProfileExportModel>(sql, dbArgs, ApiTimeout);
        }

        public async Task<AssetDataProfileByTypeQualifierApiViewModel> GetAssetsByTypeQualifier(string typeQualifier, decimal minConfidence, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();
            var results = new AssetDataProfileByTypeQualifierApiViewModel();
            results.pageNum = CompanyContext.ParsePageNumber(queryParams, 1);
            results.pageSize = CompanyContext.ParsePageSize(queryParams);
            string offset = CompanyContext.ParsePageOffsetSql(results.pageNum, results.pageSize);
            string whereConditions = $@"where 
                                        ADP.typeQualifier = @typeQualifier 
                                        AND ADP.confidence>=@minConfidence
		                                AND ADP.ProfileSetDate = maxProfileDate.profileSetDate";
            string sqlJoins = "";
            bool includeTotal = true;
            string orderDirection = "asc";
            string orderBy = "NDP.DisplayPath";

            if (string.IsNullOrEmpty(typeQualifier) || typeQualifier.Length > 200)
            {
                throw new GenericException(System.Net.HttpStatusCode.BadRequest, CommonErrors.BadRequest, string.Format(CommonErrors.InvalidParameter, "typeQualifier"));
            }
            if (minConfidence <= 0 || minConfidence > 1)
            {
                throw new GenericException(System.Net.HttpStatusCode.BadRequest, CommonErrors.BadRequest, string.Format(CommonErrors.InvalidParameter, "minConfidence"));
            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includetotal"))
            {
                bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includetotal").Value, out includeTotal);
            }

            if (queryParams.Any(q => q.Key == "_direction"))
            {
                string[] allowedValues = new[] { "asc", "desc" };
                var directionFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value.Trim().ToLower();

                if (allowedValues.Contains(directionFilter))
                {
                    orderDirection = directionFilter;
                }
            }

            if (queryParams.Any(q => q.Key == "_order"))
            {
                var orderFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_order").Value.Trim().ToLower();

                if (orderFilter.Equals("confidence", StringComparison.InvariantCultureIgnoreCase))
                {
                    orderBy = "ADP.confidence";
                }
            }

            dbArgs.Add("@typeQualifier", typeQualifier);
            dbArgs.Add("@minConfidence", minConfidence);

            sqlJoins = $@" AssetDataProfile ADP
                            inner join 
		                    [graph].AssetNodeDisplayPath NDP on NDP.ID=adp.AssetID
                            outer apply 
			                (
			                select 
				                max(ProfileSetDate) profileSetDate 
			                from 
				                AssetDataProfile 
			                where 
				                AssetID = ADP.AssetID
			                ) maxProfileDate";

            if (!CompanyContext.CurrentResourceIsAdmin)
            {
                sqlJoins = $@"{sqlJoins}
                              outer apply (select 1 as [value] from ResponsibilityDetail RD where resourceid = @userid and ((RD.AssetID = NDP.id and applyToType=0) or (RD.AssetID = 0 and RD.AssetTypeID=NDP.AssetTypeID)) and RD.PermissionsBitMask & {(int)Permission.ReadAsset} = 0) hasAccess";

                whereConditions = $@"{whereConditions} 
                                    and
		                            (
		                            hasAccess.value is null 
		                            or 
		                            hasAccess.value != 1	
		                            )";

                dbArgs.Add("@userid", CompanyContext.CurrentResourceID);
            }


            var itemsSQL = $@"
                            SELECT 
                                distinct
	                            NDP.uid, 
                                NDP.DisplayPath as [path],
                                ADP.Confidence
                            FROM                                     
	                            {sqlJoins}		                            
	                            {whereConditions}
		                    order by {orderBy} {orderDirection}
                            {offset}";

            results.items = await CompanyContext.QueryAsync<AssetDataProfileByTypeQualifierModel>(itemsSQL, dbArgs, ApiTimeout);

            if (includeTotal)
            {
                var countSQL = $@"
                            SELECT 
	                            Count(*)
                            FROM                                     
	                            {sqlJoins}		                            
	                            {whereConditions}
		                    ";

                results.total = await CompanyContext.QueryFirstOrDefaultAsync<int>(countSQL, dbArgs, ApiTimeout);
            }
            else
            {
                results.total = null;
            }

            return results;
        }

        public string BuildMatchAssetsSQL(Guid assetUid, string similarType, IEnumerable<KeyValuePair<string, string>> queryParams, DynamicParameters dbArgs, bool onlyTotal = false, bool isExport = false)
        {
            int pageNum = CompanyContext.ParsePageNumber(queryParams, 1);
            int pageSize = CompanyContext.ParsePageSize(queryParams);
            string offset = CompanyContext.ParsePageOffsetSql(pageNum, pageSize);
            bool hasAdvancedFilter = false;

            string whereConditions = $@"where 
		                                 ADP.ProfileSetDate = maxProfileDate.profileSetDate";
            string sqlJoins = $@"  inner join 
		                    [graph].AssetNodeDisplayPath NDP WITH (NOEXPAND) on NDP.ID=adp.AssetID and adp.AssetId != @assetId
                            outer apply 
			                (
			                select 
				                max(ProfileSetDate) profileSetDate 
			                from 
				                AssetDataProfile 
			                where 
				                AssetID = ADP.AssetID
			                ) maxProfileDate
                            outer apply
                            (
                                Select  (                                                                                                                
                                    Select                                     
	                                    T.Value
                                    from 
	                                    AssetTag AT
	                                    inner join 
	                                    Tag T on AT.TagId = T.Id
                                    where 
                                        AT.AssetID = adp.AssetID
                                    order by T.Value
                                    For Json Path
                                    ) as [value]
                            ) Tags
                            left Join FieldType F on f.AssetTypeID = NDP.AssetTypeID and F.[Type] = 'tag'";

            bool includeTotal = true;
            string orderDirection = "asc";
            string filterSQL = "";
            string structureCondition = "";
            string filterJoinSQL = "";
            string selectFields = $@"NDP.uid, 
                                    NDP.DisplayPath as [path]
                                    ,JSON_QUERY(replace(REPLACE(REPLACE(REPLACE(tags.value, '}}]', ']'), '[{{', '['), '""value"":', ''), '}},{{', ',')) as tagsJson 
                                    ,case when F.id is null then 0 else 1 end as hasTagField
                                    ,[Segments]";
            string filterSelectFields = "*";
            string assetDetailSQL = "";

            List<string> filters = new List<string>();

            var asset = CompanyContext.Filter<Asset>(o => o.uid == assetUid).FirstOrDefault();
            
            if (asset == null)
            {
                throw new GenericException(System.Net.HttpStatusCode.NotFound, "", "Asset with provided Uid does not exist.");
            }

            AssetDataProfile dataprofile = CompanyContext.AssetDataProfile.Where(x => x.AssetId == asset.ID).OrderByDescending(x => x.ProfileSetDate).FirstOrDefault();
            if (dataprofile == null)
            {
                throw new GenericException(System.Net.HttpStatusCode.NotFound, "", "Profile record does not exist for provided Uid.");
            }

            if (similarType == null)
            {
                throw new GenericException(System.Net.HttpStatusCode.NotFound, "", "Signature Type is required.");
            }
            else
            {
                string[] allowedValues = new[] { "structure", "data" };

                if (allowedValues.Contains(similarType.ToLowerInvariant()))
                {
                    if (similarType.ToLowerInvariant() == "structure")
                    {
                        dbArgs.Add("@signature", dataprofile.StructureSignature);
                        structureCondition = "ADP.StructureSignature = @signature";
                    }
                    else
                    {
                        dbArgs.Add("@signature", dataprofile.DataSignature);
                        structureCondition = "ADP.DataSignature = @signature";
                    }
                }
                else
                {
                    throw new GenericException(System.Net.HttpStatusCode.NotFound, "", "Signature Type is invalid.");
                }
            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includetotal"))
            {
                bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includetotal").Value, out includeTotal);
            }

            if (queryParams.Any(q => q.Key == "_filter"))
            {
                var filterValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;
                List<DefaultFilter> fieldList = new List<DefaultFilter>
                {
                new DefaultFilter("Tag", "T.tagString", SqlFieldType.Text),
                new DefaultFilter("Path", "[Segments]", SqlFieldType.Xml),
                };

                if (!string.IsNullOrEmpty(filterValue))
                {
                    CompanyContext.ParseAdvancedFilterQueryParameter(queryParams, fieldList, out DynamicParameters advFilterArgs, out List<string> advFilterStatements);
                    if (advFilterArgs != null && advFilterStatements != null)
                    {
                        dbArgs.AddDynamicParams(advFilterArgs);
                        filters.AddRange(advFilterStatements);
                    }
                    hasAdvancedFilter = true;
                }
            }

            if(hasAdvancedFilter || isExport)
            {
                filterJoinSQL = $@"outer Apply (
								Select tagString = STRING_AGG( value ,'|')
								 From  OpenJSON(tagsJson)
							 ) T";
            }            

            if (queryParams.Any(q => q.Key == "_direction"))
            {
                string[] allowedValues = new[] { "asc", "desc" };
                var directionFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value.Trim().ToLower();

                if (allowedValues.Contains(directionFilter))
                {
                    orderDirection = directionFilter;
                }
            }

            string orderBySQL = $"td.[path] {orderDirection}";

            if (queryParams.Any(qp => qp.Key.ToLower() == "_order"))
            {
                var orderBy = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_order").Value.Trim().ToLower();

                if (orderBy == "tags")
                {
                    orderBySQL = $@"JSON_VALUE('{{""tags"":'+ISNULL(tagsJson, '[]')+'}}', '$.tags[0]') {orderDirection}, hasTagField {orderDirection}";
                }               
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_simplefilter"))
            {
                var simpleFilter = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_simplefilter").Value.Trim();
                if (!string.IsNullOrEmpty(simpleFilter))
                {
                    simpleFilter = CompanyContext.GetEscapedFilterString(simpleFilter);

                    dbArgs.Add("@simpleFilter", simpleFilter);

                    filters.Add($@"(
                                    [Path] like @simpleFilter 
                                    or                                             
                                    exists (select 1 from OPENJSON(tagsJson) where Value like @simpleFilter)
                                )");
                }
            }

            if (filters.Any())
            {
                filterSQL = $"where {string.Join(" and ", filters)}";
            }

            if (!CompanyContext.CurrentResourceIsAdmin)
            {
                sqlJoins = $@"{sqlJoins}
                              outer apply (select 1 as [value] from ResponsibilityDetail RD where resourceid = @userid and ((RD.AssetID = NDP.id and applyToType=0) or (RD.AssetID = 0 and RD.AssetTypeID=NDP.AssetTypeID)) and RD.PermissionsBitMask & {(int)Permission.ReadAsset} = 0) hasAccess";

                whereConditions = $@"{whereConditions} 
                                    and
		                            (
		                            hasAccess.value is null 
		                            or 
		                            hasAccess.value != 1	
		                            )";

                dbArgs.Add("@userid", CompanyContext.CurrentResourceID);
            }

            dbArgs.Add("@assetId", asset.ID);

            if (isExport)
            {
                selectFields = $@"{selectFields}                                 
                                ,NDP.AssetTypeID
								,NDP.AssetTypeUid
                                ,NDP.ID";

                assetDetailSQL = $@"drop table if exists #tempAssetDetails;

                                    select 
							            AN.ID as AssetID
							            ,AN.Uid as AssetUid
							            ,AN.DisplayPath as AssetPath
							            ,ATP.Path as AssetTypePath
							            ,tagString as AssetTags
						            into #tempAssetDetails
						            from graph.AssetNodeDisplayPath AN
						            cross apply dbo.GetAssetTypeTextPathById(AN.AssetTypeID, ' > ') ATP
						            outer apply
                                    (   select                
							            STRING_AGG(T.Value ,'|') as tagString
							            from(
                                            Select                                     
	                                            T.Value
                                            from 
	                                            AssetTag AT
	                                            inner join 
	                                            Tag T on AT.TagId = T.Id
                                            where 
                                                AT.AssetID = AN.ID
                                            order by T.Value OFFSET 0 ROWS) T
                                    ) as Tags
						            where AN.ID=@assetId";

                filterJoinSQL = $@"{filterJoinSQL}
                                   outer apply dbo.GetAssetTypeTextPathById(td.AssetTypeId, ' > ') P
                                    left join #tempAssetDetails ad on 1=1
                                   ";

                filterSelectFields = $@"
                                        ad.AssetID
								        ,ad.AssetUid
								        ,ad.AssetPath
								        ,ad.AssetTypePath
								        ,ad.AssetTags
								        ,td.ID as MatchedAssetID
								        ,t.tagString as MatchedAssetTags
								        ,td.path MatchedAssetPath
								        ,td.Uid as MatchedAssetUid					
								        ,P.Path as MatchedAssetTypePath";                

                
            }

            string tempTablesSQL = $@"drop table if exists #tempadpid;
                            create table #tempadpid (Assetid bigint,ProfileSetDate date)

                            insert into #tempadpid
                            select ADP.Assetid,max(ProfileSetDate) profileSetDate
                            from AssetDataProfile ADP
                            where {structureCondition} and adp.AssetId != @assetId                       
                            group by ADP.Assetid;

                            create index idx_tempadid on #tempadpid(Assetid);
                            drop table if exists #tempdata2;

                            SELECT 
                                {selectFields}	                            
                            into #tempdata2
                            FROM #tempadpid adp 
                                {sqlJoins}		     
                                {whereConditions}";

            string countQuery = $@"
                            SELECT 
	                            Count(*)
                            FROM                                     
                                #tempdata2
                            {filterJoinSQL}
                            {filterSQL}
		                    ";

            if (onlyTotal)
            {
                var onlyTotalSQL = $@"{tempTablesSQL}                                                 
                                        {countQuery}
                                        ";

                return onlyTotalSQL;
            }

            if (!includeTotal || isExport)
            {
                countQuery = "";

            }

            var sql = $@"{tempTablesSQL}

                            {assetDetailSQL}

                            select 
                                {filterSelectFields}
                            from #tempdata2 td
                            {filterJoinSQL}
                            {filterSQL}
                            order by {orderBySQL}
                             {offset}

                            {countQuery}
                            ";
            return sql;
        }
    }
}
