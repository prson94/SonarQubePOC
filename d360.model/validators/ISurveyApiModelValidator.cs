using d360.core.entities.SurveyModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.model.validators
{
    public interface ISurveyApiModelValidator
    {
        bool IsValidAsset(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidResource(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidSurveyType(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsRequiredGuidExistForDeleteSurveyResult(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidDate(IEnumerable<KeyValuePair<string, string>> queryParams, string parameterName);
        
        WorkHttpStatus ValidateGetSurveyTypesRequest(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<WorkHttpStatus> ValidateSurveyTypeCreateApiModel(SurveyTypeCreateApiModel surveyType);

		Task<WorkHttpStatus> ValidateSurveyTypeUpdateApiModel(Guid surveyTypeUid, SurveyTypeUpdateApiModel surveyType);

		Task<WorkHttpStatus> ValidateSurveyTypeDelete(Guid surveyTypeUid);

		Task<WorkHttpStatus> ValidateQuestionTypeCreate(Guid surveyTypeUid, QuestionTypeUpsertModel question);

		Task<WorkHttpStatus> ValidateQuestionTypeUpdate(Guid surveyTypeUid, Guid questionTypeUid, QuestionTypeUpsertModel question);

		Task<WorkHttpStatus> ValidateQuestionTypeDelete(Guid surveyTypeUid, Guid questionTypeUid);

		Task<WorkHttpStatus> ValidateGetQuestionTypesBySurveyType(Guid surveyTypeUid);
	}
}
