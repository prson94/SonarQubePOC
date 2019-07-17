using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public DateTime asOfDate { get; set; }
        public List<SurveyApiModel> items { get; set; } = new List<SurveyApiModel>();
    }



    public class QuestionDescriptive
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DisplayStyle { get; set; }
        public List<Option> Options { get; set; }
    }

    public class SurveyTypeApiModel
    {
        public string Uid { get; set; }
        public string AssetTypeUid { get; set; }
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

}
