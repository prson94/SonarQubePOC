using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

using d360.core.entities;
using d360.core.entities.SurveyModels;
using d360.core.resources;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers.filters;
using Dapper;

using Newtonsoft.Json;

namespace d360.model.DataAccessLayer
{
	public class SurveyRepository : BaseRepository, ISurveyRepository
	{
		private readonly ICompanyContext companyContext;
		public SurveyRepository(ICompanyContext context)
			: base(context)
		{
			companyContext = context;
		}

		public async Task<SurveyType> Create(SurveyType surveyType) {
			this.companyContext.Add(surveyType);
			await this.companyContext.SaveChangesAsync();
			return surveyType;
		}

		public async Task<SurveyType> Update(SurveyType surveyType)
		{
			this.companyContext.Update(surveyType);
			await this.companyContext.SaveChangesAsync();
			return surveyType;
		}

		public async Task DeleteSurveyType(Guid uid)
		{
			var surveyType = GetSurveyTypeByUid(uid);
			var id = surveyType.ID;

			await companyContext.DeleteAsync<d360.core.entities.Question>(i => i.Survey.SurveyTypeID == id);
			await companyContext.DeleteAsync<Survey>(i => i.SurveyTypeID == id);
			await companyContext.DeleteAsync<QuestionType>(i => i.SurveyTypeID == id);
			await companyContext.DeleteAsync<SurveyType>(i => i.ID == id);
		}

		public async Task<QuestionType> CreateQuestionType(QuestionType questionType)
		{
			companyContext.Add(questionType);
			await companyContext.SaveChangesAsync();
			return questionType;
		}

		public async Task UpdateQuestionType(QuestionType update)
		{
			var questionType = this.companyContext
				.GetWithIncludes<QuestionType>(x => x.QuestionTypeOptions)
				.FirstOrDefault(x => x.Uid == update.Uid);

			questionType.Name = update.Name;
			questionType.DisplayStyle = update.DisplayStyle;
			questionType.Description = update.Description;

			var options = Enumerable.Zip(
				questionType.QuestionTypeOptions,
				update.QuestionTypeOptions,
				(existingOpt, updateOpt) => (existingOpt, updateOpt));

			foreach (var (existingOpt, updateOpt) in options)
			{
				existingOpt.Name = updateOpt.Name;
				existingOpt.Value = updateOpt.Value;
			}

			var addedOptions = update.QuestionTypeOptions.Where((_, index) => index >= questionType.QuestionTypeOptions.Count);
			foreach (var addedOption in addedOptions)
			{
				questionType.QuestionTypeOptions.Add(addedOption);
			}

			var deletedOptions = questionType.QuestionTypeOptions.Where((_, index) => index >= update.QuestionTypeOptions.Count).ToList();
			foreach (var deletedOption in deletedOptions)
			{
				companyContext.QuestionTypeOptions.Remove(deletedOption);
			}

			companyContext.Update(questionType);

			await companyContext.SaveChangesAsync();
		}

		public async Task DeleteQuestionType(Guid uid)
		{
			await companyContext.DeleteAsync<QuestionType>(i => i.Uid == uid);
		}

		public SurveyType GetSurveyTypeByUid(Guid uid)
		{
			return companyContext.SurveyTypes.FirstOrDefault(x => x.Uid == uid);
		}

		public QuestionType GetSurveyQuestionTypeByUid(Guid uid)
		{
			return companyContext.QuestionTypes.FirstOrDefault(x => x.Uid == uid);
		}

		public async Task<List<int>> GetSurveyQuestionResponses(Guid uid)
		{
			return (await companyContext.QueryAsync<int>("" +
				@" select distinct [value] 
					from    QuestionTypeOption O 
							inner join QuestionType T on T.ID = O.QuestionTypeID 
							and T.UID = @uid", new { uid }, ApiTimeout)).ToList();
		}

		public SurveyApiResponseModel GetSurveysResult(Guid surveyUid, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var response = new SurveyApiResponseModel
			{
				asOfDate = DateTime.Now.Date,
				pageSize = 200,
				pageNum = 1,
				total = 0
			};

			var additionalWhereClause = string.Empty;
			foreach (var param in queryParams)
			{
				switch (param.Key.ToLower())
				{
					case "_pagesize":
						int size = 0;
						if (int.TryParse(param.Value, out size))
						{
							response.pageSize = int.Parse(param.Value);
						}
						else
						{
							throw new ArgumentException("Invalid value for page size parametar!");
						}

						break;
					case "_pagenum":
						long num = 0;
						if (long.TryParse(param.Value, out num))
						{
							response.pageNum = num;
							if (response.pageNum <= 0)
							{
								response.pageNum = 1;
							}
						}
						else
						{
							throw new ArgumentException(OthersError.InvalidPageNum);
						}

						break;
					case "assetuid":
						Guid uid = Guid.Parse(param.Value);
						if (uid == Guid.Empty)
						{
							throw new ArgumentException("Invalid value for asset uid!");
						}

						additionalWhereClause += $" AND a.uid = '{uid}'";
						break;
					case "asofdate":
						DateTime date = DateTime.MinValue;
						if (!DateTime.TryParse(param.Value, out date))
						{
							throw new ArgumentException("Invalid date value for AsOfDate parameter!");
						}
						response.asOfDate = date;
						additionalWhereClause += $" AND S.CreatedOn <= '{date.AddDays(1)}'";
						break;
				}
			}

			var pagingSql = $"OFFSET {response.pageSize * (response.pageNum - 1)} ROWS FETCH NEXT {response.pageSize} ROWS ONLY";

			var countQuery = $@"select count(*)
									from dbo.SurveyType ST
										inner join Survey S on S.SurveyTypeID = ST.ID
										inner join Asset A on A.ID = S.AssetID
									where ST.Uid = @surveyTypeUID
									{additionalWhereClause}
									 ";

			response.total = companyContext.Query<int>(countQuery, new { surveyTypeUID = surveyUid }, ApiTimeout).FirstOrDefault();

			var query = $@"select S.Uid as Uid,
							a.uid as AssetUid,
							U.uid as UserUid,
							S.CreatedOn,
							(select 
									distinct QT.Uid,
									Q.Comment, 
									(select QTO.Name, QTO.Value from QuestionTypeOption QTO 
										inner join QuestionOption QO ON Q.ID = QO.QuestionID
										inner join QuestionType QT ON QTO.QuestionTypeID = QT.ID
										where QO.QuestionTypeOptionID = QTO.id 
										for json path) as Response		
								from Question Q
									inner join QuestionOption QO on QO.QuestionID = Q.ID
									inner join QuestionTypeOption QTO on QTO.ID = QO.QuestionTypeOptionID
									inner join QuestionType QT on QT.ID = QTO.QuestionTypeID
								where Q.SurveyID = S.Id for json path) as Questions
						
						 from dbo.SurveyType ST
							inner join Survey S on S.SurveyTypeID = ST.ID
							inner join Asset A on A.ID = S.AssetID
							inner join Asset U on U.Object = 'Resource' and U.ObjectID = S.ResourceID
						where ST.Uid = @surveyTypeUID
						{additionalWhereClause}
						order by S.CreatedOn
						{pagingSql}
						for json path";

			var itemsJson = string.Join("", companyContext.Query<string>(query, new { surveyTypeUID = surveyUid }, ApiTimeout).ToList());

			response.items = JsonConvert.DeserializeObject<List<SurveyApiModel>>(itemsJson) ?? new List<SurveyApiModel>();
			return response;
		}

		public SurveyTypeApiResponseModel GetSurveyTypes(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var response = new SurveyTypeApiResponseModel();
			var sqlParams = new DynamicParameters();
			response.pageSize = 200;
			response.pageNum = 1;
			response.total = 0;

			string orderByClause = "order by ST.CreatedOn";
			string orderByDirection = "asc";
			List<string> whereClauses = new List<string>();
			foreach (var param in queryParams)
			{
				switch (param.Key.ToLower())
				{
					case "hasresponses":
						if (bool.Parse(param.Value))
						{
							whereClauses.Add("Responses > 0");
						}
						else
						{
							whereClauses.Add("Responses is null or Responses = 0");
						}
						break;
					case "assettypeuid":
						if (Guid.TryParse(param.Value, out Guid assetTypeUid))
						{
							whereClauses.Add($"AT.Uid = @assetTypeUid");
							sqlParams.Add("@assetTypeUid", assetTypeUid);
						}
						break;
					case "surveytypeuid":
						if (Guid.TryParse(param.Value, out Guid surveyTypeUid))
						{
							whereClauses.Add($"ST.Uid = @surveyTypeUid");
							sqlParams.Add("@surveyTypeUid", surveyTypeUid);
						}
						break;
					case "_pagesize":
						int size = 0;
						if (int.TryParse(param.Value, out size))
						{
							response.pageSize = int.Parse(param.Value);
						}
						break;
					case "_pagenum":
						int num = 0;
						if (int.TryParse(param.Value, out num))
						{
							response.pageNum = int.Parse(param.Value);
							if (response.pageNum <= 0)
							{
								response.pageNum = 1;
							}
						}
						break;
					case "_order":
						switch (param.Value.ToLower())
						{
							case "name": orderByClause = "order by ST.Name"; orderByDirection = "asc"; break;
							case "validfordays": orderByClause = "order by ST.ValidForDays"; orderByDirection = "desc"; break;
							case "createdon": orderByClause = "order by ST.CreatedOn"; orderByDirection = "asc"; break;
							case "updatedon": orderByClause = "order by ST.UpdatedOn"; orderByDirection = "asc"; break;
							case "numberofresponses": orderByClause = "order by NumberOfResponses"; orderByDirection = "desc"; break;
						}
						break;
					case "_direction":
						switch (param.Value.ToLower())
						{
							case "asc": orderByDirection = "asc"; break;
							case "desc": orderByDirection = "desc"; break;
						}
						break;
					case "_simplefilter":
						whereClauses.Add("ST.Name like @simpleFilter or ST.ValidForDays like @simpleFilter");
						sqlParams.Add("@simpleFilter", companyContext.GetEscapedFilterString(param.Value, isContains: true));
						break;
					case "_filter":
						var fieldList = new List<DefaultFilter>
						{
							new DefaultFilter("name", "ST.Name", SqlFieldType.Text),
							new DefaultFilter("validForDays", "ST.ValidForDays", SqlFieldType.Number),
							new DefaultFilter("createdon", "ST.CreatedOn", SqlFieldType.DateTime),
							new DefaultFilter("updatedon", "ST.CreatedOn", SqlFieldType.DateTime),
							new DefaultFilter("numberofresponses", "NumberOfResponses", SqlFieldType.Number)
						};

						companyContext.ParseAdvancedFilterQueryParameter(
							queryParams,
							fieldList, 
							out DynamicParameters advFilterArgs, 
							out List<string> advFilterStatements);

						if (advFilterArgs != null && advFilterStatements != null)
						{
							sqlParams.AddDynamicParams(advFilterArgs);
							whereClauses.AddRange(advFilterStatements);
						}

						break;
				}
			}

			sqlParams.Add("@pageSize", response.pageSize);
			sqlParams.Add("@pageNum", response.pageNum);

			var additionalWhereClause = whereClauses.Count > 0 ? $"where {string.Join(" AND ", whereClauses)}" : "";

			var pagingSql = $"OFFSET (@pageSize * (@pageNum - 1)) ROWS FETCH NEXT @pageSize ROWS ONLY";

			var countQuery = $@"select  count(*) from dbo.SurveyType ST 
											inner join AssetType AT on AT.ID = ST.AssetTypeID
											left join (select SurveyTypeId, Count(*) as Responses from Survey Group by SurveyTypeId)Responses 
												on Responses.SurveyTypeId = ST.Id {additionalWhereClause}";
			response.total = companyContext.Query<int>(countQuery, sqlParams, ApiTimeout).FirstOrDefault();

			string QuestionsCTE = @"select 
										ST.Id as TypeId,
										QT.Uid,
										QT.Name,
										QT.Description,
										CASE
											WHEN QT.DisplayStyle = 1 THEN 'Radio'
											WHEN QT.DisplayStyle = 2 THEN 'Rating'
											WHEN QT.DisplayStyle = 3 THEN 'CheckList'
										END AS DisplayStyle,
										(select Name, Value from QuestionTypeOption WHERE QuestionTypeID = QT.Id for json path) as Options
										from QuestionType QT
										cross apply(
											select ST.ID From SurveyType ST where QT.SurveyTypeID = ST.ID
											)ST(Id)";

			string query = $@";WITH QuestionTypes AS ({QuestionsCTE})
								select 
									ST.Uid,
									AT.Uid as AssetTypeUid,
									ST.Name,
									ISNULL(ST.Description, '') as Description,
									ST.ValidForDays,
									ST.CreatedOn,
									ACreate.uid as CreatedByUid,
									ST.UpdatedOn,
									AUpdate.uid as UpdatedByUid,
									Responses as NumberOfResponses,
									(select Uid, Name, Description, DisplayStyle, Options from QuestionTypes where TypeId = ST.Id for json path) as Questions
								 from SurveyType ST
								 inner join AssetType AT on AT.ID = ST.AssetTypeID
								 inner join Asset ACreate on ACreate.Object = 'Resource' AND ACreate.ObjectID = ST.CreatedBy
								 inner join Asset AUpdate on AUpdate.Object = 'Resource' AND AUpdate.ObjectID = ST.UpdatedBy
								 left join (select SurveyTypeId, Count(*) as Responses from Survey Group by SurveyTypeId)Responses on Responses.SurveyTypeId = ST.Id
								{additionalWhereClause}
								{orderByClause} {orderByDirection}
								{pagingSql}
								for json path";

			var itemsJson = string.Join("", companyContext.Query<string>(query, sqlParams, ApiTimeout).ToList());

			response.items = JsonConvert.DeserializeObject<List<SurveyTypeApiModel>>(itemsJson) ?? new List<SurveyTypeApiModel>();

			return response;
		}

		public SurveyResultSummaryApiResponseModel GetSurveyResultSummary(Guid surveyTypeUid, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var response = new SurveyResultSummaryApiResponseModel
			{
				pageSize = 200,
				pageNum = 1,
				total = 0,
				asOfDate = DateTime.Now.Date
			};

			string orderByClause = "order by First.CreatedOn";
			var additionalWhereClause = string.Empty;

			List<string> whereClauses = new List<string>();
			foreach (var param in queryParams)
			{
				switch (param.Key.ToLower())
				{
					case "assetuid":
						Guid assetGuid = Guid.Parse(param.Value);
						whereClauses.Add($"A.uid = '{assetGuid}'");
						break;
					case "asofdate":
						DateTime date = DateTime.MinValue;

						if (!DateTime.TryParse(param.Value, out date))
						{
							throw new ArgumentException("Invalid date value for AsOfDate parameter!");
						}

						whereClauses.Add($"S.CreatedOn <= '{date.Date.AddDays(1)}'");
						response.asOfDate = date;
						break;
					case "_pagesize":
						int size = 0;

						if (int.TryParse(param.Value, out size))
						{
							response.pageSize = int.Parse(param.Value);
						}
						else
						{
							throw new ArgumentException("Invalid value for page size parametar!");
						}

						break;
					case "_pagenum":
						long pageNum = 0;

						if (long.TryParse(param.Value, out pageNum))
						{
							response.pageNum = pageNum;
							if (response.pageNum <= 0)
							{
								response.pageNum = 1;
							}
						}
						else
						{
							throw new ArgumentException(OthersError.InvalidPageNum);
						}

						break;
					case "_order":
						switch (param.Value.ToLower())
						{
							case "firstrespondedon": orderByClause = "order by First.CreatedOn"; break;
							case "lastrespondedon": orderByClause = "order by Last.CreatedOn"; break;
							case "numberofresponders": orderByClause = "order by QD.Responders desc"; break;
							default: throw new ArgumentException("Invalid value for order parametar! Use FirstRespondedOn|LastRespondedOn|NumberOfResponders");
						}
						break;
				}
			}

			var pagingSql = $"OFFSET {response.pageSize * (response.pageNum - 1)} ROWS FETCH NEXT {response.pageSize} ROWS ONLY";
			var countWhereClause = whereClauses.Count > 0 ? "and " + string.Join(" and ", whereClauses) : "";
			var countQuery = $@"select count(distinct A.uid) from Asset A
								inner join Survey S ON A.ID = S.AssetID
								inner join SurveyType ST on S.SurveyTypeID = ST.ID
								where ST.uid = @surveyTypeUid
								{countWhereClause}";
			response.total = companyContext.Query<int>(countQuery, new { surveyTypeUid }, ApiTimeout).FirstOrDefault();

			if (whereClauses.Count > 0)
			{
				additionalWhereClause = "WHERE " + string.Join(" and ", whereClauses);
			}

			string AnswersCTE = $@"
							select S.uid as SurveyUid,S.CreatedOn, A.uid as AssetUid, QT.Uid as QuestionTypeID, QTO.Name,QTO.Value from QuestionTypeOption QTO
									inner join QuestionOption QO on QO.QuestionTypeOptionID = QTO.ID
									inner join Question Q on QO.QuestionID = Q.ID
									inner join QuestionType QT on QT.ID = QTO.QuestionTypeID
									inner join Survey S on Q.SurveyID = S.ID
									inner join Asset A on A.ID = S.AssetID
									{additionalWhereClause}";

			string QuestionsCTE = @"select 
									QT.Uid, 
									S.SurveyTypeID, 
									A.Uid as AssetUid,
									(select AD.Name, AD.Value, count(*) as Count 
									   from AnswerData AD 
									   where AD.AssetUid = A.Uid AND AD.QuestionTypeID = QT.Uid 
									   group by AD.Name, AD.Value
									   for json path) Responses
								 from QuestionOption QO
									inner join QuestionTypeOption QTO on QTO.ID = QO.QuestionTypeOptionID
									inner join Question Q on QO.QuestionId = Q.Id
									inner join Survey S on S.ID = Q.SurveyID
									inner join Asset A on A.ID = S.AssetID
									inner join QuestionType QT on QT.ID = QTO.QuestionTypeID
									group by QT.Uid, S.SurveyTypeID, A.Uid";

			var sql = $@";with AnswerData as ({AnswersCTE}),QuestionsData as ({QuestionsCTE})
							select  
							A.uid as AssetUid,
							First.CreatedOn as FirstRespondedOn,
							Last.CreatedOn as LastRespondedOn,
							QD.Responders as NumberOfResponders,
							(select Uid, Responses
								from QuestionsData 
								where AssetUid = A.uid 
								for json path) AS Questions 
							from SurveyType ST 
							inner join Survey S on S.SurveyTypeID = ST.ID
							inner join Asset A on A.ID = S.AssetID
							cross apply (select count(distinct SurveyUid) as Responders from AnswerData where AssetUid = A.uid)QD
							cross apply (select top 1 CreatedOn from AnswerData where AssetUid = A.uid order by CreatedOn)First
							cross apply (select top 1 CreatedOn from AnswerData where AssetUid = A.uid order by CreatedOn desc)Last
							where ST.Uid = @surveyTypeUid
							group by A.uid, QD.Responders, First.CreatedOn, Last.CreatedOn
							{orderByClause}
							{pagingSql}
							for json path";

			var itemsJson = string.Join("", companyContext.Query<string>(sql, new { surveyTypeUid }, ApiTimeout).ToList());

			response.items = JsonConvert.DeserializeObject<List<SurveyResultSummaryApiModel>>(itemsJson) ?? new List<SurveyResultSummaryApiModel>();

			return response;
		}

		public async Task<SurveyAssetApiResponseModel> GetAssetSurvey(Guid assetUid)
		{
			var surveys = (await companyContext.QueryAsync<SurveyAssetApiResponseModel>(
				@"select	ST.[uid] as SurveyTypeUid,
							ST.[Name]
					from	SurveyType ST
							inner join AssetType T on T.ID = ST.AssetTypeId
							inner join Asset A on A.[uid] = @assetUid and A.AssetTypeID = T.ID
					where	ST.ID not in (
								select	SurveyTypeID 
								from	Survey S
										inner join Asset B on B.ID = S.AssetID
								where	S.SurveyTypeID = ST.ID
										and S.ResourceID = @resourceId
										and S.CreatedOn > DATEADD(day, (ST.ValidForDays * -1), getdate())
										and B.[uid] = @assetUid
							)"
			, new { assetUid, resourceId = companyContext.CurrentResourceID }, ApiTimeout)).ToList();

			if (!surveys?.Any() ?? true)
			{
				return null;
			}

			if (surveys.Count == 1)
			{
				return surveys.First();
			}

			var rand = new Random();
			var index = rand.Next(0, surveys.Count);

			return surveys[index];
		}

		public int DeleteSurveyResults(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			List<string> joins = new List<string>();
			List<string> whereStatements = new List<string>();
			var dbArgs = new DynamicParameters();

			if (queryParams != null)
			{
				if (queryParams.ToList().Any(q => q.Key.ToLower() == "surveytypeuid"))
				{
					var surveytypeUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "surveytypeuid").Value;
					if (Guid.TryParse(surveytypeUIDString, out Guid surveyTypeUid))
					{
						joins.Add("inner join SurveyType ST on ST.ID = S.SurveyTypeID");
						whereStatements.Add("ST.uid=@surveyTypeUid");
						dbArgs.Add("surveyTypeUid", surveyTypeUid);
					}
				}

				if (queryParams.ToList().Any(q => q.Key.ToLower() == "assetuid"))
				{
					var assetUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assetuid").Value;
					if (Guid.TryParse(assetUIDString, out Guid assetUid))
					{
						joins.Add("inner join Asset A on A.ID = S.AssetID");
						whereStatements.Add("A.uid=@assetUid");
						dbArgs.Add("assetUid", assetUid);
					}
				}

				if (queryParams.ToList().Any(q => q.Key.ToLower() == "resourceuid"))
				{
					var resourceString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "resourceuid").Value;
					if (Guid.TryParse(resourceString, out Guid resourceUid))
					{
						joins.Add("inner join reporting.Global_Resource R on S.ResourceID = R.ResourceID");
						whereStatements.Add("R.uid=@resourceUid");
						dbArgs.Add("resourceUid", resourceUid);
					}
				}

				if (queryParams.ToList().Any(q => q.Key.ToLower() == "startdaterange"))
				{
					var startDateString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "startdaterange").Value;
					if (DateTime.TryParse(startDateString, out DateTime startDate))
					{
						whereStatements.Add("S.CreatedOn >=@startDate");
						dbArgs.Add("startDate", startDate);
					}
				}

				if (queryParams.ToList().Any(q => q.Key.ToLower() == "enddaterange"))
				{
					var endDateString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "enddaterange").Value;
					if (DateTime.TryParse(endDateString, out DateTime endDate))
					{
						endDate = endDate.AddDays(1);
						whereStatements.Add("S.CreatedOn < @endDate");
						dbArgs.Add("endDate", endDate);
					}
				}
			}

			var whereSql = "";
			if (whereStatements.Any())
			{
				whereSql = $" where {string.Join("  and  ", whereStatements)}";
			}

			var joinSql = "";
			if (joins.Any())
			{
				joinSql = $"\n {string.Join("\n", joins)}";
			}

			string sql = $"Select S.uid from Survey S " +
						$" {joinSql}  {whereSql}";
			var sUids = companyContext.Query<Guid>(sql, dbArgs).ToList();

			int result = companyContext.Connection.Execute($@"
					SET NOCOUNT ON; 
					drop table if exists #tempSurveryIds;
					create table #tempSurveryIds  (id int);


					insert into #tempSurveryIds
					select Id from dbo.Survey where  uid in @surveyUids;
				   
					delete from dbo.QuestionOption where QuestionID in (select ID from dbo.Question where SurveyID in (Select Id from #tempSurveryIds));
				 
					delete from dbo.Question where SurveyID in (Select Id from #tempSurveryIds);
					SET NOCOUNT OFF; 
					delete from dbo.Survey where  id in  (Select Id from #tempSurveryIds);
				   
				", new { surveyUids = sUids });

			return result;
		}

		public async Task PostSurveyResults(SurveyResultsApiModel model, Asset asset, SurveyType surveyType)
		{
			var survey = new Survey
			{
				SurveyTypeID = surveyType.ID,
				AssetID = asset.ID,
				ResourceID = companyContext.CurrentResourceID,
				CreatedOn = DateTime.UtcNow
			};

			companyContext.SaveOrUpdate(survey);

			foreach (var question in model.Questions)
			{
				var q = new core.entities.Question
				{
					SurveyID = survey.ID,
					Comment = question.Comments
				};

				companyContext.SaveOrUpdate(q);

				foreach (var value in question.Responses)
				{
					(await companyContext.QueryAsync<int>(
						@"insert into QuestionOption (QuestionID, QuestionTypeOptionID)
							select  @questionId, 
									O.ID 
							from    QuestionType T 
									inner join QuestionTypeOption O on O.QuestionTypeID = T.ID
						where       T.Uid = @SurveyQuestionUid 
									and O.Value = @value
						", new { questionId = q.ID, question.SurveyQuestionUid, value })).FirstOrDefault();
				}
			}
		}

		public async Task<bool> IsUniqueSurveyTypeName(string name, int assetTypeId, Guid? surveyTypeUid)
		{
			var sameNameSurveyTypes = await companyContext.QueryAsync<bool>(
				@"
					select top 1 1
					from dbo.SurveyType st
					where st.AssetTypeId = @assetTypeId
						and st.Name = @name 
						and (
							@surveyTypeUid is null 
							or st.Uid <> @surveyTypeUid
						)
				",
				new { name, assetTypeId, surveyTypeUid });

			return !sameNameSurveyTypes.Any();
		}

		public async Task<bool> IsUniqueQuestionTypeName(string name, int surveyTypeId, Guid? questionTypeUid)
		{
			var sameNameQuestionTypes = await companyContext.QueryAsync<bool>(
				@"
					select top 1 1
					from dbo.QuestionType qt
					where qt.SurveyTypeID = @surveyTypeId
						and qt.Name = @name 
						and (
							@questionTypeUid is null 
							or qt.Uid <> @questionTypeUid
						)
				",
				new { name, surveyTypeId, questionTypeUid });

			return !sameNameQuestionTypes.Any();
		}

		public async Task<bool> QuestionHasAnswers(Guid questionTypeUid)
		{
			var answers = await companyContext.QueryAsync<bool>(
				@"
					select top 1 1
					from QuestionType qt
						inner join QuestionTypeOption qto on qt.ID = qto.QuestionTypeID
						inner join QuestionOption qo on qo.QuestionTypeOptionID = qto.ID
					where qt.Uid = @questionTypeUid
				",
				new { questionTypeUid });

			return answers.Any();
		}

		public async Task<List<QuestionTypeShortInfo>> GetQuestionTypesBySurveyType(Guid surveyTypeUid)
		{
			return await this.companyContext
				.GetWithIncludes<QuestionType>(x => x.QuestionTypeOptions)
				.Where(x => x.SurveyType.Uid == surveyTypeUid)
				.Select(i => new QuestionTypeShortInfo
				{
					Uid = i.Uid,
					Name = i.Name,
					OptionCount = i.QuestionTypeOptions.Count,
					DisplayStyle = i.DisplayStyle,
					Description = i.Description
				})
				.ToListAsync();
		}

		public async Task<List<QuestionOptionShortInfo>> GetSurveyQuestionValues(Guid questionTypeUid)
		{
			var options = await companyContext.QueryAsync<QuestionOptionShortInfo>(
				@"
					select 
						opt.Name,
						opt.[Value]
					from dbo.QuestionTypeOption opt
					join dbo.QuestionType question on question.ID = opt.QuestionTypeID
					where question.Uid = @questionTypeUid
					order by opt.ID
				",
				new { questionTypeUid });

			return options.ToList();
		}
	}
}
