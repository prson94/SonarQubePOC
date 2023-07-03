using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.queue;
using d360.core.resources;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers.filters;

using Dapper;

using Newtonsoft.Json;

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
			CompanyContext = companyContext;
			Community = community;
			StorageProvider = storageProvider;
			QueueSource = queueSource;
		}

		public async Task<AssetDataProfilesApiViewModel> GetDataProfiles(Guid assetUid, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var dbArgs = new DynamicParameters();
			var results = new AssetDataProfilesApiViewModel
			{
				pageNum = CompanyContext.ParsePageNumber(queryParams, 1),
				pageSize = CompanyContext.ParsePageSize(queryParams)
			};
			string offset = CompanyContext.ParsePageOffsetSql(results.pageNum, results.pageSize);
			string whereConditions = $@"Where                                            
											ADP.ProfileSetDate between @startDate and @endDate";

			bool includeTotal = true;

			var asset = CompanyContext.Filter<Asset>(o => o.uid == assetUid).FirstOrDefault();
			if (asset == null)
			{
				throw new GenericException(System.Net.HttpStatusCode.NotFound, AssetTypeErrors.NotFound, CommentErrors.AssetUidNotFound);
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

			bool includeSamples = true;

			if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includesamples"))
			{
				bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includesamples").Value, out includeSamples);
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

					if (!includeChildAssets)
					{
						List<AssetDataProfileSample> dataProfileSamples = includeSamples ? CompanyContext.AssetDataProfileSample.Where(x => x.AssetDataProfileID == dataprofile.ID).ToList() : new List<AssetDataProfileSample>();
						List<AssetDataProfileSampleJson> dataProfileDetails = includeSamples ? CompanyContext.AssetDataProfileSampleJson.Where(x => x.AssetDataProfileID == dataprofile.ID).ToList() : new List<AssetDataProfileSampleJson>();
						results.items = new List<DataProfileModel> { new DataProfileModel(assetUid, dataprofile, dataProfileSamples, dataProfileDetails) };

						if (includeTotal)
						{
							results.total = 1;
						}
						else
						{
							results.total = null;
						}
						return results;
					}
				}
				else
				{
					//no profiling records
					results.items = new List<DataProfileModel>();
					return results;
				}
			}

			var descendantsSQL = $@"with descendants as (select @assetID as AssetID)";

			if (includeChildAssets)
			{
				descendantsSQL = $@"with descendants as (
										select @assetID as AssetID
										union all
										select	AAP.ObjectAssetID as AssetID
										from	descendants as d
												inner join PredicateIntersect AAP on AAP.SubjectAssetID = d.AssetID and AAP.PredicateType in (3,4)
									)";
			}

			var dataProfileIdsSql = $@"
									drop table if exists #assetdataprofileids
									create table #assetdataprofileids (
										id bigint, 
										ProfileSetDate DateTime
									);
									{descendantsSQL}
									insert into #assetdataprofileids
									select 
										ADP.id, ADP.ProfileSetDate
									from 
										descendants A
										inner join 
										AssetDataProfile ADP on adp.AssetID = A.AssetID	                                                                                
									{whereConditions}
									";

			var dataProfileSamplesSql = "";
			if (includeSamples)
			{
				dataProfileSamplesSql = $@"
						;with rs_Data as(select distinct ID from #assetdataprofileids)
						select adps.AssetDataProfileId,adps.SampleType,adps.[Key],adps.[Value]
						into #tempADPS
						from AssetDataProfile ADP
						inner join rs_Data ids on ids.ID = ADP.ID
						inner join AssetDataProfileSample adps on adps.AssetDataProfileId = ADP.ID;";

			}

			string dataProfileSQL = GetDataProfilesBaseSQL(includeSamples, "inner join #assetdataprofileids ids on ids.ID = ADP.ID");

			dbArgs.Add("@startDate", startDate);
			dbArgs.Add("@endDate", endDate);
			dbArgs.Add("@assetId", asset.ID);

			string sql = $@"
						drop table if exists #tempADPS;

						{dataProfileIdsSql}
						order by ADP.[ProfileSetDate] desc
						{offset}

						{dataProfileSamplesSql}

						{dataProfileSQL}
						order by ADP.[ProfileSetDate] desc
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

		public async Task<AssetDataProfilesApiViewModel> GetDataProfiles(string profileIdentifier, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var dbArgs = new DynamicParameters();
			var results = new AssetDataProfilesApiViewModel
			{
				pageNum = CompanyContext.ParsePageNumber(queryParams, 1),
				pageSize = CompanyContext.ParsePageSize(queryParams)
			};
			string offset = CompanyContext.ParsePageOffsetSql(results.pageNum, results.pageSize);
			string whereConditions = $@" Where ADP.ProfileIdentifier = @profileIdentifier ";

			
			bool includeTotal = true;
			Guid assetUid;
			Asset asset;
			if (queryParams.ToList().Any(k => k.Key.ToLower() == "_assetuid"))
			{
				if (Guid.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_assetuid").Value, out assetUid))
                {
					asset = CompanyContext.Filter<Asset>(o => o.uid == assetUid).FirstOrDefault();
					if (asset == null)
					{
						throw new GenericException(System.Net.HttpStatusCode.NotFound, AssetTypeErrors.NotFound, CommentErrors.AssetUidNotFound);
					}
					else
                    {
						dbArgs.Add("@assetId", asset.ID);
						whereConditions += " and ADP.AssetID = @assetId "; 
					}
				}
			}

			if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includetotal"))
			{
				bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includetotal").Value, out includeTotal);
			}

			bool includeSamples = true;

			if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includesamples"))
			{
				bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includesamples").Value, out includeSamples);
			}

			var dataProfileIdsSql = $@"
						drop table if exists #tempADPS;
						drop table if exists #assetdataprofileids
						create table #assetdataprofileids (
							id bigint, 
							ProfileSetDate DateTime
						);

						insert into #assetdataprofileids
						select ADP.ID, ADP.[ProfileSetDate]
						from AssetDataProfile ADP
						{whereConditions}
						order by ADP.[ProfileSetDate] desc
						{offset}";

			var dataProfileSamplesSql = $@"
						select adps.AssetDataProfileId,adps.SampleType,adps.[Key],adps.[Value]
						into #tempADPS
						from #assetdataprofileids tempADP
						inner join AssetDataProfileSample adps on adps.AssetDataProfileId = tempADP.ID;";

			string dataProfileSQL = GetDataProfilesBaseSQL(includeSamples,"inner join #assetdataprofileids ids on ids.ID = ADP.ID");

			dbArgs.Add("@profileIdentifier", profileIdentifier);


			var countSql = $"select count(*) from AssetDataProfile ADP {whereConditions}";

			if (includeTotal)
            {
				results.total = await CompanyContext.QueryFirstOrDefaultAsync<int>(countSql, dbArgs, ApiTimeout);
			}
			else
            {
				results.total = null;
            }

			string sql = $@"
						{dataProfileIdsSql}

						{dataProfileSamplesSql}

						{dataProfileSQL}
						order by ADP.[ProfileSetDate] desc
						for Json Path";
			var jsonStrings = await CompanyContext.QueryAsync<string>(sql, dbArgs, ApiTimeout);
			var json = string.Join("", jsonStrings);

			results.items = JsonConvert.DeserializeObject<List<DataProfileModel>>(string.IsNullOrEmpty(json) ? "[]" : json);

			return results;
		}


		public async Task<List<DataProfileUpsertResponse>> UpsertAsync(List<DataProfileUpsertModel> DataProfileUpsertModels, ApiExecution execution, bool isInsert)
		{
			CompanyContext.Add(execution);

			List<DataProfileUpsertResponse> results = null;

			try
			{
				await CompanyContext.UpsertDataProfilesAsync(DataProfileUpsertModels, execution, isInsert);
				results = await CompanyContext.GetExecutionDataProfileResultsAsync(execution.ExecutionID);

				execution.Processed = results.Count; 
				execution.Error = results.Count(i => !i.Success);
				execution.CompletedOn = DateTime.UtcNow;
				CompanyContext.Update(execution);
			}
			catch (Exception ex)
			{
				CompanyContext.UpdateExecutionWithErrorFromException(execution, ex);
			}

			return results;
		}

		public async Task<List<DataProfileDeleteResponse>> DeleteAsync(Asset asset, DateTime startDate, DateTime endDate, ApiExecution execution, bool cascade = false)
		{
			CompanyContext.Add(execution);

			var assetDataProfileDeleteModel = new AssetDataProfileDeleteModel { AssetUid = asset.uid, StartDate = startDate, EndDate = endDate, Cascade = cascade };

			List<AssetDataProfileDeleteModel> models = new List<AssetDataProfileDeleteModel>
			{
				assetDataProfileDeleteModel
			};

			List<DataProfileDeleteResponse> results = null;

			try
			{
				await CompanyContext.DeleteDataProfilesAsync(models, execution);
				results = await CompanyContext.GetExecutionDeleteDataProfileResultsAsync(execution.ExecutionID);
				
				execution.Processed = results.Count;
				execution.Error = results.Count(i => !i.Success);
				execution.CompletedOn = DateTime.UtcNow;
				CompanyContext.Update(execution);
			}
			catch (Exception ex)
			{
				CompanyContext.UpdateExecutionWithErrorFromException(execution, ex);
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
			var results = new AssetDataProfilesMatchingAssetsApiViewModel
			{
				pageNum = CompanyContext.ParsePageNumber(queryParams, 1),
				pageSize = CompanyContext.ParsePageSize(queryParams)
			};

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

		public async Task<AssetDataProfileByTypeQualifierApiViewModel> GetAssetsByTypeQualifier(string typeQualifier, decimal minConfidence, IEnumerable<KeyValuePair<string, string>> queryParams, bool isExport = false)
		{
			var dbArgs = new DynamicParameters();
			var results = new AssetDataProfileByTypeQualifierApiViewModel
			{
				pageNum = CompanyContext.ParsePageNumber(queryParams, 1),
				pageSize = CompanyContext.ParsePageSize(queryParams)
			};
			string offset = CompanyContext.ParsePageOffsetSql(results.pageNum, results.pageSize);
			string whereConditions = $@"where 
										ADP.typeQualifier = @typeQualifier 
										AND ADP.confidence>=@minConfidence
										AND ADP.ProfileSetDate = maxProfileDate.profileSetDate";
			string sqlJoins = "";
			bool includeTotal = true;
			string orderDirection = "asc";
			string orderBy = "AP.DisplayPath";
			List<string> filters = new List<string>();

			string filterSQL = "";


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

				if (orderFilter.Equals("assettypepath", StringComparison.InvariantCultureIgnoreCase))
				{
					orderBy = "P.Path";
				}

				if (orderFilter.Equals("path", StringComparison.InvariantCultureIgnoreCase))
				{
					orderBy = "AP.DisplayPath";
				}
			}

			if (queryParams.Any(q => q.Key == "_filter"))
			{
				var filterValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;
				List<DefaultFilter> fieldList = new List<DefaultFilter>
				{
				new DefaultFilter("assetTypePath", "P.Path", SqlFieldType.Text),
				new DefaultFilter("Path", "AP.[Segments]", SqlFieldType.Xml),
				new DefaultFilter("outOfDate", "(case when CAST(s.effectiveDate as DATE) > ADP.profileSetDate then 'true' else 'false' end)", SqlFieldType.Boolean),
				};

				if (!string.IsNullOrEmpty(filterValue))
				{
					CompanyContext.ParseAdvancedFilterQueryParameter(queryParams, fieldList, out DynamicParameters advFilterArgs, out List<string> advFilterStatements);
					if (advFilterArgs != null && advFilterStatements != null)
					{
						dbArgs.AddDynamicParams(advFilterArgs);
						filters.AddRange(advFilterStatements);
					}
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
									AP.DisplayPath like @simpleFilter 
									or                                             
									P.[Path] like @simpleFilter
								)");
				}
			}

			if (filters.Any())
			{
				filterSQL = $"and {string.Join(" and ", filters)}";
			}

			dbArgs.Add("@typeQualifier", typeQualifier);
			dbArgs.Add("@minConfidence", minConfidence);

			sqlJoins = $@" AssetDataProfile ADP
							INNER JOIN
							Asset A on A.ID=adp.AssetID
							INNER JOIN
							AssetPath AP on A.ID=AP.ID
							INNER JOIN
							AssetType AST on A.AssetTypeID=AST.ID
							outer apply 
							(
							select 
								max(ProfileSetDate) profileSetDate 
							from 
								AssetDataProfile 
							where 
								AssetID = ADP.AssetID
							) maxProfileDate
							cross apply dbo.GetAssetTypeTextPathById(A.AssetTypeID, ' > ') P";

			if (!CompanyContext.CurrentResourceIsAdmin)
			{
				sqlJoins = $@"{sqlJoins}
							  outer apply (select 1 as [value] from ResponsibilityDetail RD where resourceid = @userid and ((RD.AssetID = A.id and applyToType=0) or (RD.AssetID = 0 and RD.AssetTypeID=A.AssetTypeID)) and RD.PermissionsBitMask & {(int)Permission.ReadAsset} = 0) hasAccess";

				whereConditions = $@"{whereConditions} 
									and
									(
									hasAccess.value is null 
									or 
									hasAccess.value != 1	
									)";

				dbArgs.Add("@userid", CompanyContext.CurrentResourceID);
			}

				sqlJoins = $@"{sqlJoins}
							   outer apply 
							(
							select 
								top 1 *
							from 
								semantic
							where 
								qualifier = @typeQualifier
							order by 
								EffectiveDate desc

							) s";

			var itemsSQL = $@"
							SELECT 
								distinct
								A.uid, 
								AP.DisplayPath as [path],
								P.[path] as assetTypePath,
								ADP.Confidence,
								ADP.ProfileSetDate as effectiveDate,
								AST.Uid as assettypeUid
								{(isExport ? ", s.Uid as semanticTypeUid" : "")}
							FROM                                     
								{sqlJoins}		                            
								{whereConditions}
								{filterSQL}
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
                                {filterSQL}
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
			string sqlJoins = $@" 
							INNER JOIN
							Asset A on A.ID=adp.AssetID and adp.AssetId != @assetId
							INNER JOIN
							AssetPath AP on A.ID=AP.ID
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
							left Join FieldType F on f.AssetTypeID = A.AssetTypeID and F.[Type] = 'tag'";

			bool includeTotal = true;
			string orderDirection = "asc";
			string filterSQL = "";
			string structureCondition = "";
			string filterJoinSQL = "";
			string selectFields = $@"A.uid, 
									AP.DisplayPath as [path]
									,JSON_QUERY(replace(REPLACE(REPLACE(REPLACE(tags.value, '}}]', ']'), '[{{', '['), '""value"":', ''), '}},{{', ',')) as tagsJson 
									,case when F.id is null then 0 else 1 end as hasTagField
									,AP.[Segments]";
			string filterSelectFields = "*";
			string assetDetailSQL = "";

			List<string> filters = new List<string>();

			var asset = CompanyContext.Filter<Asset>(o => o.uid == assetUid).FirstOrDefault();

			if (asset == null)
			{
				throw new GenericException(System.Net.HttpStatusCode.NotFound, "", CommentErrors.AssetUidNotFound);
			}

			AssetDataProfile dataprofile = CompanyContext.AssetDataProfile.Where(x => x.AssetId == asset.ID).OrderByDescending(x => x.ProfileSetDate).FirstOrDefault();
			if (dataprofile == null)
			{
				throw new GenericException(System.Net.HttpStatusCode.NotFound, "", OthersError.ProfileRecordNotExists);
			}

			if (similarType == null)
			{
				throw new GenericException(System.Net.HttpStatusCode.NotFound, "", OthersError.SignatureTypeRequired);
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
					throw new GenericException(System.Net.HttpStatusCode.NotFound, "", OthersError.SignatureTypeInvalid);
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

			if (hasAdvancedFilter || isExport)
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
					orderBySQL = $@"hasTagField {(orderDirection == "asc" ? "desc" : "asc")}, JSON_VALUE('{{""tags"":'+ISNULL(tagsJson, '[]')+'}}', '$.tags[0]') {orderDirection}, td.[path] {orderDirection}";
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
							  outer apply (select 1 as [value] from ResponsibilityDetail RD where resourceid = @userid and ((RD.AssetID = A.id and applyToType=0) or (RD.AssetID = 0 and RD.AssetTypeID=A.AssetTypeID)) and RD.PermissionsBitMask & {(int)Permission.ReadAsset} = 0) hasAccess";

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
								,A.AssetTypeID
								,AST.Uid as AssetTypeUid
								,A.ID";

				sqlJoins = $@"{sqlJoins}
							INNER JOIN
							AssetType AST on A.AssetTypeID=AST.ID ";

				assetDetailSQL = $@"drop table if exists #tempAssetDetails;

									select 
										A.ID as AssetID
										,A.Uid as AssetUid
										,AP.DisplayPath as AssetPath
										,ATP.Path as AssetTypePath
										,tagString as AssetTags
									into #tempAssetDetails
									from
										Asset A
										INNER JOIN
										AssetPath AP on A.ID=AP.ID
										cross apply dbo.GetAssetTypeTextPathById(A.AssetTypeID, ' > ') ATP
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
													AT.AssetID = A.ID
												order by T.Value OFFSET 0 ROWS) T
										) as Tags
									where A.ID=@assetId";

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
										,P.Path as MatchedAssetTypePath
										,td.hasTagField";
			}

			string tempTablesSQL = $@"drop table if exists #tempadpid;
							create table #tempadpid (Assetid bigint,ProfileSetDate datetime)

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

		private string GetDataProfilesBaseSQL(bool includeSamples, string joinSql = null)
        {
			return $@"select
								A.[Uid] as assetUid
								,ADP.[ProfileSetDate]
								,ADP.[ProfileIdentifier]
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
								{(includeSamples ? ",JSON_QUERY(outlierDetail.[value]) as outlierDetail" : "")}
								,ADP.[KeyConfidence]
								,ADP.[DataSignature]
								,ADP.[StructureSignature]
								,ADP.[Cardinality]
								{(includeSamples ? ",JSON_QUERY(cardinalityDetail.[value]) as cardinalityDetail" : "")}
								,ADP.[ShapeCardinality] as shapesCardinality
								{(includeSamples ? ",JSON_QUERY(shapesDetail.[value]) as shapesDetail" : "")}
								{(includeSamples ? ",JSON_QUERY(scriptDistributionStatistics.[value]) as scriptDistributionStatistics" : "")}
								{(includeSamples ? ",JSON_QUERY(characterCasingStatistics.[value]) as characterCasingStatistics" : "")}
								{(includeSamples ? ",JSON_QUERY(characterDataTypeStatistics.[value]) as characterDataTypeStatistics" : "")}
								{(includeSamples ? ",JSON_QUERY(characterSpacingStatistics.[value]) as characterSpacingStatistics" : "")}
								{(includeSamples ? ",JSON_QUERY(specialCharacterStatistics.[value]) as specialCharacterStatistics" : "")}
								{(includeSamples ? ",JSON_QUERY(percentileStatistics.[value]) as percentileStatistics" : "")}
								{(includeSamples ? $@",JSON_QUERY(replace(REPLACE(REPLACE(REPLACE(textPatternDetails.value, '}}]', ']'), '[{{', '['), '""value"":', ''), '}},{{', ',')) as textPatternDetails" : "")}
								{(includeSamples ? $@",JSON_QUERY(REPLACE(REPLACE(semanticAnalysisDetails.value,'""value"":{{',''),'}}}}','}}')) as semanticAnalysisDetails" : "")}
								{(includeSamples ? $@",JSON_QUERY(REPLACE(REPLACE(confidenceAnalysisDetails.value,'""value"":{{',''),'}}}}','}}')) as confidenceAnalysisDetails" : "")}
								{(includeSamples ? $@",JSON_QUERY(REPLACE(REPLACE(tableStructureInfo.value,'""value"":{{',''),'}}}}','}}')) as tableStructureInfo" : "")}
								{(includeSamples ? $@",JSON_QUERY(replace(REPLACE(REPLACE(REPLACE(bottomK.value, '}}]',']'), '[{{','['), '""value"":',''), '}},{{',',')) as bottomK" : "")}
								{(includeSamples ? $@",JSON_QUERY(replace(REPLACE(REPLACE(REPLACE(topK.value, '}}]', ']'), '[{{', '['), '""value"":', ''), '}},{{', ',')) as topK" : "")}
								,ADP.TotalCount
								,ADP.UniqueCount
								,ADP.OutlierCount
								,ADP.DetectionLocale
								,ADP.FtaVersion
								,ADP.DecimalSeparator
								,ADP.PopularityCount
								,ADP.IsAuthorizedForPopularity
								,ADP.SourceLastModified
								,ADP.FilterCount
							from 
								AssetDataProfile ADP
								{(!string.IsNullOrWhiteSpace(joinSql) ? joinSql : "")}
								Inner Join Asset A on A.ID = ADP.AssetID    
							{(includeSamples ? $@"
									outer apply (            
									select  (
											select [key], [value] as Count
																from #tempADPS
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
															from #tempADPS
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
															from #tempADPS
															where
																AssetDataProfileId = ADP.ID
																and
																lower(SampleType) = 'shapesdetail'
															for json path
															) as [value]
													) shapesDetail
									outer apply (
													select  (
															select [key], [value] as Count
															from #tempADPS
															where
																AssetDataProfileId = ADP.ID
																and
																lower(SampleType) = 'scriptdistributionstatistics'
															for json path
															) as [value]
													) scriptDistributionStatistics
									outer apply (
													select  (
															select [key], [value] as Count
															from #tempADPS
															where
																AssetDataProfileId = ADP.ID
																and
																lower(SampleType) = 'charactercasingstatistics'
															for json path
															) as [value]
													) characterCasingStatistics
									outer apply (
													select  (
															select [key], [value] as Count
															from #tempADPS
															where
																AssetDataProfileId = ADP.ID
																and
																lower(SampleType) = 'characterdatatypestatistics'
															for json path
															) as [value]
													) characterDataTypeStatistics
									outer apply (
													select  (
															select [key], [value] as Count
															from #tempADPS
															where
																AssetDataProfileId = ADP.ID
																and
																lower(SampleType) = 'characterspacingstatistics'
															for json path
															) as [value]
													) characterSpacingStatistics
									outer apply (
													select  (
															select [key], [value] as Count
															from #tempADPS
															where
																AssetDataProfileId = ADP.ID
																and
																lower(SampleType) = 'specialcharacterstatistics'
															for json path
															) as [value]
													) specialCharacterStatistics
									outer apply (
													select  (
															select [key], [value] as Count
															from #tempADPS
															where
																AssetDataProfileId = ADP.ID
																and
																lower(SampleType) = 'percentilestatistics'
															for json path
															) as [value]
													) percentileStatistics
									outer apply (
													select  (
															select [value]
															from #tempADPS
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
															from #tempADPS
															where
																AssetDataProfileId = ADP.ID
																and
																lower(SampleType) = 'topk'
															for json path
															) as [value]
													) topK 
								outer apply (
								
													select  (
															select json_query([value]) as [value]
															from AssetDataProfileSampleJson
															where
																AssetDataProfileId = ADP.ID
																and
																lower(SampleType) = 'textpatterndetails'
															for json path
															) as [value]
								) as textPatternDetails
								outer apply (
								
													select  (
															select json_query([value]) as [value]
															from AssetDataProfileSampleJson
															where
																AssetDataProfileId = ADP.ID
																and
																lower(SampleType) = 'semanticAnalysisdetails'
															for json path
															) as [value]
								) as semanticAnalysisDetails
								outer apply (								
													select  (
															select json_query([value]) as [value]
															from AssetDataProfileSampleJson
															where
																AssetDataProfileId = ADP.ID
																and
																lower(SampleType) = 'confidenceAnalysisDetails'
															for json path
															) as [value]
								) as confidenceAnalysisDetails
								outer apply (								
													select  (
															select top 1 json_query([value]) as [value]
															from AssetDataProfileSampleJson
															where
																AssetDataProfileId = ADP.ID
																and
																lower(SampleType) = 'tableStructureInfo'
															for json path, WITHOUT_ARRAY_WRAPPER
															) as [value]
								) as tableStructureInfo
"
					: "")}";
		}

		public async Task<bool> DoesTypeQualifierExist(string typeQualifier)
		{
			return (await CompanyContext.QueryAsync<int>("select count(1) from AssetDataProfile where TypeQualifier = @typeQualifier", new { typeQualifier })).FirstOrDefault() > 0;
		}

		public async Task<bool> DoesSemanticTypeExist(string qualifier)
		{
			return (await CompanyContext.QueryAsync<int>("select count(1) from Semantic where qualifier = @qualifier", new { qualifier })).FirstOrDefault() > 0;
		}

		public async Task<List<DataProfileDeleteResponse>> DeleteAsync(Asset asset, ApiExecution execution, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var startDate = new DateTime(1800, 1, 1);//Can't use MinValue as that is 01/01/0001 but SQL server min is 01/01/1759
			var endDate = DateTime.MaxValue;
			var cascade = false;

			var hasStartDate = false;
			var hasEndDate = false;

			if (queryParams.ToList().Any(k => k.Key.ToLower() == "_startdate" || k.Key.ToLower() == "_enddate"))
			{
				if (queryParams.ToList().Any(k => k.Key.ToLower() == "_startdate"))
				{
					hasStartDate = DateTime.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_startdate").Value, out startDate);
				}

				if (queryParams.ToList().Any(k => k.Key.ToLower() == "_enddate"))
				{
					hasEndDate = DateTime.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_enddate").Value, out endDate);
				}
			}

			if(!hasStartDate && !hasEndDate)
			{
				var profile = CompanyContext.AssetDataProfile.Where(adp => adp.AssetId == asset.ID).OrderByDescending(adp => adp.ProfileSetDate).FirstOrDefault();
				if(profile == null)
				{
					return new List<DataProfileDeleteResponse> { new DataProfileDeleteResponse { uid = asset.uid, DeletedCount = 0 } };
				}
				startDate = endDate = profile.ProfileSetDate;
			}

			if (queryParams.Any(qp => qp.Key.ToLower() == "_cascade"))
			{
				bool.TryParse(queryParams.FirstOrDefault(q => q.Key.ToLower() == "_cascade").Value, out cascade);				
			}

			return await DeleteAsync(asset, startDate, endDate, execution, cascade);
		}
	}
}
