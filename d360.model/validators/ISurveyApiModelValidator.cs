using System.Collections.Generic;

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
    }
}
