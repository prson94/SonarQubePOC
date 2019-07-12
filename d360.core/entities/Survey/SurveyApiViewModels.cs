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
        public int ID { get; set; }
        public string Comment { get; set; }
        public List<Response> Response { get; set; }
    }

    public class SurveyApiModel
    {
        public int Uid { get; set; }
        public string AssetUid { get; set; }
        public string UserUid { get; set; }
        public DateTime CreatedOn { get; set; }
        public List<Question> Question { get; set; }
    }

    public class SurveyApiResponseModel
    {
        public int pageSize { get; set; }
        public int pageNum { get; set; }
        public int total { get; set; }
        public DateTime asOfDate { get; set; }
        public List<SurveyApiModel> items { get; set; }
    }

}
