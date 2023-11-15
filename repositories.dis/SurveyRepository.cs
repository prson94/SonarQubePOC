using d360.core.entities;
using d360.core.entities.SurveyModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace repositories.dis
{
	internal class SurveyRepository : ISurveyRepository
	{
		public Task<SurveyType> Create(SurveyType surveyType)
		{
			throw new NotImplementedException();
		}

		public Task<QuestionType> CreateQuestionType(QuestionType questionType)
		{
			throw new NotImplementedException();
		}

		public Task DeleteQuestionType(Guid questionTypeUid)
		{
			throw new NotImplementedException();
		}

		public int DeleteSurveyResults(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			throw new NotImplementedException();
		}

		public Task DeleteSurveyType(Guid uid)
		{
			throw new NotImplementedException();
		}

		public Task<SurveyAssetApiResponseModel> GetAssetSurvey(Guid assetUid)
		{
			throw new NotImplementedException();
		}

		public Task<List<QuestionTypeShortInfo>> GetQuestionTypesBySurveyType(Guid surveyTypeUid)
		{
			throw new NotImplementedException();
		}

		public Task<List<int>> GetSurveyQuestionResponses(Guid uid)
		{
			throw new NotImplementedException();
		}

		public QuestionType GetSurveyQuestionTypeByUid(Guid uid)
		{
			throw new NotImplementedException();
		}

		public Task<List<QuestionOptionShortInfo>> GetSurveyQuestionValues(Guid questionTypeUid)
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

		public Task<bool> IsUniqueQuestionTypeName(string name, int surveyTypeId, Guid? questionTypeUid)
		{
			throw new NotImplementedException();
		}

		public Task<bool> IsUniqueSurveyTypeName(string name, int assetTypeId, Guid? surveyTypeUid)
		{
			throw new NotImplementedException();
		}

		public Task PostSurveyResults(SurveyResultsApiModel model, Asset asset, SurveyType surveyType)
		{
			throw new NotImplementedException();
		}

		public Task<bool> QuestionHasAnswers(Guid questionTypeUid)
		{
			throw new NotImplementedException();
		}

		public Task<SurveyType> Update(SurveyType surveyType)
		{
			throw new NotImplementedException();
		}

		public Task UpdateQuestionType(QuestionType questionType)
		{
			throw new NotImplementedException();
		}
	}
}
