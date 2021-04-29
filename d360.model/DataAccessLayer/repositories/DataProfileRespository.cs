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

namespace d360.model.DataAccessLayer
{
    public class DataProfileRepository : BaseRepository, IDataProfileRepository
    {
        internal ICompanyContext CompanyContext;
        internal ICommunityContext Community;

        public DataProfileRepository(ICompanyContext companyContext, ICommunityContext community)
            : base(companyContext)
        {
            this.CompanyContext = companyContext;
            this.Community = community;
        }

        public async Task<AssetDataProfilesApiViewModel> GetDataProfiles(Guid assetUid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();
            var results = new AssetDataProfilesApiViewModel();
            results.pageNum = CompanyContext.ParsePageNumber(queryParams, 1);
            results.pageSize = CompanyContext.ParsePageSize(queryParams);
            string offset = CompanyContext.ParsePageOffsetSql(results.pageNum, results.pageSize);
            string whereConditions = $@"Where
                                            A.ID in @assetIds
                                            And
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
                AssetDataProfile dataprofile = CompanyContext.AssetDataProfile.OrderByDescending(x => x.ProfileSetDate).FirstOrDefault(x => x.AssetId == asset.ID);
                if(dataprofile != null)
                {
                    startDate = endDate = dataprofile.ProfileSetDate;
                }                
            }             
            
            string dataProfileSQL = $@"select
	                            A.[Uid] as assetUid
	                            ,ADP.[ProfileSetDate]
                                ,ADP.[SampleCount]
                                ,ADP.[NullCount]
                                ,ADP.[BlankCount]
                                ,ADP.[MeanValue]
                                ,ADP.[MinimumValue] as maxValue
                                ,ADP.[MaximumValue] as minValue
                                ,ADP.[MinimumLength]
                                ,ADP.[MaximumLength]
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
                                ,ADP.[PossibleKey]
                                ,ADP.[DataSignature]
                                ,ADP.[StructureSignature]
                                ,ADP.[Cardinality]
	                            ,JSON_QUERY(cardinalityDetail.[value]) as cardinalityDetail
                                ,ADP.[ShapeCardinality] as shapesCardinality
	                            ,JSON_QUERY(shapeDetail.[value]) as shapeDetail
                                ,JSON_QUERY((select CONCAT('[""', STRING_AGG(STRING_ESCAPE(cast([value] as nvarchar(36)), 'JSON'), '"",""'), '""]')
                                                        from AssetDataProfileSample
                                                        where
                                                            AssetDataProfileId = ADP.ID
                                                            and
                                                            lower(SampleType) = 'topk'
                                                        )) as topk
                                ,JSON_QUERY((select CONCAT('[""', STRING_AGG(STRING_ESCAPE(cast([value] as nvarchar(36)), 'JSON'), '"",""'), '""]')
                                                        from AssetDataProfileSample
                                                        where
                                                            AssetDataProfileId = ADP.ID
                                                            and
                                                            lower(SampleType) = 'bottomk'
                                                        )) as bottomk
                            from 
	                            Asset A
	                            Inner Join 
	                            AssetDataProfile ADP on A.ID = ADP.AssetID
	                            outer apply (
                                                select  (
                                                        select [key], [value]
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
                                                        select [key], [value]
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
                                                        select [key], [value]
                                                        from AssetDataProfileSample
                                                        where   
								                            AssetDataProfileId = ADP.ID
                                                            and 
								                            lower(SampleType) = 'shapesdetail'
                                                        for json path
                                                        ) as [value]
                                                ) shapeDetail";

            var assetIds = new List<long>();
            assetIds.Add(asset.ID);

            if (includeChildAssets)
            {
                var decendentSQL = $@"drop table if exists #childIds
                                create table #childIds (
	                                id int
                                )

                                drop table if exists #parentIds
                                create table #parentIds (
	                                id int
                                )

                                drop table if exists #assetIds
                                create table #assetIds (
	                                id int
                                )

                                insert into #parentIds (id) values (@assetId)

                                WHILE ( (Select Count(*) from #parentIds) > 0)
                                BEGIN
	                                insert into #childIDs
	                                select AAP.Assetid from 
		                                #parentIds P 
		                                inner join 
		                                [utility].[ArtifactAssetParent] AAP on P.ID = AAP.ParentAssetID

	                                delete from #parentIDs 
	
	                                insert into #parentIds
	                                select * from #childIDs

	                                insert into #assetIds
	                                select c.* from #childIDs c left join #assetIds a on c.id=a.id
	                                where a.id is null

	                                delete from #childIDs
                                END

                                select * from #assetIds";

                var decendants = await CompanyContext.QueryAsync<long>(decendentSQL, new { assetId = asset.ID }, ApiTimeout);

                assetIds.AddRange(decendants);                    
            }

            dbArgs.Add("@startDate", startDate.Date);
            dbArgs.Add("@endDate", endDate.Date);
            dbArgs.Add("@assetIds", assetIds);

            string sql = $@"{dataProfileSQL}
                        {whereConditions}
                        order by A.[ID]
                        {offset}
                        for Json Path";                

            var jsonStrings = await CompanyContext.QueryAsync<string>(sql, dbArgs, ApiTimeout);
            var json = string.Join("", jsonStrings);

            results.items = JsonConvert.DeserializeObject<List<DataProfileModel>>(string.IsNullOrEmpty(json) ? "[]" : json);

            if (includeTotal)
            {
                var countSQL = $@"select 
                                    COUNT(*)
                                  from 
	                                Asset A
	                                Inner Join 
                                    AssetDataProfile ADP on A.ID = ADP.AssetID
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
    }
}
