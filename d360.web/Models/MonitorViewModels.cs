using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using d360.core.entities;
using System.Data;
using d360.core;
using System.Collections.Specialized;

namespace d360.web.Models
{
    public class CloseEventViewModel
    {
        public Event Event { get; set; }
        public List<Resolution> Resolutions { get; set; }
    }

    public class EventDefinitionListViewModel
    {
    }

    public class ExceptionDefinitionEditModel
    {
        public Fields Fields { get; set; }
        //public List<QualityExceptionType> Types { get; set; }
    }

    public class ResultsGridViewModel
    {
        public ResultsGridViewModel()
        {
            EditorTab = 0;
        }

        public SystemObjects Type { get; set; }
        public int ID { get; set; }
        public int EditorTab { get; set; }
        public Dictionary<string, string> Statuses { get; set; }
    }

    public class ResultDetailViewModel
    {
        public Event Result { get; set; }
        public List<Field> Fields {get;set;}
    }

    public class ResolutionViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public string CreatingResource { get; set; }
        public string UpdatingResource { get; set; }
    }
}