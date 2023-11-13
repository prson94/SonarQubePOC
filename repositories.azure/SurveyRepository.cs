using d360.core.entities;
using d360.core.entities.SurveyModels;
using Dapper;
using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class SurveyRepository : Repository, ISurveyRepository
	{
		public async Task<SurveyType> Create(SurveyType surveyType)
		{
			using (var db = new SqlConnection(ConnectionString))
			{
				await db.OpenAsync();

				if (surveyType.Uid == Guid.Empty)
				{ 
					surveyType.Uid = Guid.NewGuid();
				}
				var assetTypeCount = await db.QuerySingleAsync<int>(
					"select count(1) from AssetType where ID = @AssetTypeID",
					new { surveyType.AssetTypeID }
				);
				if (assetTypeCount != 1)
				{
					throw new ApplicationException("Asset type not found.");
				}

				await db.InsertAsync(surveyType);
			}
			return surveyType;
		}

		public async Task<QuestionType> CreateQuestionType(QuestionType questionType)
		{
			using (var db = new SqlConnection(ConnectionString))
			{
				await db.OpenAsync();

				questionType.Name = questionType.Name.Trim();

				var isValid = await db.QuerySingleAsync<bool>(
					"declare @valid bit = 0, @surveyTypeCount int, @questionTypeCount int;" +
					"select @surveyTypeCount = count(1) from SurveyType where SurveyTypeID = @SurveyTypeID;" +
					"select @questionTypeCount = count(1) from QuestionType where SurveyTypeID = @SurveyTypeID and Name = @Name;" +
					"if @surveyTypeCount = 1 and @questionTypeCount = 0 begin set @valid = 1 end;" +
					"select @valid",
					new { questionType.SurveyTypeID, questionType.Name }
				);
				if (!isValid)
				{
					throw new ApplicationException("Request to create question on survey is not valid.");
				}

				await db.InsertAsync(questionType);
			}
			return questionType;
		}

		public async Task DeleteQuestionType(Guid questionTypeUid)
		{
			throw new NotImplementedException();
		}

		public int DeleteSurveyResults(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			throw new NotImplementedException();
		}

		public async Task DeleteSurveyType(Guid uid)
		{
			using (var db = new SqlConnection(ConnectionString))
			{
				await db.OpenAsync();

				var existingSurveyType = await db.QueryAsync<int>("select count(1) from SurveyType where Uid = @uid", new { uid });
				if (existingSurveyType.Count() != 1)
				{
					throw new ApplicationException("Survey type not found.");
				};
				await db.ExecuteAsync(
					"declare @id int;" +
					"select @id = ID from SurveyType where Uid = @uid;" +
					"delete q from Question q inner join QuestionType qt on qt.ID = q.QuestionTypeID and qt.SurveyTypeID = @id;" +
					"delete SurveyType where ID = @id;",
					new { uid }
				);
			}
		}

		public async Task<SurveyAssetApiResponseModel> GetAssetSurvey(Guid assetUid)
		{
			throw new NotImplementedException();
		}

		public async Task<List<QuestionTypeShortInfo>> GetQuestionTypesBySurveyType(Guid surveyTypeUid)
		{
			throw new NotImplementedException();
		}

		public async Task<List<int>> GetSurveyQuestionResponses(Guid uid)
		{
			throw new NotImplementedException();
		}

		public QuestionType GetSurveyQuestionTypeByUid(Guid uid)
		{
			throw new NotImplementedException();
		}

		public async Task<List<QuestionOptionShortInfo>> GetSurveyQuestionValues(Guid questionTypeUid)
		{
			throw new NotImplementedException();
		}

		public SurveyResultSummaryApiResponseModel GetSurveyResultSummary(Guid surveyTypeUid, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			throw new NotImplementedException();
		}

		public SurveyApiResponseModel GetSurveysResult(Guid surveyUid, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			throw new NotImplementedException();
		}

		public SurveyType GetSurveyTypeByUid(Guid uid)
		{
			throw new NotImplementedException();
		}

		public SurveyTypeApiResponseModel GetSurveyTypes(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			throw new NotImplementedException();
		}

		public async Task<bool> IsUniqueQuestionTypeName(string name, int surveyTypeId, Guid? questionTypeUid)
		{
			throw new NotImplementedException();
		}

		public async Task<bool> IsUniqueSurveyTypeName(string name, int assetTypeId, Guid? surveyTypeUid)
		{
			throw new NotImplementedException();
		}

		public async Task PostSurveyResults(SurveyResultsApiModel model, Asset asset, SurveyType surveyType)
		{
			throw new NotImplementedException();
		}

		public async Task<bool> QuestionHasAnswers(Guid questionTypeUid)
		{
			throw new NotImplementedException();
		}

		public async Task<SurveyType> Update(SurveyType surveyType)
		{
			using (var db = new SqlConnection(ConnectionString))
			{
				await db.OpenAsync();

				var existingSurveyType = await db.GetAsync<SurveyType>(surveyType.ID);
				if (existingSurveyType != null)
				{
					throw new ApplicationException("Survey type not found.");
				}
				existingSurveyType.ValidForDays = surveyType.ValidForDays;
				existingSurveyType.UpdatedBy = 0;
				existingSurveyType.UpdatedOn = DateTime.UtcNow;
				existingSurveyType.Description = surveyType.Description;
				existingSurveyType.Name = surveyType.Name;

				await db.UpdateAsync(existingSurveyType);

				return existingSurveyType;
			}
		}

		public Task UpdateQuestionType(QuestionType questionType)
		{
			throw new NotImplementedException();
		}
	}
}
