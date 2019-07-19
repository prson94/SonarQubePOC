using System;
using System.Collections.Generic;
using d360.core.entities;
using d360.core.entities.SurveyModels;

namespace d360.model.DataAccessLayer
{
    public interface ISurveyRepository
    {
        SurveyApiResponseModel GetSurveysResult(Guid surveyUid, IEnumerable<KeyValuePair<string, string>> queryParams);
        SurveyTypeApiResponseModel GetSurveyTypes(IEnumerable<KeyValuePair<string, string>> queryParams);
        SurveyType GetSurveyTypeByUid(Guid uid);
    }
}