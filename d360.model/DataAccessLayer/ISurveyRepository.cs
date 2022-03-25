using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using d360.core.entities;
using d360.core.entities.SurveyModels;

namespace d360.model.DataAccessLayer
{
    public interface ISurveyRepository
    {
        SurveyApiResponseModel GetSurveysResult(Guid surveyUid, IEnumerable<KeyValuePair<string, string>> queryParams);
        
        SurveyTypeApiResponseModel GetSurveyTypes(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        SurveyResultSummaryApiResponseModel GetSurveyResultSummary(Guid surveyTypeUid, IEnumerable<KeyValuePair<string, string>> queryParams);
        
        SurveyType GetSurveyTypeByUid(Guid uid);
        
        QuestionType GetSurveyQuestionTypeByUid(Guid uid);
        
        Task<SurveyAssetApiResponseModel> GetAssetSurvey(Guid assetUid);
        
        int DeleteSurveyResults(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        Task PostSurveyResults(SurveyResultsApiModel model, Asset asset, SurveyType surveyType);
        
        Task<List<int>> GetSurveyQuestionResponses(Guid uid);
    }
}
