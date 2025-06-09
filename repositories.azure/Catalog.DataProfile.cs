using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;
using Dapper;
using System.Diagnostics;
using Newtonsoft.Json;
using repositories.azure.extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Security;
using System.Transactions;


namespace repositories.azure
{
	public partial class Catalog
	{

		public async Task<RepositoryResponse<AssetDataProfilesApiViewModel>> ReadDataProfilesAsync(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			RepositoryResponse<AssetDataProfilesApiViewModel> response = new(null, 200, true);

			var results = new AssetDataProfilesApiViewModel
			{
				pageNum = queryParams.CheckForPageNumber(),
				pageSize = queryParams.CheckForPageSize()
			};
			string offset = "";
			if (results.pageSize > 0 || results.pageNum > 0)
			{
				offset = $"offset {results.pageSize * (results.pageNum - 1)} rows fetch next {results.pageSize} rows only";
			}


			List<string> whereClauses = new List<string>();
			var dbArgs = new DynamicParameters();

			if (queryParams.IsQueryParameterPresent("_filter"))
			{
				List<FilterColumnOption> fieldList = new List<FilterColumnOption>
						{
							new FilterColumnOption("assetUid", "A.Uid", SqlFieldType.Guid),
							new FilterColumnOption("ProfileIdentifier", "ADP.ProfileIdentifier", SqlFieldType.Text),
							new FilterColumnOption("profileSetDate", "ADP.profileSetDate", SqlFieldType.DateTime),
							new FilterColumnOption("typeQualifier", "ADP.typeQualifier", SqlFieldType.Text),
							new FilterColumnOption("type", "ADP.type", SqlFieldType.Text),
							new FilterColumnOption("ftaVersion", "ADP.ftaVersion", SqlFieldType.Text),
							new FilterColumnOption("freshness", "ADP.freshness", SqlFieldType.Number),
							new FilterColumnOption("ProfileSource", "ADP.ProfileSource", SqlFieldType.Text),
							new FilterColumnOption("ProfileSeries", "ADP.ProfileSeries", SqlFieldType.Text),
							new FilterColumnOption("ProfileType", "coalesce(ADP.ProfileType,0)", SqlFieldType.Number),
						};

				// Parse and get back any advanced filters, and load dbArguments and where clauses.
				var advancedFilters = queryParams.ParseODataFilters();
				(dbArgs, whereClauses) = advancedFilters.ConvertToSqlFilters(fieldList);
			}

			var whereConditions = whereClauses.Count > 0 ? $"where {string.Join(" AND ", whereClauses)}" : "";

			bool includeTotal = true;
			if (queryParams.IsQueryParameterPresent("_includetotal"))
			{
				string includeTotalStringValue = queryParams.ReadQueryParameterValue("_includetotal");
				bool.TryParse(includeTotalStringValue, out includeTotal);
			}

			bool includeSamples = true;
			if (queryParams.IsQueryParameterPresent("_includesamples"))
			{
				string includesamplesStringValue = queryParams.ReadQueryParameterValue("_includesamples");
				bool.TryParse(includesamplesStringValue, out includeSamples);
			}

			bool includeChildAssets = false;
			if (queryParams.IsQueryParameterPresent("_includechildassets"))
			{
				string includeChildAssetsStringValue = queryParams.ReadQueryParameterValue("_includechildassets");
				bool.TryParse(includeChildAssetsStringValue, out includeChildAssets);
			}

			var descendantsSQL = $@"with descendants as (select @assetID as ID)";

			if (includeChildAssets)
			{
				descendantsSQL = $@"with descendants as (
										select @assetID as ID
										union all
										select	AAP.ObjectAssetID as ID
										from	descendants as d
												inner join PredicateIntersect AAP on AAP.SubjectAssetID = d.ID and AAP.PredicateType in (3,4)
									)";
			}

			bool hasAssetUidParam = false;
			if (queryParams.IsQueryParameterPresent("_assetuid"))
			{
				hasAssetUidParam = true;
				string assetuidStringValue = queryParams.ReadQueryParameterValue("_assetuid");
				if (Guid.TryParse(assetuidStringValue, out Guid assetUid))
				{
					using (var connection = ConnectionProvider.Connect(true))
					{
						int id = await connection.QueryFirstOrDefaultAsync<int>("select id from asset where uid = @assetUid", new {assetUid});
						if (id == 0)
						{
							response.StatusCode = 404;
							response.IsSuccess = false;
							response.Message = Error.AssetUidNotFound;
						}
						else
						{
							dbArgs.Add("@assetId", id);
						}
					}
				}
			}

			if (response.IsSuccess)
			{
				var dataProfileIdsSql = $@"
						drop table if exists #tempADPS;
						drop table if exists #assetdataprofileids
						create table #assetdataprofileids (
							id bigint, 
							ProfileSetDate DateTime
						);

						{(hasAssetUidParam ? descendantsSQL : "")}

						insert into #assetdataprofileids
						select ADP.ID, ADP.[ProfileSetDate]
						from 
						AssetDataProfile ADP inner join 
						{(hasAssetUidParam ? "descendants" : "asset")} A on A.id = ADP.AssetID
						{whereConditions}
						order by ADP.[ProfileSetDate] desc
						{offset}";

				var dataProfileSamplesSql = $@"
						select adps.AssetDataProfileId,adps.SampleType,adps.[Key],adps.[Value]
						into #tempADPS
						from #assetdataprofileids tempADP
						inner join AssetDataProfileSample adps on adps.AssetDataProfileId = tempADP.ID;";

				string dataProfileSQL = GetDataProfilesBaseSQL(includeSamples, "inner join #assetdataprofileids ids on ids.ID = ADP.ID");


				var countSql = $@"
							{(hasAssetUidParam ? descendantsSQL : "")}

							  select count(1) 
							  from AssetDataProfile ADP 
							  inner join {(hasAssetUidParam ? "descendants" : "asset")} A on A.id = ADP.AssetID 
							  {whereConditions}
							  option (recompile)";


				string sql = $@"
						{dataProfileIdsSql}

						{dataProfileSamplesSql}

						{dataProfileSQL}
						order by ADP.[ProfileSetDate] desc
						for Json Path
						option (recompile)";
				using (var connection = ConnectionProvider.Connect(true))
				{
					if (includeTotal)
					{
						results.total = await connection.QueryFirstOrDefaultAsync<int>(countSql, dbArgs, commandTimeout: CommandTimeout);
					}
					else
					{
						results.total = null;
					}

					var jsonStrings = await connection.QueryAsync<string>(sql, dbArgs, commandTimeout: CommandTimeout);
					var json = string.Join("", jsonStrings);

					results.items = JsonConvert.DeserializeObject<List<DataProfileModel>>(string.IsNullOrEmpty(json) ? "[]" : json);
					response.Data = results;
				}
			}
			return response;
		}

		
		public async Task<RepositoryResponse<AssetDataProfilesApiViewModel>> ReadDataProfilesAsync(Guid assetUid, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			RepositoryResponse<AssetDataProfilesApiViewModel> response = new(null, 200, true);

			var dbArgs = new DynamicParameters();
			var results = new AssetDataProfilesApiViewModel
			{
				pageNum = queryParams.CheckForPageNumber(),
				pageSize = queryParams.CheckForPageSize()
			};
			string offset = "";
			if (results.pageSize > 0 || results.pageNum > 0)
			{
				offset = $"offset {results.pageSize * (results.pageNum - 1)} rows fetch next {results.pageSize} rows only";
			}

			string whereConditions = $@"Where ADP.ProfileSetDate between @startDate and @endDate";

			long assetid = 0;

			using (var connection = ConnectionProvider.Connect(true))
			{
				assetid = await connection.QueryFirstOrDefaultAsync<int>("select id from asset where uid = @assetUid", new { assetUid });
				if (assetid == 0)
				{
					response.StatusCode = 404;
					response.IsSuccess = false;
					response.Message = Error.AssetUidNotFound;
					return response;
				}
			}

			bool includeTotal = true;
			if (queryParams.IsQueryParameterPresent("_includetotal"))
			{
				string includeTotalStringValue = queryParams.ReadQueryParameterValue("_includetotal");
				bool.TryParse(includeTotalStringValue, out includeTotal);
			}

			bool includeChildAssets = false;

			if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includechildassets"))
			{
				bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_includechildassets").Value, out includeChildAssets);
			}

			bool includeSamples = true;
			if (queryParams.IsQueryParameterPresent("_includesamples"))
			{
				string includesamplesStringValue = queryParams.ReadQueryParameterValue("_includesamples");
				bool.TryParse(includesamplesStringValue, out includeSamples);
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
				AssetDataProfile dataprofile = new AssetDataProfile();
				List<AssetDataProfileSample> dataProfileSamples = new List<AssetDataProfileSample>();
				List<AssetDataProfileSampleJson> dataProfileDetails = new List<AssetDataProfileSampleJson>();


				using (var connection = ConnectionProvider.Connect(true))
				{
						string sqlqry = "Select * from AssetDataProfile where AssetId = @assetid";
						dataprofile = await connection.QueryFirstOrDefaultAsync<AssetDataProfile>(sqlqry, new { assetid }, commandTimeout: CommandTimeout);
						if (dataprofile != null)
						{
							startDate = endDate = dataprofile.ProfileSetDate;
							if (includeSamples)
							{
								sqlqry = "select * from AssetDataProfileSample where AssetDataProfileID = @AssetDataProfileID";
								dataProfileSamples = (await connection.QueryAsync<AssetDataProfileSample>(sqlqry, new { AssetDataProfileID = dataprofile.ID}, commandTimeout: CommandTimeout)).ToList();

								sqlqry = "select * from AssetDataProfileSampleJson where AssetDataProfileID = @AssetDataProfileID";
								dataProfileDetails = (await connection.QueryAsync<AssetDataProfileSampleJson>(sqlqry, new { AssetDataProfileID = dataprofile.ID }, commandTimeout: CommandTimeout)).ToList();
						}
					}
				}


				if (dataprofile != null)
				{
					if (!includeChildAssets)
					{
						results.items = new List<DataProfileModel> { new DataProfileModel(assetUid, dataprofile, dataProfileSamples, dataProfileDetails) };

						if (includeTotal)
						{
							results.total = 1;
						}
						else
						{
							results.total = null;
						}

						response.Data = results;

						return response;
					}
				}
				else
				{
					//no profiling records
					results.items = new List<DataProfileModel>();
					response.Data = results;

					return response;
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
						inner join AssetDataProfileSample adps on adps.AssetDataProfileId = ADP.ID;

						create clustered index cx_tempADPS on #tempADPS(AssetDataProfileId);
						";

			}

			string dataProfileSQL = GetDataProfilesBaseSQL(includeSamples, "inner join #assetdataprofileids ids on ids.ID = ADP.ID");

			dbArgs.Add("@startDate", startDate);
			dbArgs.Add("@endDate", endDate);
			dbArgs.Add("@assetId", assetid);

			string sql = $@"
						drop table if exists #tempADPS;

						{dataProfileIdsSql}
						order by ADP.[ProfileSetDate] desc
						{offset}

						{dataProfileSamplesSql}

						{dataProfileSQL}
						order by ADP.[ProfileSetDate] desc
						for Json Path";

			var countSQL = $@"
								{descendantsSQL}
								select 
									COUNT(*)
								from      
									descendants A
									inner join 
									AssetDataProfile ADP on adp.AssetID = A.AssetID	                                    
								{whereConditions}";

			using (var connection = ConnectionProvider.Connect(true))
			{
					var jsonStrings = await connection.QueryAsync<string>(sql, dbArgs, commandTimeout: CommandTimeout);
					var json = string.Join("", jsonStrings);
					results.items = JsonConvert.DeserializeObject<List<DataProfileModel>>(string.IsNullOrEmpty(json) ? "[]" : json);

					if (includeTotal)
					{
						results.total = await connection.QueryFirstOrDefaultAsync< int>(countSQL, dbArgs, commandTimeout: CommandTimeout);
				}
					else
					{
						results.total = null;
					}
					response.Data = results;
			}

			return response;
		}

		public async Task<RepositoryResponse<AssetDataProfilesApiViewModel>> ReadDataProfilesAsync(string profileIdentifier, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			RepositoryResponse<AssetDataProfilesApiViewModel> response = new(null, 200, true);

			var dbArgs = new DynamicParameters();
			var results = new AssetDataProfilesApiViewModel
			{
				pageNum = queryParams.CheckForPageNumber(),
				pageSize = queryParams.CheckForPageSize()
			};
			string offset = "";
			if (results.pageSize > 0 || results.pageNum > 0)
			{
				offset = $"offset {results.pageSize * (results.pageNum - 1)} rows fetch next {results.pageSize} rows only";
			}
			string whereConditions = $@" Where ADP.ProfileIdentifier = @profileIdentifier ";


			Guid assetUid;
			if (queryParams.IsQueryParameterPresent("_assetuid"))
			{
				string assetUidstring = queryParams.ReadQueryParameterValue("_assetuid");
				if (Guid.TryParse(assetUidstring, out assetUid))
				{
					using (var connection = ConnectionProvider.Connect(true))
					{
						int id = await connection.QueryFirstOrDefaultAsync<int>("select id from asset where uid = @assetUid", new { assetUid });
						if (id == 0)
						{
							response.StatusCode = 404;
							response.IsSuccess = false;
							response.Message = Error.AssetUidNotFound;
							return response;
						}
						else
						{
							dbArgs.Add("@assetId", id);
							whereConditions += " and ADP.AssetID = @assetId ";
						}
					}
				}
			}

			bool includeTotal = true;
			if (queryParams.IsQueryParameterPresent("_includetotal"))
			{
				string includeTotalStringValue = queryParams.ReadQueryParameterValue("_includetotal");
				bool.TryParse(includeTotalStringValue, out includeTotal);
			}

			bool includeSamples = true;
			if (queryParams.IsQueryParameterPresent("_includesamples"))
			{
				string includesamplesStringValue = queryParams.ReadQueryParameterValue("_includesamples");
				bool.TryParse(includesamplesStringValue, out includeSamples);
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

			string dataProfileSQL = GetDataProfilesBaseSQL(includeSamples, "inner join #assetdataprofileids ids on ids.ID = ADP.ID");

			dbArgs.Add("@profileIdentifier", profileIdentifier);


			string sql = $@"
						{dataProfileIdsSql}

						{dataProfileSamplesSql}

						{dataProfileSQL}
						order by ADP.[ProfileSetDate] desc
						for Json Path";

			var countSql = $"select count(*) from AssetDataProfile ADP {whereConditions}";

			using (var connection = ConnectionProvider.Connect(true))
			{
				if (includeTotal)
				{
					results.total = await connection.QueryFirstOrDefaultAsync<int>(countSql, dbArgs, commandTimeout: CommandTimeout);
				}
				else
				{
					results.total = null;
				}

				var jsonStrings = await connection.QueryAsync<string>(sql, dbArgs, commandTimeout: CommandTimeout);
				var json = string.Join("", jsonStrings);
				results.items = JsonConvert.DeserializeObject<List<DataProfileModel>>(string.IsNullOrEmpty(json) ? "[]" : json);
				response.Data = results;
			}

			return response;
		}

		public async Task<RepositoryResponse<List<ProfilesSeriesApiViewModel>>> ReadDataProfilesSeriesAsyn(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			RepositoryResponse<List<ProfilesSeriesApiViewModel>> response = new(null, 200, true);

			var dbArgs = new DynamicParameters();
			List<string> whereClauses = new List<string>();

			if (queryParams.IsQueryParameterPresent("_filter"))
			{
				List<FilterColumnOption> fieldList = new List<FilterColumnOption>
						{
							new FilterColumnOption("assetUid", "A.Uid", SqlFieldType.Guid),
							new FilterColumnOption("ProfileIdentifier", "ADP.ProfileIdentifier", SqlFieldType.Text),
							new FilterColumnOption("profileSetDate", "ADP.profileSetDate", SqlFieldType.DateTime),
							new FilterColumnOption("typeQualifier", "ADP.typeQualifier", SqlFieldType.Text),
							new FilterColumnOption("type", "ADP.type", SqlFieldType.Text),
							new FilterColumnOption("ftaVersion", "ADP.ftaVersion", SqlFieldType.Text),
							new FilterColumnOption("freshness", "ADP.freshness", SqlFieldType.Number),
							new FilterColumnOption("ProfileSource", "ADP.ProfileSource", SqlFieldType.Text),
							new FilterColumnOption("ProfileSeries", "ADP.ProfileSeries", SqlFieldType.Text),
							new FilterColumnOption("ProfileType", "coalesce(ADP.ProfileType,0)", SqlFieldType.Number),
						};

				// Parse and get back any advanced filters, and load dbArguments and where clauses.
				var advancedFilters = queryParams.ParseODataFilters();//.ParseAdvancedFilters();
				(dbArgs, whereClauses) = advancedFilters.ConvertToSqlFilters(fieldList);
			}

			var additionalWhereClause = whereClauses.Count > 0 ? $"And {string.Join(" AND ", whereClauses)}" : "";

			string sql = $@"
							select distinct ADP.ProfileSeries
							from dbo.AssetDataProfile ADP
							inner join Asset a on A.id = ADP.AssetID
						    where ADP.ProfileSeries is not null 
							{additionalWhereClause}
							order by 1";

			using (var connection = ConnectionProvider.Connect(true))
			{
				List<ProfilesSeriesApiViewModel>  results = (await connection.QueryAsync<ProfilesSeriesApiViewModel>(sql, dbArgs, commandTimeout: CommandTimeout)).ToList();

				response.Data =  results;

			}
			return response;
		}

		public async Task<RepositoryResponse<AssetDataProfilesMatchingAssetsApiViewModel>> ReadMatchingAssets(Guid assetUid, string similarType, IEnumerable<KeyValuePair<string, string>> queryParams, bool onlyTotal = false)
		{
			RepositoryResponse<AssetDataProfilesMatchingAssetsApiViewModel> response = new(null, 200, true);

			var dbArgs = new DynamicParameters();
			var results = new AssetDataProfilesMatchingAssetsApiViewModel
			{
				pageNum = queryParams.CheckForPageNumber(),
				pageSize = queryParams.CheckForPageSize()
			};

			bool includeTotal = true;
			if (queryParams.IsQueryParameterPresent("_includetotal"))
			{
				string includeTotalStringValue = queryParams.ReadQueryParameterValue("_includetotal");
				bool.TryParse(includeTotalStringValue, out includeTotal);
			}

			var responsequery = await BuildMatchAssetsSQL(assetUid, similarType, queryParams, dbArgs, onlyTotal);

			string sql;

			if (!responsequery.IsSuccess)
			{
				response.StatusCode = responsequery.StatusCode;
				response.IsSuccess = responsequery.IsSuccess;
				response.Message = responsequery.Message;
				return response;
			}
			else
			{
				sql = responsequery.Data;
			}

			using (var connection = ConnectionProvider.Connect(true))
			{
				if (onlyTotal)
				{
					results.total = await connection.QueryFirstOrDefaultAsync<int>(sql, dbArgs, commandTimeout: CommandTimeout);
					response.Data = results;
					return response;
				}

				var multiQuery = await connection.QueryMultipleAsync(sql, dbArgs, commandTimeout: CommandTimeout);
				results.items = multiQuery.Read<AssetDataProfileMatchingAssetsModel>().ToList();

				if (includeTotal)
				{
					results.total = multiQuery.Read<int?>().FirstOrDefault();
				}
				else
				{
					results.total = null;
				}
				response.Data = results;
			}
			return response;
		}

		public async Task<RepositoryResponse<IEnumerable<DataProfileExportModel>>> GetMatchedAssetsForExport(Guid assetUid, string similarType, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			RepositoryResponse<IEnumerable<DataProfileExportModel>> response = new(null, 200, true);

			var dbArgs = new DynamicParameters();

			var responsequery = await BuildMatchAssetsSQL(assetUid, similarType, queryParams, dbArgs, isExport: true);

			string sql;

			if (!responsequery.IsSuccess)
			{
				response.StatusCode = responsequery.StatusCode;
				response.IsSuccess = responsequery.IsSuccess;
				response.Message = responsequery.Message;
				return response;
			}
			else
			{
				sql = responsequery.Data;
			}

			using (var connection = ConnectionProvider.Connect(true))
			{
				var results = await connection.QueryAsync<DataProfileExportModel>(sql, dbArgs, commandTimeout: CommandTimeout);
				response.Data = results;
			}
			return response;
		}

		public async Task<RepositoryResponse<AssetDataProfileByTypeQualifierApiViewModel>> ReadAssetsByTypeQualifier(string typeQualifier, decimal minConfidence, IEnumerable<KeyValuePair<string, string>> queryParams, bool isExport = false)
		{
			RepositoryResponse<AssetDataProfileByTypeQualifierApiViewModel> response = new(null, 200, true);

			var dbArgs = new DynamicParameters();
			var results = new AssetDataProfileByTypeQualifierApiViewModel
			{
				pageNum = queryParams.CheckForPageNumber(),
				pageSize = queryParams.CheckForPageSize()
			};

			string offset = "";
			if (results.pageSize > 0 || results.pageNum > 0)
			{
				offset = $"offset {results.pageSize * (results.pageNum - 1)} rows fetch next {results.pageSize} rows only";
			}

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

			string parameterName = "_direction";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string[] allowedValues = new[] { "asc", "desc" };
				string directionFilter = queryParams.ReadQueryParameterValue(parameterName);
				if (!allowedValues.Contains(directionFilter))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = Error.InvalidDirection;
					return response;
				}
				else
				{
					orderDirection = directionFilter;
				}
			}

			parameterName = "_order";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string[] allowedValues = new[] { "confidence", "path", "assettypepath" };
				string orderFilter = queryParams.ReadQueryParameterValue(parameterName);
				if (!allowedValues.Contains(orderFilter))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = Error.InvalidOrder;
					return response;
				}
				else
				{
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
			}

			if (string.IsNullOrEmpty(typeQualifier) || typeQualifier.Length > 200)
			{
				response.IsSuccess = false;
				response.StatusCode = 400;
				response.Message = Error.TypeQualifierInvalid;
				return response;
			}


			if (!await DoesTypeQualifierExist(typeQualifier) && !await DoesSemanticTypeExist(typeQualifier))
			{
				response.IsSuccess = false;
				response.StatusCode = 404;
				response.Message = String.Format(Error.TypeQualifierNotFound, typeQualifier);
				return response;
			}

			if (minConfidence <= 0 || minConfidence > 1)
			{
				response.IsSuccess = false;
				response.StatusCode = 400;
				response.Message = string.Format(Error.InvalidParameter, "minConfidence");
				return response;
			}

			includeTotal = true;
			parameterName = "_includetotal";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string includeTotalStringValue = queryParams.ReadQueryParameterValue(parameterName);
				bool.TryParse(includeTotalStringValue, out includeTotal);
			}

			if (queryParams.IsQueryParameterPresent("_filter"))
			{
				List<FilterColumnOption> fieldList = new List<FilterColumnOption>
						{
							new FilterColumnOption("assetTypePath", "P.Path", SqlFieldType.Text),
							new FilterColumnOption("Path", "AP.[Segments]", SqlFieldType.Xml),
							new FilterColumnOption("outOfDate", "(case when CAST(s.effectiveDate as DATE) > ADP.profileSetDate then 'true' else 'false' end)", SqlFieldType.Boolean),
						};

				// Parse and get back any advanced filters, and load dbArguments and where clauses.
				var advancedFilters = queryParams.ParseODataFilters();//.ParseAdvancedFilters();
				(dbArgs, filters) = advancedFilters.ConvertToSqlFilters(fieldList);
			}

			parameterName = "_simplefilter";
			if (queryParams.ToList().Any(x => x.Key.ToLower() == parameterName))
			{
				var simpleFilter = queryParams.FirstOrDefault(x => x.Key.ToLower() == parameterName).Value.Trim();
				if (!string.IsNullOrEmpty(simpleFilter))
				{
					simpleFilter = GetEscapedFilterString(simpleFilter);

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

			if (!CurrentUserIsAdmin)
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

				dbArgs.Add("@userid", CurrentUserId);
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

			var countSQL = $@"
                            SELECT 
	                            Count(*)
                            FROM                                     
	                            {sqlJoins}		                            
	                            {whereConditions}
                                {filterSQL}
		                    ";

			using (var connection = ConnectionProvider.Connect(true))
			{
				results.items = await connection.QueryAsync<AssetDataProfileByTypeQualifierModel>(itemsSQL, dbArgs, commandTimeout: CommandTimeout);

				if (includeTotal)
				{
					results.total = await connection.QueryFirstOrDefaultAsync<int>(countSQL, dbArgs, commandTimeout: CommandTimeout);
					response.Data = results;
				}
				else
				{
					results.total = null;
					response.Data = results;
				}
			}
			return response;
		}


		public async Task<RepositoryResponse<List<DataProfileDeleteResponse>>> RemoveDataProfileAsync(Guid assetUid, DateTime startDate, DateTime endDate, ApiExecution execution, bool cascade = false)
		{
			RepositoryResponse<List<DataProfileDeleteResponse>> response = new RepositoryResponse<List<DataProfileDeleteResponse>>(null, 200, true, null, null);

			if (!CurrentUserIsAdmin)
			{
				var Permissions = await HasAssetPermissionByUid(assetUid, Permission.EditAsset,true);

				if (!Permissions)
				{
					response.StatusCode = 403;
					response.IsSuccess = false;
					response.Message = Error.Non_Auth_Mess;
					return response;
				}
			}

			var asset = await ReadAssetwithAssetTypeAsync(assetUid);

			if (asset == null)
			{
				response.StatusCode = 400;
				response.IsSuccess = false;
				response.Message = string.Format(Error.AssetUidIsNotValid, assetUid.ToString());

				return response;
			}

			string sqlqry = "";
			using (var connection = ConnectionProvider.Connect(true))
			{

				sqlqry = "select count(1) from AssetDataProfile where AssetId = @AssetID and ProfileSetDate between @startDateDate and @endDateDate";
				int recordCount = await connection.QueryFirstAsync<int>(sqlqry, new { AssetID = asset.ID, @startDateDate = startDate.Date, @endDateDate =endDate.Date }, commandTimeout: CommandTimeout);

				if (recordCount > MAX_SYNCHRONOUS_API_ITEM_COUNT)
				{
					response.StatusCode = 400;
					response.IsSuccess = false;
					response.Message = string.Format(Error.DataProfileDeleteMaxLimit, MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString());
					return response;
				}

				if (startDate > endDate)
				{
					response.StatusCode = 400;
					response.IsSuccess = false;
					response.Message = Error.StartEndDateValidation;
					return response;
				}
			}


			await UpsertApiExecution(execution);

			var assetDataProfileDeleteModel = new AssetDataProfileDeleteModel { AssetUid = asset.uid, StartDate = startDate, EndDate = endDate, Cascade = cascade };

			List<AssetDataProfileDeleteModel> models = new List<AssetDataProfileDeleteModel>
			{
				assetDataProfileDeleteModel
			};

			var responseresult = await RemoveDataProfilesAsync(models, execution);

			if (!responseresult.IsSuccess)
			{
				if (responseresult.Data != null)
				{
					response.Ex = responseresult.Data;
				}
				await UpdateExecutionWithErrorFromException(execution, responseresult.Data);
			}

			await completeApiExecutionAndGetCounts(ApiExecutionAction.DeleteDataProfile, execution.Id, execution.ExecutionID);
			var results = await GetExecutionDataProfileDeleteResultsAsync(execution.ExecutionID);
			response.Data = results;

			response.StatusCode = responseresult.StatusCode;
			response.IsSuccess = responseresult.IsSuccess;
			response.Message = responseresult.Message;
			return response;
		}

		public async Task<RepositoryResponse<Exception>> RemoveDataProfilesAsync(List<AssetDataProfileDeleteModel> models, ApiExecution execution, int timeout = 3600)
		{
			RepositoryResponse<Exception> response = new RepositoryResponse<Exception>(null, 200, true);

			bool generalChecksCompleted = false;
			int itemNumber = 1;
			CurrentExecutionLocationModel currentLocation = null;
			Dictionary<string, double> metrics = new Dictionary<string, double>();
			Stopwatch sw = Stopwatch.StartNew();
			int step = 0;

			var dups = models.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

			await SetApiExecutionProcessingStartTime(execution.ExecutionID);

			addMeasurement(metrics, "Checks for duplicates in load", sw.ElapsedMilliseconds, ++step);
			sw.Restart();

			if (dups.Any())
			{
				string message = $"Duplicate Execution Item Identifiers: {string.Join(", ", dups.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
				response.StatusCode = 400;
				response.IsSuccess = false;
				response.Message = message.Substring(0, Math.Min(2000, message.Length));
			}
			else
			{
				try
				{
					addMeasurement(metrics, "Getting execution current location", sw.ElapsedMilliseconds, ++step);
					currentLocation = await GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionAssetDataProfile");

					sw.Restart();

					DataTable table = new DataTable();
					table.Columns.Add("ExecutionID", typeof(Guid));
					table.Columns.Add("ItemNumber", typeof(int));
					table.Columns.Add("ExecutionItemUid", typeof(Guid));
					table.Columns.Add("AssetUid", typeof(Guid));
					table.Columns.Add("ProfileSeries", typeof(string));
					table.Columns.Add("StartDate", typeof(DateTime));
					table.Columns.Add("EndDate", typeof(DateTime));
					table.Columns.Add("Cascade", typeof(bool));
					table.Columns.Add("Message", typeof(string));
					table.Columns.Add("Success", typeof(bool));

					foreach (AssetDataProfileDeleteModel item in models)
					{
						DataRow row = table.NewRow();
						List<string> errorMessages = new List<string>();

						row["ExecutionID"] = execution.ExecutionID;
						row["ItemNumber"] = itemNumber;
						if (item.ExecutionItemUid.HasValue)
						{
							row["ExecutionItemUid"] = item.ExecutionItemUid;
						}
						else
						{
							row["ExecutionItemUid"] = DBNull.Value;
						}
						if (item.AssetUid.HasValue)
						{
							row["AssetUid"] = item.AssetUid;
						}
						else
						{
							row["AssetUid"] = Guid.Empty;
						}

						if (!string.IsNullOrWhiteSpace(item.ProfileSeries))
						{
							row["ProfileSeries"] = item.ProfileSeries;
						}
						else
						{
							row["ProfileSeries"] = DBNull.Value;
						}
						row["StartDate"] = item.StartDate;

						if (item.StartDate == DateTime.MinValue)
						{
							errorMessages.Add("Startdate is a required field");
						}

						row["EndDate"] = item.EndDate;

						if (item.EndDate == DateTime.MinValue)
						{
							errorMessages.Add("EndDate is a required field");
						}

						if ((Guid)row["AssetUid"] == Guid.Empty && string.IsNullOrWhiteSpace(item.ProfileSeries))
						{
							row["Message"] = "AssetUid or ProfileSeries is required field.";
							row["Success"] = 0;
						}

						if (errorMessages.Any())
						{
							row["Message"] = string.Join(";", errorMessages);
							row["Success"] = 0;
						}

						row["Cascade"] = item.Cascade;

						table.Rows.Add(row);

						itemNumber++;
					}

					#region Bulk Copy

					using (var connection = (SqlConnection)ConnectionProvider.Connect())
					{
						connection.Open();
						if (table.Rows.Count > 0)
						{
							SqlBulkCopy bulkCopy = connection.CreateBulkCopy("[api].[ExecutionDeleteAssetDataProfile]", table.Rows.Count, SqlBulkBatchTimeout);
							bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
							bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
							bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
							bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
							bulkCopy.ColumnMappings.Add("ProfileSeries", "ProfileSeries");
							bulkCopy.ColumnMappings.Add("StartDate", "StartDate");
							bulkCopy.ColumnMappings.Add("EndDate", "EndDate");
							bulkCopy.ColumnMappings.Add("Cascade", "Cascade");
							bulkCopy.ColumnMappings.Add("Message", "Message");
							bulkCopy.ColumnMappings.Add("Success", "Success");
							bulkCopy.WriteToServer(table);
						}

						addMeasurement(metrics, "BulkCopy to execution table", sw.ElapsedMilliseconds, ++step);
						sw.Restart();

						#endregion

						connection.Execute($@"
							update	api.ExecutionDeleteAssetDataProfile
							set		Success = 0,
									[Message] = coalesce([Message] + '; ', '') + 'You must provide a valid Uid.'
							where	ExecutionID = @ExecutionID and ([AssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER))
							and coalesce(ProfileSeries,'') = '';


							update	DEDP
							set		Success = 0,
									[Message] = coalesce([Message] + '; ', '') + 'Asset not found based on Uid provided'
							from	api.ExecutionDeleteAssetDataProfile DEDP
							left Join Asset A on DEDP.AssetUid = A.Uid
							where	ExecutionID = @ExecutionID and A.Uid is null
							and ([AssetUid] != CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER));

							drop table if exists #tempProfileSeries;

							select distinct adp.ProfileSeries 
							into #tempProfileSeries
							from api.ExecutionDeleteAssetDataProfile DEDP
							inner join dbo.AssetDataProfile adp on adp.ProfileSeries = DEDP.ProfileSeries
							where DEDP.ExecutionID = @ExecutionID and DEDP.ProfileSeries is not null
							and ADP.ProfileSetDate between DEDP.startDate and DEDP.endDate;

							create clustered index cx_tempProfileSeries on #tempProfileSeries(ProfileSeries);

							update	DEDP
							set		Success = 0,
									[Message] = coalesce([Message] + '; ', '') + 'Asset Profile not found based on ProfileSeries, StartDate and EndDate provided'
							from   api.ExecutionDeleteAssetDataProfile DEDP
							left Join #tempProfileSeries A on A.ProfileSeries = DEDP.ProfileSeries
							where	ExecutionID = @ExecutionID 
							and ([AssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER))
							and A.ProfileSeries is null and (DEDP.ProfileSeries is not null);


							drop table if exists #tempProfileSeriesAssetUid;

							select distinct DEDP.ItemNumber 
							into #tempProfileSeriesAssetUid
							from api.ExecutionDeleteAssetDataProfile DEDP
							inner join dbo.AssetDataProfile adp on adp.ProfileSeries = DEDP.ProfileSeries
							inner join Asset A on A.ID = adp.AssetID and A.uid = DEDP.AssetUid
							where DEDP.ExecutionID = @ExecutionID and DEDP.ProfileSeries is not null
							and (DEDP.[AssetUid] != CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER))
							and ADP.ProfileSetDate between DEDP.startDate and DEDP.endDate
							option (recompile);

							create clustered index cx_tempProfileSeriesAssetUid on #tempProfileSeriesAssetUid(ItemNumber);

							update	DEDP
							set		Success = 0,
									[Message] = coalesce([Message] + '; ', '') + 'Asset Profile not found based on AssetUid,ProfileSeries, StartDate and EndDate provided'
							from   api.ExecutionDeleteAssetDataProfile DEDP
							left Join #tempProfileSeriesAssetUid A on A.ItemNumber = DEDP.ItemNumber
							where	ExecutionID = @ExecutionID 
							and (DEDP.[AssetUid] != CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER))  and (DEDP.ProfileSeries is not null)
							and A.ItemNumber is null;

							update	api.ExecutionDeleteAssetDataProfile
							set		Success = 0,
									[Message] = coalesce([Message] + '; ', '') + 'StartDate must be before EndDate.'
							where	ExecutionID = @ExecutionID and startdate > enddate;

							declare @IsAdministrator bit = 0						
							select	@IsAdministrator = IsAdministrator
							from	reporting.Global_Resource
							where	ResourceID = @ResourceID

							IF(@IsAdministrator = 0)
							BEGIN
								update	EDP
								set		Success = 0,
										[Message] = coalesce([Message] + '; ', '') + '{Error.DataProfilingNoPermission}'
								from	api.ExecutionDeleteAssetDataProfile EDP
								where   EDP.ExecutionID = @ExecutionID 
										and EDP.[AssetUid] != CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER)
										and not exists (
													select 1
													from	Asset A
															outer apply dbo.UserAssetPermissions(@ResourceID, A.AssetTypeID) P
															where	
															A.Uid = EDP.AssetUid 
															and
															(															
																(
																	P.AssetID = A.ID
																	or 
																	P.AssetTypeID is null
																)
																OR
																(																	
																	P.AssetID=0 
																	and 
																	P.AssetTypeID=A.AssetTypeID
																)
															)
															and 
															P.PermissionsBitMask is not null and P.PermissionsBitMask & @p = @p	
													);

								if exists(select 1 from api.ExecutionDeleteAssetDataProfile DEDP
										  where ExecutionID = @ExecutionID and DEDP.ProfileSeries is not null
										  and DEDP.Success is null)
								begin
									drop table if exists #TempcheckPermission;

									select distinct DEDP.ItemNumber,DEDP.ProfileSeries,ADP.AssetID,Null Success
									into #TempcheckPermission
									from  api.ExecutionDeleteAssetDataProfile DEDP
									inner join dbo.AssetDataProfile ADP on ADP.ProfileSeries = DEDP.ProfileSeries
									where ExecutionID = @ExecutionID and DEDP.ProfileSeries is not null
									and DEDP.[AssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER) 
									and DEDP.Success is null
									and ADP.ProfileSetDate between DEDP.startDate and DEDP.endDate;

									create clustered index cx_TempcheckPermission on #TempcheckPermission(ItemNumber);

									update	TempEDP
									set		Success = 0
									from	#TempcheckPermission TempEDP
									where   not exists (
														select 1
														from	Asset A
																outer apply dbo.UserAssetPermissions(@ResourceID, A.AssetTypeID) P
																where	
																A.ID = TempEDP.AssetId 
																and
																(															
																	(
																		P.AssetID = A.ID
																		or 
																		P.AssetTypeID is null
																	)
																	OR
																	(																	
																		P.AssetID=0 
																		and 
																		P.AssetTypeID=A.AssetTypeID
																	)
																)
																and 
																P.PermissionsBitMask is not null and P.PermissionsBitMask & @p = @p	
														);

									drop table if exists #tempnotpremission;
									select distinct ItemNumber,ProfileSeries
									into #tempnotpremission
									from #TempcheckPermission t
									where t.Success = 0;

									create clustered index cdx_tempnotpremission on #tempnotpremission(ItemNumber,ProfileSeries)

									update	EDP
									set		Success = 0,
											[Message] = coalesce([Message] + '; ', '') + 'User does not have permission to delete profiling data for this ProfileSeries Assets'
									from	api.ExecutionDeleteAssetDataProfile EDP
									inner join #tempnotpremission t on t.ItemNumber  = EDP.ItemNumber and t.ProfileSeries  = EDP.ProfileSeries 
									where  EDP.ExecutionID = @ExecutionID and EDP.ProfileSeries is not null
									and EDP.[AssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER) 

									drop table if exists #tempProfileSeries;
									drop table if exists #TempcheckPermission;
									drop table if exists #tempnotpremission;

								END
							END
	",
										new { execution.ExecutionID, execution.ResourceID, p = Permission.EditAsset }, commandTimeout: timeout);

						addMeasurement(metrics, "LogDeleteAssetDataProfileErrors", sw.ElapsedMilliseconds, ++step);
						sw.Restart();

						generalChecksCompleted = true;
					}
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					await UpdateExecutionWithErrorFromExceptionCount(execution, generalEx, 0, models.Count());
					response.IsSuccess = false;
					response.Message = ReadExceptionMessage(generalEx);
					response.Data = generalEx;
				}
				}
				if (generalChecksCompleted)
				{
					int loopSize = 250;
					int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
					int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
					int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;
					string querySuffix = $"E.Success is null and E.ExecutionID = @ExecutionID and E.ItemNumber between @beginItemNumber and @endItemNumber";

					string sql = $@"
								drop table if exists #child
								create table #child (
									itemnumber int,
									assetID bigint,
									profileSeries nvarchar(100),
									startDate datetime,
									endDate datetime
								)

								drop table if exists #parent
								create table #parent (
									itemnumber int,
									assetID bigint,
									profileSeries nvarchar(100),
									startDate datetime,
									endDate datetime
								)

								drop table if exists #deleteItemCount
								create table #deleteItemCount (
									itemnumber int,
									DeletedCount bigint
								)

								create clustered index cx_deleteItemCount on #deleteItemCount(itemnumber) 

								drop table if exists #deleteAssetDataProfile
								create table #deleteAssetDataProfile (
									itemnumber int,
									assetID bigint,
									profileSeries nvarchar(100),
									startDate datetime,
									endDate datetime
								)

								create clustered index cx_deleteAssetDataProfile on #deleteAssetDataProfile(assetID)


								drop table if exists #tempIntersectTypeIDS
								create table #tempIntersectTypeIDS (
									IntersecttypeID int
								)

								create clustered index cx_tempIntersectTypeIDS on #tempIntersectTypeIDS(IntersecttypeID);

								insert into #tempIntersectTypeIDS
								select IT.ID IntersecttypeID
								from intersecttype IT
								inner join [Predicate] P on IT.PredicateID = P.ID and P.[Type] in (3,4)
								option (recompile);

								drop table if exists #TempExecDeleAssetDataProfile
								create table #TempExecDeleAssetDataProfile (
									Itemnumber int,
									AssetUid uniqueidentifier,
									ProfileSeries Varchar(100),
									startDate datetime,
									endDate datetime,
									[Cascade] bit
								)

								insert into #TempExecDeleAssetDataProfile
								(Itemnumber,AssetUid,ProfileSeries,startDate,endDate,[Cascade])
								select E.ItemNumber,
								 E.AssetUid,
								 E.ProfileSeries,
								 E.StartDate,
								 E.EndDate,
								 E.[Cascade]
								 from API.ExecutionDeleteAssetDataProfile E
								Where {querySuffix};


								insert into #TempExecDeleAssetDataProfile
								(Itemnumber,AssetUid,ProfileSeries,startDate,endDate,[Cascade])
								select distinct E.ItemNumber,
								 A.Uid AssetUid,
								 E.ProfileSeries,
								 E.StartDate,
								 E.EndDate,
								 E.[Cascade]
								 from #TempExecDeleAssetDataProfile E
								 inner join dbo.AssetDataProfile ADP on ADP.ProfileSeries = E.ProfileSeries 
											and ADP.ProfileSetDate between E.startDate and E.endDate
								 inner join Asset a on A.id = ADP.AssetID
								 where E.ProfileSeries is not null 
								 and E.[AssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER)
								option (recompile);

								delete E
								from #TempExecDeleAssetDataProfile E
								where E.ProfileSeries is not null 
								and E.[AssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER);

								insert into #parent
								select 
									ItemNumber,
									ID,
									ProfileSeries,
									startdate,
									enddate
								from #TempExecDeleAssetDataProfile E 
									inner join	Asset A on A.uid = E.AssetUid 
								Where E.[Cascade] = 1;

								insert into #deleteAssetDataProfile
								select * from #parent

								WHILE ((Select Count(*) from #parent) > 0)
								BEGIN
									insert into #child
									select 
										P.ItemNumber,
										I.ObjectAssetID as AssetID,
										P.ProfileSeries,
										P.startDate,
										P.endDate
									from 
										#parent P 
										inner join [intersect] I on I.SubjectAssetID = P.AssetID 
										inner join #tempIntersectTypeIDS IT on IT.IntersectTypeID = I.IntersectTypeID
										option (recompile);

									delete from #parent 
	
									insert into #parent
									select * from #child

									insert into #deleteAssetDataProfile
									select c.* 
									from #child c 
									left join #deleteAssetDataProfile a 
										 on c.assetID=a.assetID and a.startdate =c.startdate and a.enddate=c.enddate
									where a.assetID is null;

									delete from #child;
								END

								insert into #deleteAssetDataProfile
								select 
									E.ItemNumber,
									A.id,
									E.ProfileSeries,
									E.startdate,
									E.enddate
								from #TempExecDeleAssetDataProfile E
								inner join Asset A on A.uid = E.AssetUid 
								where E.[Cascade] = 0
								option (recompile);

								drop table if exists #deletedResultsConsolidate
								create table #deletedResultsConsolidate (
									itemnumber int,
									id bigint
								)

								create clustered index cx_deletedResultsConsolidate on #deletedResultsConsolidate(id);

								drop table if exists #deletedResults
								create table #deletedResults (
									itemnumber int,
									id bigint
								)

								if exists(select 1 from #deleteAssetDataProfile where ProfileSeries is not null)
								begin
									merge AssetDataProfile as ADP
									using (select * from #deleteAssetDataProfile where ProfileSeries is not null) DADP
									on DADP.assetID = ADP.AssetID and ADP.ProfileSetDate between DADP.startDate and DADP.endDate
									and ADP.ProfileSeries =  DADP.ProfileSeries and ADP.ProfileSeries is not null
									when matched then
									DELETE OUTPUT DADP.itemNumber, DELETED.ID into #deletedResults
									option(recompile);

									insert into #deletedResultsConsolidate
									select * from #deletedResults;

									delete from #deletedResults;
								end

								if exists(select 1 from #deleteAssetDataProfile where ProfileSeries is null)
								begin
									merge AssetDataProfile as ADP
									using (select * from #deleteAssetDataProfile where ProfileSeries is null) DADP
									on DADP.assetID = ADP.AssetID and ADP.ProfileSetDate between DADP.startDate and DADP.endDate
									when matched then
									DELETE OUTPUT DADP.itemNumber, DELETED.ID into #deletedResults
									option(recompile);

									insert into #deletedResultsConsolidate
									select * from #deletedResults;
								end

								if exists(select 1 from #deletedResultsConsolidate)
								begin
									Delete t 
									from AssetDataProfileSample t
									where exists (select 1 from #deletedResultsConsolidate dr 
												  where dr.ID = t.AssetDataProfileID 
												  and dr.ItemNumber between @beginItemNumber and @endItemNumber );

									Delete t 
									from AssetDataProfileSampleJson t
									where exists (select 1 from #deletedResultsConsolidate dr 
												  where dr.ID = t.AssetDataProfileID 
												  and dr.ItemNumber between @beginItemNumber and @endItemNumber );

									insert into #deleteItemCount
									select DR.itemNumber,count(distinct ID) DeletedCount
									from #deletedResultsConsolidate DR
									group by DR.itemNumber;
								end


								Update E
								set E.DeletedCount = DR.DeletedCount
								from api.ExecutionDeleteAssetDataProfile E 
								inner join #deleteItemCount DR on DR.itemnumber = E.itemNumber
								where {querySuffix}

								drop table if exists #child;
								drop table if exists #parent;
								drop table if exists #deleteAssetDataProfile;
								drop table if exists #tempIntersectTypeIDS;
								drop table if exists #deleteItemCount;
								drop table if exists #deletedResultsConsolidate;
								drop table if exists #TempExecDeleAssetDataProfile;";

					for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
					{
						bool runCompleted = false;
						int retryCount = 0;

					while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
					{
						using (var connection = (SqlConnection)ConnectionProvider.Connect())
						{
							connection.Open();
							using (SqlTransaction trans = connection.BeginTransaction())
							{
								#region Load valid items into table

								try
								{
									await connection.ExecuteAsync(sql, new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID = CurrentUserId }, transaction: trans, commandTimeout: timeout);

									#endregion

									// Update success flag.
									await connection.ExecuteAsync(
										$@"update E 
													set Success = 1 
											   From api.ExecutionDeleteAssetDataProfile E
											   where {querySuffix};",
										new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

									trans.Commit();
									runCompleted = true;
								}
								catch (Exception ex)
								{
									try
									{
										if (trans != null)
										{
											trans.Rollback();
										}
									}
									catch
									{
										// Continue through loops, do not kill the entire process.
									}

									retryCount++;

									if (retryCount > API_V2_RETRY_LIMIT)
									{
										sw.Restart();
										response.IsSuccess = false;
										response.Message = ReadExceptionMessage(ex);
										response.Data = ex;
										LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionDeleteAssetDataProfile", ReadExceptionMessage(ex), timeout);
									}
								}
							}
						}
					}
					beginItemNumber += loopSize;
					endItemNumber += loopSize;
					sw.Restart();
				}
			}
			return response;
		}

		public async Task<RepositoryResponse<List<DataProfileDeleteResponse>>> RemoveDataProfileAsync(Guid assetUid, ApiExecution execution, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			RepositoryResponse<List<DataProfileDeleteResponse>> response = new RepositoryResponse<List<DataProfileDeleteResponse>> (null, 200, true);

			DateTime? startDate = null;
			DateTime? endDate = null;
			bool cascade = false;

			string parametername = "_startdate";
			if (queryParams.IsQueryParameterPresent(parametername))
			{
				string startdatestring = queryParams.ReadQueryParameterValue(parametername);
				if (!DateTime.TryParse(startdatestring, out DateTime outStartDate))
				{
					response.StatusCode = 400;
					response.IsSuccess = false;
					response.Message = Error.InvalidStartDate;
				}
				else
				{
					startDate = outStartDate;
				}
			}

			 parametername = "_enddate";
			if (queryParams.IsQueryParameterPresent(parametername))
			{
				string enddatestring = queryParams.ReadQueryParameterValue(parametername);
				if (!DateTime.TryParse(enddatestring, out DateTime outEndDate))
				{
					response.StatusCode = 400;
					response.IsSuccess = false;
					response.Message = Error.InvalidEndDate;
				}
				else
				{
					endDate = outEndDate;
				}
			}


			parametername = "_cascade";
			if (queryParams.IsQueryParameterPresent(parametername))
			{
				string cascadestring = queryParams.ReadQueryParameterValue(parametername);
				if (!bool.TryParse(cascadestring, out  cascade))
				{
					response.StatusCode = 400;
					response.IsSuccess = false;
					response.Message = Error.InvalidCascade;
					return response;
				}
			}

			if (startDate.HasValue && endDate.HasValue && startDate > endDate)
			{
				response.StatusCode = 400;
				response.IsSuccess = false;
				response.Message = Error.StartEndDateValidation;
				return response;
			}

			if (!startDate.HasValue && !endDate.HasValue)
			{
				using (var connection = ConnectionProvider.Connect(true))
				{
					string sqlqry = $@" declare @assetid bigint;
										select @assetid = id from asset where uid = @assetUid;
									   Select * from AssetDataProfile where AssetId = @assetid order by ProfileSetDate desc";
					AssetDataProfile dataprofile = await connection.QueryFirstOrDefaultAsync<AssetDataProfile>(sqlqry, new { assetUid }, commandTimeout: CommandTimeout);
					if (dataprofile != null)
					{
						startDate = endDate = dataprofile.ProfileSetDate;
					}
					else
					{
						response.StatusCode = 200;
						response.IsSuccess = true;
						return response;
					}
				}
			}

			startDate = startDate ?? new DateTime(1800, 1, 1);//Can't use MinValue as that is 01/01/0001 but SQL server min is 01/01/1759
			endDate = endDate ?? DateTime.MaxValue;


			response =  await RemoveDataProfileAsync(assetUid, startDate.Value, endDate.Value, execution, cascade);

			return response;
		}

		public async Task<RepositoryResponse<bool>> ValidateDataProfileUpsertRequest(List<DataProfileUpsertModel> models, bool IsInsert)
		{
			RepositoryResponse<bool> response = new RepositoryResponse<bool>(false, 200, true);

			if (models == null || models.Count == 0)
			{
				response.IsSuccess = false;
				response.StatusCode = 400;
				response.Message = Error.JSONValidMessage;
				return response;
			}

			//Key Field Validation
			if (models.Any(dp => dp.profileSetDate == null || dp.assetUid == null))
			{
				response.IsSuccess = false;
				response.StatusCode = 400;
				response.Message = "Error while processing request.";
				return response;
			}

			var dupRecords = models.GroupBy(i => new { i.assetUid, i.profileSetDate }).Where(i => i.Count() > 1).Select(i => new { keyFields = i.Key, Count = i.Count() }).ToList();

			if (dupRecords.Any())
			{
				var ErrorMessage = string.Format(Error.DuplicateRecordBatchProfile, string.Join(", ", dupRecords.Select(i => $"(AssetUid: {i.keyFields.assetUid}, ProfileSetDate: {i.keyFields.profileSetDate.Date: yyyy-MM-dd})")));

				response.IsSuccess = false;
				response.StatusCode = 400;
				response.Message = ErrorMessage;
				return response;
			}

			List<ValidationResult> validationResults = new List<ValidationResult>();
			foreach (var model in models)
			{
				validationResults.Clear();
				var asset = await ReadAssetwithAssetTypeAsync(model.assetUid);

				if (asset == null)
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = string.Format(Error.InvalidAssetUid, model.assetUid.ToString());
					return response;
				}

				if (asset.assetTypeClass != AssetTypeClass.BusinessAsset && asset.assetTypeClass != AssetTypeClass.TechnicalAsset)
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = string.Format(Error.ProfilingNotSupportAssetClass, asset.assetTypeClass.ToString());
					return response;
				}

				bool success = Enum.IsDefined(typeof(ProfileType), model.ProfileType);
				if (!success)
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = string.Format(Error.ValidProfiletype, (int)model.ProfileType);
					return response;
				}

				bool recordExists = false;

				using (var connection = ConnectionProvider.Connect(true))
				{
					var dtoffset = 	(DateTimeOffset)model.profileSetDate;
					recordExists = await connection.QueryFirstOrDefaultAsync<bool>("select cast(1 as bit) from AssetDataProfile where AssetId = @assetID and ProfileSetDate = @ProfileSetDate", new { assetID = asset.ID, ProfileSetDate = dtoffset });
				}

				//check insert
				if (recordExists && IsInsert)
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = string.Format(Error.ProfileRecordAlreadyExists, model.assetUid.ToString(), model.profileSetDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
					return response;
				}


				//check update
				if (!recordExists && !IsInsert)
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = string.Format(Error.AssetUidProfileSetDateRecordNotfound, model.assetUid.ToString(), model.profileSetDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
					return response;
				}

				if (model.topK != null && model.topK.Any(x => x.Trim() == string.Empty))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = Error.ElementTopKNotEmpty;
					return response;
				}

				if (model.topK != null &&  model.bottomK != null && model.bottomK.Any(x => x.Trim() == string.Empty))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = Error.ElementBottomKNotEmpty;
					return response;
				}

				bool isValid =  Validator.TryValidateObject(model, new ValidationContext(model, serviceProvider: null, items: null), validationResults, true);

				if (!isValid)
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = validationResults.First().ErrorMessage;
					return response;
				}


				if (model.ProfileSource != null && model.ProfileSource?.Length > 100)
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = Error.InvalidProfileSourceLength;
					return response;
				}

				if (model.ProfileSeries != null && model.ProfileSeries?.Length > 100)
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = Error.InvalidProfileSeriesLength;
					return response;
				}
			}

			return response;
		}


		public async Task<RepositoryResponse<List<DataProfileUpsertResponse>>> UpsertDataProfilesAsync(List<DataProfileUpsertModel> request, ApiExecution execution, bool isInsert, int timeout = 3600)
		{
			RepositoryResponse<List<DataProfileUpsertResponse>> response = new RepositoryResponse<List<DataProfileUpsertResponse>>(null,200, true, null,null);

			bool generalChecksCompleted = false;
			int itemNumber = 1;
			CurrentExecutionLocationModel currentLocation = null;
			Dictionary<string, double> metrics = new Dictionary<string, double>();
			Stopwatch sw = Stopwatch.StartNew();
			int step = 0;

			if (!CurrentUserIsAdmin)
			{
				bool isPermissions = await HasAssetPermissionByUid(request.Select(s => s.assetUid).Distinct().ToList(), Permission.EditAsset, true);

				if (!isPermissions)
				{
					response.StatusCode = 403;
					response.IsSuccess = false;
					response.Message = Error.Non_Auth_Mess;
					return response;
				}
			}

			var validationResult = await ValidateDataProfileUpsertRequest(request, isInsert);

			if (!validationResult.IsSuccess)
			{
				response.StatusCode = validationResult.StatusCode;
				response.IsSuccess = validationResult.IsSuccess;
				response.Message = validationResult.Message;
				return response;
			}

			if (request.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
			{
				response.StatusCode =400;
				response.IsSuccess = false;
				response.Message = string.Format(Error.DataProfileRecordsLimit, MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString(), MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString());
				return response;
			}

			await UpsertApiExecution(execution);

			var dups = request.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

			var dupRecords = request.GroupBy(i => new { i.assetUid, i.profileSetDate }).Where(i => i.Count() > 1).Select(i => new { keyFields = i.Key, Count = i.Count() }).ToList();

			await SetApiExecutionProcessingStartTime(execution.ExecutionID);

			addMeasurement(metrics, "Checks for duplicates in load", sw.ElapsedMilliseconds, ++step);

			sw.Restart();

			if (dups.Any() || dupRecords.Any())
			{
				if (dups.Any())
				{
					string message = $"Duplicate Execution Item Identifiers: {string.Join(", ", dups.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
					response.StatusCode = 400;
					response.IsSuccess = false;
					response.Message = message.Substring(0, Math.Min(2000, message.Length));
				}
				else
				{
					string message = $"Duplicate Records: {string.Join(", ", dupRecords.Select(i => $"AssetUid: {i.keyFields.assetUid}, ProfileSetDate: {i.keyFields.profileSetDate}"))}. AssetUid and ProfileSetDate pairs are used as record identifiers and must be unique within a batch.";
					response.StatusCode = 400;
					response.IsSuccess = false;
					response.Message = message.Substring(0, Math.Min(2000, message.Length));
				}
			}
			else
			{
				try
				{
					addMeasurement(metrics, "Getting execution current location", sw.ElapsedMilliseconds, ++step);
					currentLocation = await GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionAssetDataProfile");
					sw.Restart();

					#region Build data tables.

					DataTable DataProfileTable = new DataTable();
					DataTable DataProfileSampleTable = new DataTable();

					DataProfileTable.Columns.Add("ExecutionID", typeof(Guid));
					DataProfileTable.Columns.Add("ItemNumber", typeof(int));
					DataProfileTable.Columns.Add("ExecutionItemUid", typeof(Guid));
					DataProfileTable.Columns.Add("AssetUid", typeof(Guid));
					DataProfileTable.Columns.Add("ProfileSetDate", typeof(DateTime));
					DataProfileTable.Columns.Add("ProfileIdentifier", typeof(string));

					DataProfileTable.Columns.Add("UniqueCount", typeof(long));
					DataProfileTable.Columns.Add("SampleCount", typeof(long));
					DataProfileTable.Columns.Add("NullCount", typeof(long));
					DataProfileTable.Columns.Add("BlankCount", typeof(long));
					DataProfileTable.Columns.Add("MeanValue", typeof(double));
					DataProfileTable.Columns.Add("MinimumValue", typeof(string));

					DataProfileTable.Columns.Add("MaximumValue", typeof(string));
					DataProfileTable.Columns.Add("MinimumLength", typeof(int));
					DataProfileTable.Columns.Add("MaximumLength", typeof(int));
					DataProfileTable.Columns.Add("StandardDeviation", typeof(double));
					DataProfileTable.Columns.Add("Type", typeof(string));

					DataProfileTable.Columns.Add("Multiline", typeof(bool));
					DataProfileTable.Columns.Add("RegExp", typeof(string));
					DataProfileTable.Columns.Add("Confidence", typeof(decimal));
					DataProfileTable.Columns.Add("TypeQualifier", typeof(string));
					DataProfileTable.Columns.Add("LogicalType", typeof(bool));

					DataProfileTable.Columns.Add("LeadingWhiteSpace", typeof(bool));
					DataProfileTable.Columns.Add("LeadingZeroCount", typeof(int));
					DataProfileTable.Columns.Add("TrailingWhiteSpace", typeof(bool));

					DataProfileTable.Columns.Add("MatchCount", typeof(long));
					DataProfileTable.Columns.Add("OutlierCardinality", typeof(int));
					DataProfileTable.Columns.Add("DataSignature", typeof(string));

					DataProfileTable.Columns.Add("StructureSignature", typeof(string));
					DataProfileTable.Columns.Add("Cardinality", typeof(int));
					DataProfileTable.Columns.Add("ShapeCardinality", typeof(int));

					DataProfileTable.Columns.Add("TotalCount", typeof(long));
					DataProfileTable.Columns.Add("OutlierCount", typeof(long));
					DataProfileTable.Columns.Add("KeyConfidence", typeof(decimal));
					DataProfileTable.Columns.Add("DetectionLocale", typeof(string));
					DataProfileTable.Columns.Add("FtaVersion", typeof(string));
					DataProfileTable.Columns.Add("DecimalSeparator", typeof(string));

					DataProfileTable.Columns.Add("PopularityCount", typeof(long));
					DataProfileTable.Columns.Add("IsAuthorizedForPopularity", typeof(bool));
					DataProfileTable.Columns.Add("SourceLastModified", typeof(DateTime));
					DataProfileTable.Columns.Add("FilterCount", typeof(long));
					DataProfileTable.Columns.Add("Freshness", typeof(int));

					DataProfileTable.Columns.Add("ProfileSource", typeof(string));
					DataProfileTable.Columns.Add("ProfileSeries", typeof(string));
					DataProfileTable.Columns.Add("ProfileType", typeof(int));

					DataProfileSampleTable.Columns.Add("ExecutionID", typeof(Guid));
					DataProfileSampleTable.Columns.Add("ItemNumber", typeof(int));
					DataProfileSampleTable.Columns.Add("ExecutionItemUid", typeof(Guid));
					DataProfileSampleTable.Columns.Add("SampleType", typeof(string));
					DataProfileSampleTable.Columns.Add("Key", typeof(string));
					DataProfileSampleTable.Columns.Add("Value", typeof(string));
					DataProfileSampleTable.Columns.Add("JsonValue", typeof(string));

					#endregion

					#region Populate Data Tables

					foreach (DataProfileUpsertModel item in request)
					{
						DataRow row = DataProfileTable.NewRow();

						row["ExecutionID"] = execution.ExecutionID;
						row["ItemNumber"] = itemNumber;
						if (item.ExecutionItemUid.HasValue)
						{
							row["ExecutionItemUid"] = item.ExecutionItemUid;
						}
						else
						{
							row["ExecutionItemUid"] = DBNull.Value;
						}
						row["AssetUid"] = item.assetUid;
						row["ProfileSetDate"] = item.profileSetDate;
						row["ProfileIdentifier"] = item.profileIdentifier ?? (object)DBNull.Value;

						row["UniqueCount"] = item.uniqueCount ?? (object)DBNull.Value;
						row["SampleCount"] = item.sampleCount ?? (object)DBNull.Value;
						row["NullCount"] = item.nullCount ?? (object)DBNull.Value;
						row["BlankCount"] = item.blankCount ?? (object)DBNull.Value;
						row["MeanValue"] = item.meanValue ?? (object)DBNull.Value;
						row["MinimumValue"] = item.minValue ?? (object)DBNull.Value;

						row["MaximumValue"] = item.maxValue ?? (object)DBNull.Value;
						row["MinimumLength"] = item.minLength ?? (object)DBNull.Value;
						row["MaximumLength"] = item.maxLength ?? (object)DBNull.Value;
						row["StandardDeviation"] = item.standardDeviation ?? (object)DBNull.Value;
						row["Type"] = item.type ?? (object)DBNull.Value;

						row["Multiline"] = item.multiline ?? (object)DBNull.Value;
						row["RegExp"] = item.regExp ?? (object)DBNull.Value;
						row["Confidence"] = item.confidence ?? (object)DBNull.Value;
						row["TypeQualifier"] = item.typeQualifier ?? (object)DBNull.Value;
						row["LogicalType"] = item.logicalType ?? (object)DBNull.Value;

						row["LeadingWhiteSpace"] = item.leadingWhiteSpace ?? (object)DBNull.Value;
						row["LeadingZeroCount"] = item.leadingZeroCount ?? (object)DBNull.Value;
						row["TrailingWhiteSpace"] = item.trailingWhiteSpace ?? (object)DBNull.Value;
						row["MatchCount"] = item.matchCount ?? (object)DBNull.Value;
						row["OutlierCardinality"] = item.outlierCardinality ?? (object)DBNull.Value;

						row["DataSignature"] = item.dataSignature ?? (object)DBNull.Value;
						row["StructureSignature"] = item.structureSignature ?? (object)DBNull.Value;
						row["Cardinality"] = item.cardinality ?? (object)DBNull.Value;
						row["ShapeCardinality"] = item.shapesCardinality ?? (object)DBNull.Value;

						row["TotalCount"] = item.TotalCount ?? (object)DBNull.Value;
						row["OutlierCount"] = item.OutlierCount ?? (object)DBNull.Value;
						row["KeyConfidence"] = item.KeyConfidence ?? (object)DBNull.Value;
						row["DetectionLocale"] = item.DetectionLocale ?? (object)DBNull.Value;
						row["FtaVersion"] = item.FtaVersion ?? (object)DBNull.Value;
						row["DecimalSeparator"] = item.DecimalSeparator ?? (object)DBNull.Value;

						row["PopularityCount"] = item.PopularityCount ?? (object)DBNull.Value;
						row["IsAuthorizedForPopularity"] = item.IsAuthorizedForPopularity ?? (object)DBNull.Value;
						row["SourceLastModified"] = item.SourceLastModified ?? (object)DBNull.Value;
						row["FilterCount"] = item.FilterCount ?? (object)DBNull.Value;
						row["Freshness"] = item.Freshness ?? (object)DBNull.Value;

						row["ProfileSource"] = item.ProfileSource ?? (object)DBNull.Value;
						row["ProfileSeries"] = item.ProfileSeries ?? (object)DBNull.Value;
						row["ProfileType"] = (object)(int)item.ProfileType ?? (int)ProfileType.Full;

						DataProfileTable.Rows.Add(row);
						if (item.outlierDetail != null)
						{
							foreach (DataProfileSampleDetail outlier in item.outlierDetail)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									row["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								else
								{
									row["ExecutionItemUid"] = DBNull.Value;
								}
								sampleRow["SampleType"] = "outlierDetail";
								sampleRow["Key"] = outlier.key ?? (object)DBNull.Value;
								sampleRow["Value"] = outlier.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.shapesDetail != null)
						{
							foreach (DataProfileSampleDetail shape in item?.shapesDetail)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "shapesDetail";
								sampleRow["Key"] = shape.key;
								sampleRow["Value"] = shape.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.cardinalityDetail != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.cardinalityDetail)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "cardinalityDetail";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.characterCasingStatistics != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.characterCasingStatistics)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "characterCasingStatistics";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.characterDataTypeStatistics != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.characterDataTypeStatistics)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "characterDataTypeStatistics";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.characterSpacingStatistics != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.characterSpacingStatistics)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "characterSpacingStatistics";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.scriptDistributionStatistics != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.scriptDistributionStatistics)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "scriptDistributionStatistics";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.specialCharacterStatistics != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.specialCharacterStatistics)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "specialCharacterStatistics";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.percentileStatistics != null)
						{
							foreach (DataProfileSampleDetail cardinality in item?.percentileStatistics)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "percentileStatistics";
								sampleRow["Key"] = cardinality.key;
								sampleRow["Value"] = cardinality.count.ToString();
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.textPatternDetails != null)
						{
							foreach (DataProfileTextPatternDetail stat in item?.textPatternDetails)
							{
								DataRow jsonRow = DataProfileSampleTable.NewRow();
								jsonRow["ExecutionID"] = execution.ExecutionID;
								jsonRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									jsonRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								jsonRow["SampleType"] = "textPatternDetails";
								jsonRow["JsonValue"] = JsonConvert.SerializeObject(stat);
								DataProfileSampleTable.Rows.Add(jsonRow);
							}
						}

						if (item.semanticAnalysisDetails != null)
						{
							foreach (DataProfileSemanticAnalysisDetail stat in item?.semanticAnalysisDetails)
							{
								DataRow jsonRow = DataProfileSampleTable.NewRow();
								jsonRow["ExecutionID"] = execution.ExecutionID;
								jsonRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									jsonRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								jsonRow["SampleType"] = "semanticAnalysisDetails";
								jsonRow["JsonValue"] = JsonConvert.SerializeObject(stat);
								DataProfileSampleTable.Rows.Add(jsonRow);
							}
						}

						if (item.confidenceAnalysisDetails != null)
						{
							foreach (DataProfileConfidenceAnalysisDetails stat in item?.confidenceAnalysisDetails)
							{
								DataRow jsonRow = DataProfileSampleTable.NewRow();
								jsonRow["ExecutionID"] = execution.ExecutionID;
								jsonRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									jsonRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								jsonRow["SampleType"] = "confidenceAnalysisDetails";
								jsonRow["JsonValue"] = JsonConvert.SerializeObject(stat);
								DataProfileSampleTable.Rows.Add(jsonRow);
							}
						}

						if (item.tableStructureInfo != null)
						{
							DataRow jsonRow = DataProfileSampleTable.NewRow();
							jsonRow["ExecutionID"] = execution.ExecutionID;
							jsonRow["ItemNumber"] = itemNumber;
							if (item.ExecutionItemUid.HasValue)
							{
								jsonRow["ExecutionItemUid"] = item.ExecutionItemUid;
							}
							jsonRow["SampleType"] = "tableStructureInfo";
							jsonRow["JsonValue"] = JsonConvert.SerializeObject(item.tableStructureInfo);
							DataProfileSampleTable.Rows.Add(jsonRow);
						}

						if (item.topK != null)
						{
							foreach (string topK in item?.topK)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "topK";
								sampleRow["Key"] = DBNull.Value;
								sampleRow["Value"] = topK;
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						if (item.bottomK != null)
						{
							foreach (string bottomK in item?.bottomK)
							{
								DataRow sampleRow = DataProfileSampleTable.NewRow();
								sampleRow["ExecutionID"] = execution.ExecutionID;
								sampleRow["ItemNumber"] = itemNumber;
								if (item.ExecutionItemUid.HasValue)
								{
									sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
								}
								sampleRow["SampleType"] = "bottomK";
								sampleRow["Key"] = DBNull.Value;
								sampleRow["Value"] = bottomK;
								DataProfileSampleTable.Rows.Add(sampleRow);
							}
						}

						itemNumber++;
					}

					#endregion

					#region Bulk Copy

					using (var connection = (SqlConnection)ConnectionProvider.Connect())
					{
						connection.Open();

						using (SqlTransaction transaction = connection.BeginTransaction())
						{
							try
							{
								#region Bulk Copy Data Profile

								using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
								{
									BatchSize = DataProfileTable.Rows.Count,
									DestinationTableName = "[api].[ExecutionAssetDataProfile]",
									BulkCopyTimeout = SqlBulkBatchTimeout
								})
								{
									bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
									bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
									bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
									bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
									bulkCopy.ColumnMappings.Add("ProfileSetDate", "ProfileSetDate");
									bulkCopy.ColumnMappings.Add("ProfileIdentifier", "ProfileIdentifier");
									bulkCopy.ColumnMappings.Add("UniqueCount", "UniqueCount");
									bulkCopy.ColumnMappings.Add("SampleCount", "SampleCount");
									bulkCopy.ColumnMappings.Add("NullCount", "NullCount");
									bulkCopy.ColumnMappings.Add("BlankCount", "BlankCount");
									bulkCopy.ColumnMappings.Add("MeanValue", "MeanValue");

									bulkCopy.ColumnMappings.Add("MinimumValue", "MinimumValue");
									bulkCopy.ColumnMappings.Add("MaximumValue", "MaximumValue");
									bulkCopy.ColumnMappings.Add("MinimumLength", "MinimumLength");
									bulkCopy.ColumnMappings.Add("MaximumLength", "MaximumLength");
									bulkCopy.ColumnMappings.Add("StandardDeviation", "StandardDeviation");

									bulkCopy.ColumnMappings.Add("Type", "Type");
									bulkCopy.ColumnMappings.Add("Multiline", "Multiline");
									bulkCopy.ColumnMappings.Add("RegExp", "RegExp");
									bulkCopy.ColumnMappings.Add("Confidence", "Confidence");
									bulkCopy.ColumnMappings.Add("TypeQualifier", "TypeQualifier");

									bulkCopy.ColumnMappings.Add("LogicalType", "LogicalType");
									bulkCopy.ColumnMappings.Add("LeadingWhiteSpace", "LeadingWhiteSpace");
									bulkCopy.ColumnMappings.Add("LeadingZeroCount", "LeadingZeroCount");

									bulkCopy.ColumnMappings.Add("TrailingWhiteSpace", "TrailingWhiteSpace");
									bulkCopy.ColumnMappings.Add("MatchCount", "MatchCount");
									bulkCopy.ColumnMappings.Add("OutlierCardinality", "OutlierCardinality");

									bulkCopy.ColumnMappings.Add("DataSignature", "DataSignature");
									bulkCopy.ColumnMappings.Add("StructureSignature", "StructureSignature");
									bulkCopy.ColumnMappings.Add("Cardinality", "Cardinality");
									bulkCopy.ColumnMappings.Add("ShapeCardinality", "ShapeCardinality");

									bulkCopy.ColumnMappings.Add("TotalCount", "TotalCount");
									bulkCopy.ColumnMappings.Add("OutlierCount", "OutlierCount");
									bulkCopy.ColumnMappings.Add("KeyConfidence", "KeyConfidence");
									bulkCopy.ColumnMappings.Add("DetectionLocale", "DetectionLocale");
									bulkCopy.ColumnMappings.Add("FtaVersion", "FtaVersion");
									bulkCopy.ColumnMappings.Add("DecimalSeparator", "DecimalSeparator");

									bulkCopy.ColumnMappings.Add("PopularityCount", "PopularityCount");
									bulkCopy.ColumnMappings.Add("IsAuthorizedForPopularity", "IsAuthorizedForPopularity");
									bulkCopy.ColumnMappings.Add("SourceLastModified", "SourceLastModified");
									bulkCopy.ColumnMappings.Add("FilterCount", "FilterCount");
									bulkCopy.ColumnMappings.Add("Freshness", "Freshness");

									bulkCopy.ColumnMappings.Add("ProfileSource", "ProfileSource");
									bulkCopy.ColumnMappings.Add("ProfileSeries", "ProfileSeries");
									bulkCopy.ColumnMappings.Add("ProfileType", "ProfileType");

									bulkCopy.WriteToServer(DataProfileTable);
								}

								#endregion

								#region Bulk Copy Data Profile Sample

								using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
								{
									BatchSize = DataProfileSampleTable.Rows.Count,
									DestinationTableName = "[api].[ExecutionAssetDataProfileSample]",
									BulkCopyTimeout = SqlBulkBatchTimeout
								})
								{
									bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
									bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
									bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
									bulkCopy.ColumnMappings.Add("SampleType", "SampleType");
									bulkCopy.ColumnMappings.Add("Key", "Key");
									bulkCopy.ColumnMappings.Add("Value", "Value");
									bulkCopy.ColumnMappings.Add("JsonValue", "JsonValue");

									bulkCopy.WriteToServer(DataProfileSampleTable);
								}

								#endregion

								transaction.Commit();

								addMeasurement(metrics, "BulkCopy to api.Execution table", sw.ElapsedMilliseconds, ++step);
							}
							catch (Exception)
							{
								if (transaction != null)
								{
									transaction.Rollback();
								}
								throw;
							}
						}
						#endregion

						await connection.ExecuteAsync($@"
						update	api.ExecutionAssetDataProfile
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'You must provide a valid Uid.'
						where	ExecutionID = @ExecutionID and ([AssetUid] is null or [AssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER));

						update	api.ExecutionAssetDataProfile
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'You must provide a ProfileSetDate.'
						where	ExecutionID = @ExecutionID and [ProfileSetDate] is null;

						update	EDP
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Asset not found based on Uid provided'
						from
							api.ExecutionAssetDataProfile EDP
							left Join
							Asset A on EDP.AssetUid = A.Uid
						where	ExecutionID = @ExecutionID and (A.Uid is null or a.Uid = '00000000-0000-0000-0000-000000000000');

						update	EDP
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Profiling data can only be associated with Business or Technical Asset types'
						from
							api.ExecutionAssetDataProfile EDP
							inner Join Asset A on EDP.AssetUid = A.Uid and A.Uid != '00000000-0000-0000-0000-000000000000'
							inner join AssetType AST on A.AssetTypeId = AST.ID
						where	ExecutionID = @ExecutionID and AST.Class not in (1, 8);

						update	EDP
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Record does not exist with AssetUid '+ convert(nvarchar(36), EDP.AssetUid) +' and profileSetDate '+ convert(varchar, EDP.ProfileSetDate, 121)
						from
							api.ExecutionAssetDataProfile EDP
							inner join 
							Asset A on EDP.AssetUid = A.Uid
							left join 
							AssetDataProfile ADP on A.ID = ADP.AssetId and EDP.ProfileSetDate = ADP.ProfileSetDate
						where	ExecutionID = @ExecutionID and ADP.AssetId is null and @isInsert = 0;
						
						update	EDP
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Record already exists with AssetUid '+ convert(nvarchar(36), EDP.AssetUid) +' and profileSetDate '+ convert(varchar, EDP.ProfileSetDate, 121)
						from
							api.ExecutionAssetDataProfile EDP
							inner join 
							Asset A on EDP.AssetUid = A.Uid
							inner join 
							AssetDataProfile ADP on A.ID = ADP.AssetId and EDP.ProfileSetDate = ADP.ProfileSetDate
						where	ExecutionID = @ExecutionID and @isInsert = 1;						

						Update EDP
						set		Success = 0,
								[Message] = coalesce([Message] + '; ', '') + 'Elements in '+ EDPS.SampleType +' cannot be Empty strings'
						from  
							api.ExecutionAssetDataProfile EDP 
							inner join 
							(
								select 
									distinct ExecutionID, itemnumber, SampleType 
								from 
									api.ExecutionAssetDataProfileSample 
								where ExecutionID = @ExecutionID and LEN(TRIM(value))=0 and LOWER(SampleType) in ('topk', 'bottomk') 
							) EDPS on EDP.ExecutionID=EDPS.ExecutionID and EDP.ItemNumber=EDPS.ItemNumber 
						where 
							EDP.ExecutionID = @ExecutionID                             
						
						declare @IsAdministrator bit = 0						
						select	@IsAdministrator = IsAdministrator
						from	reporting.Global_Resource
						where	ResourceID = @ResourceID
						IF(@IsAdministrator = 0)
						BEGIN
							update	EDP
							set		Success = 0,
									[Message] = coalesce([Message] + '; ', '') + '{Error.DataProfilingNoPermission}'
							from	
									api.ExecutionAssetDataProfile EDP
							where 
									EDP.ExecutionID = @ExecutionID and not exists (
												select 1
												from	Asset A
														outer apply dbo.UserAssetPermissions(@ResourceID, A.AssetTypeID) P
														where	
														A.Uid = EDP.AssetUid 
														and
														(															
															(
																P.AssetID = A.ID
																or 
																P.AssetTypeID is null
															)
															OR
															(																	
																P.AssetID=0 
																and 
																P.AssetTypeID=A.AssetTypeID
															)
														)
														and 
														P.PermissionsBitMask is not null and P.PermissionsBitMask & @p = @p	
												);
						END
",
										new { execution.ExecutionID, isInsert, execution.ResourceID, p = Permission.EditAsset }, commandTimeout: timeout);

						addMeasurement(metrics, "LogAssetDataProfileErrors", sw.ElapsedMilliseconds, ++step);
						sw.Restart();

						generalChecksCompleted = true;
					}
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					await UpdateExecutionWithErrorFromExceptionCount(execution, generalEx,0, request.Count());
					response.IsSuccess = false;
					response.Message = ReadExceptionMessage(generalEx);
					response.Ex = generalEx;
				}

				if (generalChecksCompleted && response.IsSuccess)
				{
					int loopSize = 250;
					int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
					int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
					int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;
					string querySuffix = $"E.Success is null and E.ExecutionID = @ExecutionID and E.ItemNumber between @beginItemNumber and @endItemNumber";
					string insertSQL = $@"
										DROP TABLE IF EXISTS #mergeResultTable
										CREATE TABLE #mergeResultTable (DataProfileId INT, ItemNumber INT) 

										MERGE INTO AssetDataProfile ADP
										USING (
												SELECT
													A.ID as AssetId, E.*
												FROM  
													api.ExecutionAssetDataProfile E
												INNER JOIN
													Asset A ON A.Uid = E.AssetUid
												WHERE {querySuffix}
												) EDP
										ON 1 = 0                                       
										WHEN NOT MATCHED THEN
										INSERT ([AssetID]
													,[ProfileSetDate]
													,[ProfileIdentifier]
                                                    ,[UniqueCount]
													,[SampleCount]
													,[NullCount]
													,[BlankCount]
													,[MeanValue]
													,[MinimumValue]
													,[MaximumValue]
													,[MinimumLength]
													,[MaximumLength]
													,[StandardDeviation]
													,[Type]
													,[Multiline]
													,[RegExp]
													,[Confidence]
													,[TypeQualifier]
													,[LogicalType]
													,[LeadingWhiteSpace]
													,[LeadingZeroCount]
													,[TrailingWhiteSpace]
													,[MatchCount]
													,[OutlierCardinality]
													,[DataSignature]
													,[StructureSignature]
													,[Cardinality]
													,[ShapeCardinality]
													,[TotalCount]
													,[OutlierCount]
													,[KeyConfidence]
													,[DetectionLocale]
													,[FtaVersion]
													,[DecimalSeparator]
													,[PopularityCount]
													,[IsAuthorizedForPopularity]
													,[SourceLastModified]
													,[FilterCount]
													,[Freshness]
													,[ProfileSource]
													,[ProfileSeries]
													,[ProfileType]
													,[CreatedBy]
													,[CreatedOn]
													,[UpdatedBy]
													,[UpdatedOn])
												VALUES
													(EDP.AssetID
													,EDP.ProfileSetDate
													,EDP.ProfileIdentifier
                                                    ,EDP.UniqueCount
													,EDP.SampleCount
													,EDP.NullCount
													,EDP.BlankCount
													,EDP.MeanValue
													,EDP.MinimumValue
													,EDP.MaximumValue
													,EDP.MinimumLength
													,EDP.MaximumLength
													,EDP.StandardDeviation
													,EDP.Type
													,EDP.Multiline
													,EDP.RegExp
													,EDP.Confidence
													,EDP.TypeQualifier
													,EDP.LogicalType
													,EDP.LeadingWhiteSpace
													,EDP.LeadingZeroCount
													,EDP.TrailingWhiteSpace
													,EDP.MatchCount
													,EDP.OutlierCardinality
													,EDP.DataSignature
													,EDP.StructureSignature
													,EDP.Cardinality
													,EDP.ShapeCardinality
													,EDP.TotalCount
													,EDP.OutlierCount
													,EDP.KeyConfidence
													,EDP.DetectionLocale
													,EDP.FtaVersion
													,EDP.DecimalSeparator
													,EDP.PopularityCount
													,EDP.IsAuthorizedForPopularity
													,EDP.SourceLastModified
													,EDP.FilterCount
													,EDP.Freshness
													,EDP.ProfileSource
													,EDP.ProfileSeries
													,EDP.ProfileType
													,@CurrentResourceID
													,getutcdate()
													,@CurrentResourceID
													,getutcdate())
											OUTPUT  inserted.ID INT, EDP.ItemNumber INTO #mergeResultTable;";
					string updateSQL = $@"
										DROP TABLE IF EXISTS #mergeResultTable
										CREATE TABLE #mergeResultTable (DataProfileId INT, ItemNumber INT) 

										MERGE INTO AssetDataProfile ADP
										USING (
												SELECT
													A.ID as AssetId, E.*
												FROM  
													api.ExecutionAssetDataProfile E
												INNER JOIN
													Asset A ON A.Uid = E.AssetUid
												WHERE {querySuffix}
												) EDP
										ON (EDP.AssetId = ADP.AssetID AND EDP.profileSetDate = ADP.profileSetDate)
										WHEN MATCHED THEN
										UPDATE SET
                                            ADP.[ProfileIdentifier] = EDP.[ProfileIdentifier]
                                            ,ADP.[UniqueCount] = EDP.[UniqueCount]
											,ADP.[SampleCount] = EDP.[SampleCount]
											,ADP.[NullCount] = EDP.[NullCount]
											,ADP.[BlankCount] = EDP.[BlankCount]
											,ADP.[MeanValue] = EDP.[MeanValue]
											,ADP.[MinimumValue] = EDP.[MinimumValue]
											,ADP.[MaximumValue] = EDP.[MaximumValue]
											,ADP.[MinimumLength] = EDP.[MinimumLength]
											,ADP.[MaximumLength] = EDP.[MaximumLength]
											,ADP.[StandardDeviation] = EDP.[StandardDeviation]
											,ADP.[Type] = EDP.[Type]
											,ADP.[Multiline] = EDP.[Multiline]
											,ADP.[RegExp] = EDP.[RegExp]
											,ADP.[Confidence] = EDP.[Confidence]
											,ADP.[TypeQualifier] = EDP.[TypeQualifier]
											,ADP.[LogicalType] = EDP.[LogicalType]
											,ADP.[LeadingWhiteSpace] = EDP.[LeadingWhiteSpace]
											,ADP.[LeadingZeroCount] = EDP.[LeadingZeroCount]
											,ADP.[TrailingWhiteSpace] = EDP.[TrailingWhiteSpace]
											,ADP.[MatchCount] = EDP.[MatchCount]
											,ADP.[OutlierCardinality] = EDP.[OutlierCardinality]
											,ADP.[DataSignature] = EDP.[DataSignature]
											,ADP.[StructureSignature] = EDP.[StructureSignature]
											,ADP.[Cardinality] = EDP.[Cardinality]
											,ADP.[ShapeCardinality] = EDP.[ShapeCardinality]
											,ADP.[TotalCount] = EDP.[TotalCount]
											,ADP.[OutlierCount] = EDP.[OutlierCount]
											,ADP.[KeyConfidence] = EDP.[KeyConfidence]
											,ADP.[DetectionLocale] = EDP.[DetectionLocale]
											,ADP.[FtaVersion] = EDP.[FtaVersion]
											,ADP.[DecimalSeparator] = EDP.[DecimalSeparator]
											,ADP.[PopularityCount] = EDP.[PopularityCount]
											,ADP.[IsAuthorizedForPopularity] = EDP.[IsAuthorizedForPopularity]
											,ADP.[SourceLastModified] = EDP.[SourceLastModified]
											,ADP.[FilterCount] = EDP.[FilterCount]
											,ADP.[Freshness] = EDP.[Freshness]
											,ADP.[ProfileSource] = EDP.[ProfileSource]
											,ADP.[ProfileSeries] = EDP.[ProfileSeries]
											,ADP.[ProfileType] = EDP.[ProfileType]
											,ADP.[UpdatedBy] = @CurrentResourceID
											,ADP.[UpdatedOn] = getutcdate()                                       
										OUTPUT  inserted.ID INT, EDP.ItemNumber INTO #mergeResultTable;

											Delete ADPS from AssetDataProfileSample ADPS inner join #mergeResultTable rt on ADPS.AssetDataProfileID = rt.DataProfileID
											Delete ADPSJ from AssetDataProfileSampleJson ADPSJ inner join #mergeResultTable rt on ADPSJ.AssetDataProfileID = rt.DataProfileID";


					string insertSampleSQL = $@"
										insert into AssetDataProfileSample 
													([AssetDataProfileID]
													,[SampleType]
													,[Key]
													,[Value])                                            
										SELECT  
											rt.DataProfileID
											,EDPS.SampleType
											,EDPS.[Key]
											,EDPS.Value
										FROM  
											api.ExecutionAssetDataProfileSample EDPS
										INNER JOIN
											api.ExecutionAssetDataProfile E ON EDPS.ExecutionID=E.ExecutionID AND EDPS.itemnumber = E.itemnumber
										INNER JOIN 
											#mergeResultTable rt ON rt.itemNumber = EDPS.itemNumber
										WHERE 
											EDPS.JsonValue is null AND
											{querySuffix}
											";

					string insertSampleJsonSQL = $@"
										insert into AssetDataProfileSampleJson 
													([AssetDataProfileID]
													,[SampleType]
													,[Value])                                            
										SELECT  
											rt.DataProfileID
											,EDPS.SampleType
											,EDPS.JsonValue
										FROM  
											api.ExecutionAssetDataProfileSample EDPS
										INNER JOIN
											api.ExecutionAssetDataProfile E ON EDPS.ExecutionID=E.ExecutionID AND EDPS.itemnumber = E.itemnumber
										INNER JOIN 
											#mergeResultTable rt ON rt.itemNumber = EDPS.itemNumber
										WHERE 
											EDPS.Value is null AND EDPS.JsonValue is not null AND
											{querySuffix}
											";

					string sql = $@"{insertSQL}
								{insertSampleSQL}
								{insertSampleJsonSQL}";

					if (!isInsert)
					{
						sql = $@"{updateSQL}
								{insertSampleSQL}
								{insertSampleJsonSQL}";
					}

					for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
					{
						bool runCompleted = false;
						int retryCount = 0;

						while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
						{
							using (var connection = (SqlConnection)ConnectionProvider.Connect())
							{
								connection.Open();
								using (SqlTransaction trans = connection.BeginTransaction())
								{
									#region Load valid items into table
									try
									{
										connection.Execute(sql, new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID = CurrentUserId }, transaction: trans, commandTimeout: timeout);

										#endregion

										// Update success flag.
										connection.Execute(
											$@"update E 
												set Success = 1 
										   From api.ExecutionAssetDataProfile E
										   where {querySuffix};",
											new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

										trans.Commit();
										runCompleted = true;
									}
									catch (Exception ex)
									{
										try
										{
											if (trans != null)
											{
												trans.Rollback();
											}
										}
										catch
										{
											// Do not interrupt loop if only this instance fails.
										}

										retryCount++;

										if (retryCount > API_V2_RETRY_LIMIT)
										{
											sw.Restart();
											response.IsSuccess = false;
											response.Message = ReadExceptionMessage(ex);
											response.Ex = ex;
											LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionAssetDataProfile", ReadExceptionMessage(ex), timeout);
											addMeasurement(metrics, $"LogLoopExecutionError >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
											sw.Restart();
										}
									}
								}
							}
						}
						beginItemNumber += loopSize;
						endItemNumber += loopSize;
						sw.Restart();
					}
				}
			}

			await completeApiExecutionAndGetCounts(isInsert ? ApiExecutionAction.PostDataProfile : ApiExecutionAction.PutDataProfile, execution.Id, execution.ExecutionID);

			var results = await GetExecutionDataProfileResultsAsync(execution.ExecutionID);
			response.Data = results;

			return response;
		}

		public async Task<List<DataProfileUpsertResponse>> GetExecutionDataProfileResultsAsync(Guid executionId)
		{
			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var sql = "select [ItemNumber], AssetUid as [uid], [ExecutionItemUid], [Message], [Success] from api.ExecutionAssetDataProfile where ExecutionID = @executionId order by ItemNumber asc";
				return (await connection.QueryAsync<DataProfileUpsertResponse>(sql, new { executionId })).ToList();
			}
		}

		public async Task<List<DataProfileDeleteResponse>> GetExecutionDataProfileDeleteResultsAsync(Guid executionId)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{
				var sqlqry = "select [ItemNumber], AssetUid as [uid], [ExecutionItemUid], [Message], [Success], DeletedCount from api.ExecutionDeleteAssetDataProfile where ExecutionID = @executionId order by ItemNumber asc";
				List<DataProfileDeleteResponse> results = (await connection.QueryAsync<DataProfileDeleteResponse>(sqlqry, new { executionId }, commandTimeout: CommandTimeout)).ToList();
				return results;
			}
		}

		public async Task<RepositoryResponse<bool>> ValidateDataProfileGetParameters(string profileIdentifier, IEnumerable<KeyValuePair<string, string>> queryParams, string isValid)
		{
			RepositoryResponse<bool> response = new RepositoryResponse<bool>(false, 200, true);

			if (string.IsNullOrWhiteSpace(profileIdentifier))
			{
				response.IsSuccess = false;
				response.StatusCode = 400;
				response.Message = Error.InvalidProfileIdentifier;
				return response;
			}

			string parameterName = "_assetuid";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				var assetuidstring = queryParams.ReadQueryParameterValue(parameterName);
				if (!Guid.TryParse(assetuidstring, out Guid assetUid))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = string.Format(Error.InvalidAssetUid, assetUid.ToString());
					return response;
				}
				else
				{
					var asset = AssetRepository.GetAssetByUID(assetUid);
					if (asset == null || (asset.AssetType.Class != AssetTypeClass.BusinessAsset && asset.AssetType.Class != AssetTypeClass.TechnicalAsset))
					{
						response.IsSuccess = false;
						response.StatusCode = 400;
						response.Message = string.Format(Error.InvalidAssetUid, assetUid.ToString());
						return response;
					}
				}
			}

			return await ValidateDataProfileBaseParameters(queryParams,isValid);
		}

		private async Task<RepositoryResponse<bool>> ValidateDataProfileBaseParameters(IEnumerable<KeyValuePair<string, string>> queryParams, string isValid)
		{
			RepositoryResponse<bool> response = new RepositoryResponse<bool>(false, 200, true);

			if (!string.IsNullOrEmpty(isValid))
			{
				response.IsSuccess = false;
				response.StatusCode = 400;
				response.Message = isValid;
				return response;
			}

			if (isValid.Length > 0)
			{
				response.IsSuccess = false;
				response.StatusCode = 400;
				response.Message = isValid;
				return response;
			}

			String parameterName = "_includetotal";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string includetotalString = queryParams.ReadQueryParameterValue(parameterName);
				if (!bool.TryParse(includetotalString, out _))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = Error.InvalidIncludeTotal;
					return response;
				}
			}

			parameterName = "_includesamples";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string includesamplesStringValue = queryParams.ReadQueryParameterValue(parameterName);
				if ( !bool.TryParse(includesamplesStringValue, out _))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = string.Format(Error.InvalidParameterMessage, includesamplesStringValue, parameterName);
					return response;
				}
			}

			return response;
		}

		public async Task<RepositoryResponse<bool>> ValidateMatchAssetGetParameters(Guid assetUid, string similarType, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			RepositoryResponse<bool> response = new RepositoryResponse<bool>(false, 200, true);

			var asset = await ReadAssetwithAssetTypeAsync(assetUid);
			if (asset == null || (asset.assetTypeClass != AssetTypeClass.BusinessAsset && asset.assetTypeClass != AssetTypeClass.TechnicalAsset))
			{
				response.IsSuccess = false;
				response.StatusCode = 400;
				response.Message = string.Format(Error.AssetUidIsNotValid, assetUid.ToString());
				return response;
			}

			string parameterName = "_includetotal";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string includeTotalStringValue = queryParams.ReadQueryParameterValue(parameterName);
				if (!bool.TryParse(includeTotalStringValue, out _))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = Error.InvalidIncludeTotal;
					return response;
				}
			}

			parameterName = "_order";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string[] allowedValues = new[] { "path", "tags" };
				string directionFilter = queryParams.ReadQueryParameterValue(parameterName);
				if (!allowedValues.Contains(directionFilter))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = Error.InvalidOrder;
					return response;
				}
			}

			parameterName = "_direction";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string[] allowedValues = new[] { "asc", "desc" };
				string directionFilter = queryParams.ReadQueryParameterValue(parameterName);
				if (!allowedValues.Contains(directionFilter))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = Error.InvalidDirection;
					return response;
				}
			}

			if (similarType != null)
			{
				string[] allowedValues = new[] { "structure", "data" };

				if (!allowedValues.Contains(similarType.ToLowerInvariant()))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = string.Format(Error.InvalidSimilarType, similarType);
					return response;
				}
			}
			else
			{
				response.IsSuccess = false;
				response.StatusCode = 400;
				response.Message = Label.RequiredSimilarType;
				return response;
			}

			using (var connection = ConnectionProvider.Connect(true))
			{
				string sqlqry = "Select * from AssetDataProfile where AssetId = @assetid order by ProfileSetDate desc";
				AssetDataProfile dataprofile = await connection.QueryFirstOrDefaultAsync<AssetDataProfile>(sqlqry, new { @assetid = asset.ID }, commandTimeout: CommandTimeout);
				if (dataprofile == null || similarType.ToLowerInvariant() == "structure" && dataprofile.StructureSignature == null || similarType.ToLowerInvariant() == "data" && dataprofile.DataSignature == null)
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = string.Format(Error.NoSimilarTypeForAssetUid, similarType, assetUid);
					return response;
				}

			}
			return response;
		}


		public async Task<RepositoryResponse<bool>> ValidateDataProfileGetParameters(Guid assetUid, IEnumerable<KeyValuePair<string, string>> queryParams, string isValid)
		{
			RepositoryResponse<bool> response = new RepositoryResponse<bool>(false, 200, true);

			var asset =  await ReadAssetwithAssetTypeAsync(assetUid);
			if (asset == null || (asset.assetTypeClass != AssetTypeClass.BusinessAsset && asset.assetTypeClass != AssetTypeClass.TechnicalAsset))
			{
				response.IsSuccess = false;
				response.StatusCode = 400;
				response.Message = string.Format(Error.InvalidAssetUid, assetUid.ToString());
				return response;
			}

			string parameterName = "_startdate";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string startdatestring = queryParams.ReadQueryParameterValue(parameterName);
				if (!DateTime.TryParse(startdatestring, out _))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = Error.InvalidStartDate;
					return response;
				}
			}

			parameterName = "_enddatete";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string enddatestring = queryParams.ReadQueryParameterValue(parameterName);
				if (!DateTime.TryParse(enddatestring, out _))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = Error.InvalidEndDate;
					return response;
				}
			}

			parameterName = "_includechildassets";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string includechildassetsstring = queryParams.ReadQueryParameterValue(parameterName);
				if (!bool.TryParse(includechildassetsstring, out _))
				{
					response.IsSuccess = false;
					response.StatusCode = 400;
					response.Message = Error.Invalid_includeChildAssetsProvided;
					return response;
				}
			}

			return await ValidateDataProfileBaseParameters(queryParams,isValid);
		}

		#region "Private method"
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
								,ADP.Freshness
								,ADP.ProfileSource
								,ADP.ProfileSeries
								,ADP.ProfileType
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

		private async Task<RepositoryResponse<string>> BuildMatchAssetsSQL(Guid assetUid, string similarType, IEnumerable<KeyValuePair<string, string>> queryParams, DynamicParameters dbArgs, bool onlyTotal = false, bool isExport = false)
		{
			RepositoryResponse<string> response = new("", 200, true);

			int pageNum = queryParams.CheckForPageNumber();
			int pageSize = queryParams.CheckForPageSize();

			string offset = "";
			if (pageSize > 0 || pageNum > 0)
			{
				offset = $"offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
			}

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

			var asset = await ReadAssetwithAssetTypeAsync(assetUid);

			if (asset == null)
			{
				response.StatusCode = 404;
				response.IsSuccess = false;
				response.Message = Error.AssetUidNotFound;
				return response;
			}

			AssetDataProfile dataprofile = null;
			using (var connection = ConnectionProvider.Connect(true))
			{
				string sqlqry = "Select * from AssetDataProfile where AssetId = @assetid order by ProfileSetDate desc";
				dataprofile = await connection.QueryFirstOrDefaultAsync<AssetDataProfile>(sqlqry, new { @assetid = asset.ID }, commandTimeout: CommandTimeout);
				if (dataprofile == null)
				{
					response.IsSuccess = false;
					response.StatusCode = 404;
					response.Message = Error.ProfileRecordNotExists;
					return response;
				}

			}

			if (similarType == null)
			{
				response.IsSuccess = false;
				response.StatusCode = 404;
				response.Message = Error.SignatureTypeRequired;
				return response;
			}
			else
			{
				string[] allowedValues = new[] { "structure", "data" };

				if (!allowedValues.Contains(similarType.ToLowerInvariant()))
				{
					response.IsSuccess = false;
					response.StatusCode = 404;
					response.Message = Error.SignatureTypeInvalid;
					return response;
				}
				else
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
			}

			if (queryParams.IsQueryParameterPresent("_includetotal"))
			{
				string includeTotalStringValue = queryParams.ReadQueryParameterValue("_includetotal");
				bool.TryParse(includeTotalStringValue, out includeTotal);
			}

			if (queryParams.IsQueryParameterPresent("_filter"))
			{
				List<FilterColumnOption> fieldList = new List<FilterColumnOption>
						{
							new FilterColumnOption("Tag", "T.tagString", SqlFieldType.Text),
							new FilterColumnOption("Path", "[Segments]", SqlFieldType.Xml)
						};

				// Parse and get back any advanced filters, and load dbArguments and where clauses.
				var advancedFilters = queryParams.ParseODataFilters();//.ParseAdvancedFilters();
				(dbArgs, filters) = advancedFilters.ConvertToSqlFilters(fieldList);

				hasAdvancedFilter = true;
			}

			if (hasAdvancedFilter || isExport)
			{
				filterJoinSQL = $@"outer Apply (
								Select tagString = STRING_AGG( value ,'|')
								 From  OpenJSON(tagsJson)
							 ) T";
			}

			string parameterName = "_direction";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string[] allowedValues = new[] { "asc", "desc" };
				string directionFilter = queryParams.ReadQueryParameterValue(parameterName);
				if (allowedValues.Contains(directionFilter))
				{
					orderDirection = directionFilter;
				}
			}

			string orderBySQL = $"td.[path] {orderDirection}";


			parameterName = "_order";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string orderBy = queryParams.ReadQueryParameterValue(parameterName);
				if (orderBy == "tags")
				{
					orderBySQL = $@"hasTagField {(orderDirection == "asc" ? "desc" : "asc")}, JSON_VALUE('{{""tags"":'+ISNULL(tagsJson, '[]')+'}}', '$.tags[0]') {orderDirection}, td.[path] {orderDirection}";
				}
			}

			parameterName = "_simplefilter";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				var simpleFilter = queryParams.ReadQueryParameterValue(parameterName);
				if (!string.IsNullOrEmpty(simpleFilter))
				{
					simpleFilter = GetEscapedFilterString(simpleFilter);

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

			if (!CurrentUserIsAdmin)
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

				dbArgs.Add("@userid", CurrentUserId);
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

				response.Data = onlyTotalSQL;

				return response;
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
			response.Data = sql;
			return response;
		}

		private async Task<bool> DoesTypeQualifierExist(string typeQualifier)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{
				var sqlqry = "select count(1) from AssetDataProfile where TypeQualifier = @typeQualifier";
				int results = (await connection.QueryFirstOrDefaultAsync<int>(sqlqry, new { typeQualifier }, commandTimeout: CommandTimeout));
				return results > 0 ? true : false;
			}
		}

		private async Task<bool> DoesSemanticTypeExist(string qualifier)
		{
			using (var connection = ConnectionProvider.Connect(true))
			{
				var sqlqry = "select count(1) from Semantic where qualifier = @qualifier";
				int results = (await connection.QueryFirstOrDefaultAsync<int>(sqlqry, new { qualifier }, commandTimeout: CommandTimeout));
				return results > 0 ? true : false;
			}
		}

		private async Task<bool> HasAssetPermissionByUid(Guid assetuid, Permission p, bool checkHasNoPermission)
		{
			using (var connection = ConnectionProvider.Connect())
			{
				connection.Open();

				var sqlqry = @"exec [dbo].[HasPermission] @resourceid, @p, @CheckHasNoPermission, @assetuid;";
				var results = (await connection.QueryFirstOrDefaultAsync<dynamic>(sqlqry, new { resourceid  = CurrentUserId, p, checkHasNoPermission, assetuid}, commandTimeout: CommandTimeout));
				if (results != null)
				{
					if (results.IsPermission ?? false)
					{
						return true;
					}
					else
					{
						return false;
					}
				}
				else
				{
					return false;
				}
			}
		}

		private async Task<bool> HasAssetPermissionByUid(List<Guid> assetUidList, Permission p, bool checkHasNoPermission)
		{
			if (assetUidList != null && assetUidList.Count() > 0)
			{
				int itemNumber = 1; 
				DataTable table = new DataTable();
				table.Columns.Add("ItemNumber", typeof(int));
				table.Columns.Add("AssetUid", typeof(Guid));
				table.Columns.Add("Assetid", typeof(long));
				table.Columns.Add("Permission", typeof(int));

				foreach (Guid item in assetUidList)
				{
					DataRow row = table.NewRow();
					row["ItemNumber"] = itemNumber;
					row["AssetUid"] = item;
					table.Rows.Add(row);

					itemNumber++;
				}

				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					connection.Open();
					await connection.ExecuteAsync(
						sql: @"IF OBJECT_ID('tempdb..#TempAssetUid') IS NOT NULL
												DROP TABLE #TempAssetUid;

												CREATE TABLE #TempAssetUid(
													ItemNumber int NOT NULL,
													AssetUid Uniqueidentifier,
													AssetId bigint,
													Permission int,
													PRIMARY KEY CLUSTERED ( [ItemNumber] ASC)
												);"
					);

					if (table.Rows.Count > 0)
					{
						SqlBulkCopy bulkCopy = connection.CreateBulkCopy("#TempAssetUid", table.Rows.Count, SqlBulkBatchTimeout);
						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
						bulkCopy.WriteToServer(table);

					}

					var sqlqry = @"exec [dbo].[HasPermission] @resourceid, @p, @CheckHasNoPermission, null;";
					var results = (await connection.QueryFirstOrDefaultAsync<dynamic>(sqlqry, new { resourceid = CurrentUserId, p, checkHasNoPermission }, commandTimeout: CommandTimeout));
					if (results != null)
					{
						if (results.IsPermission ?? false)
						{
							return true;
						}
						else
						{
							return false;
						}
					}
					else
					{
						return false;
					}
				}
			}
			else 
			{ 
				return false; 
			}
		}

		#endregion
	}
}
