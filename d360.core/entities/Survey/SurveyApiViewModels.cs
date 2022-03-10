using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace d360.core.entities.SurveyModels
{
    public class Option
    {
        public string Name { get; set; }

        public int Value { get; set; }
    }

    public class Question
    {
        public Guid Uid { get; set; }

        public string Comment { get; set; }

        public List<Option> Response { get; set; } = new List<Option>();
    }

    public class SurveyApiModel
    {
        public Guid Uid { get; set; }

        public Guid AssetUid { get; set; }

        public Guid UserUid { get; set; }

        public DateTime CreatedOn { get; set; }

        public List<Question> Questions { get; set; }
    }

    public class SurveyApiResponseModel
    {
        public int pageSize { get; set; }

        public int pageNum { get; set; }

        public int total { get; set; }

        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        public DateTime asOfDate { get; set; }

        public List<SurveyApiModel> items { get; set; } = new List<SurveyApiModel>();
    }

    public class SurveyAPIDeleteResultsResponseModel
    {
        public bool Success { get; set; }

        public string Message { get; set; }
    }

    public class QuestionDescriptive
    {
        public Guid Uid { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string DisplayStyle { get; set; }

        public List<Option> Options { get; set; }
    }

    public class SurveyTypeApiModel
    {
        public Guid Uid { get; set; }

        public Guid AssetTypeUid { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int ValidForDays { get; set; }

        public DateTime CreatedOn { get; set; }

        public Guid CreatedByUid { get; set; }

        public DateTime UpdatedOn { get; set; }

        public Guid UpdatedByUid { get; set; }

        public int NumberOfResponses { get; set; }

        public List<QuestionDescriptive> Questions { get; set; } = new List<QuestionDescriptive>();
    }

    public class SurveyTypeApiResponseModel
    {
        public int pageSize { get; set; }

        public int pageNum { get; set; }

        public int total { get; set; }

        public List<SurveyTypeApiModel> items { get; set; } = new List<SurveyTypeApiModel>();
    }

    public class Response
    {
        public string Name { get; set; }

        public int Value { get; set; }

        public int Count { get; set; }
    }

    public class Questions
    {
        public Guid Uid { get; set; }

        public List<Response> Responses { get; set; } = new List<Response>();
    }

    public class SurveyResultSummaryApiModel
    {
        public Guid AssetUid { get; set; }

        public int NumberOfResponders { get; set; }

        public DateTime FirstRespondedOn { get; set; }

        public DateTime LastRespondedOn { get; set; }

        public List<Questions> Questions { get; set; } = new List<Questions>();
    }

    public class SurveyResultSummaryApiResponseModel
    {
        public int pageSize { get; set; }

        public int pageNum { get; set; }

        public int total { get; set; }

        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        public DateTime asOfDate { get; set; }

        public List<SurveyResultSummaryApiModel> items { get; set; } = new List<SurveyResultSummaryApiModel>();
    }

    public class SurveyAssetApiResponseModel
    {
        public Guid SurveyTypeUid { get; set; }

        public string Name { get; set; }
    }

    public class SurveyResultsApiModel
    {
        public Guid AssetUid { get; set; }

        public List<SurveyQuestionResponseApiModel> Questions { get; set; }
    }

    public class SurveyQuestionResponseApiModel
    {
        public Guid SurveyQuestionUid { get; set; }

        public List<int> Responses { get; set; }

        public string Comments { get; set; }
    }

    public class DateFormatConverter : IsoDateTimeConverter
    {
        public DateFormatConverter(string format)
        {
            DateTimeFormat = format;
        }
    }
}
