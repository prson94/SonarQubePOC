using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.SurveyModels
{

    public class Response
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class Question
    {
        public Guid Uid { get; set; }
        public string Comment { get; set; }
        public List<Response> Response { get; set; } = new List<Response>();
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

}
