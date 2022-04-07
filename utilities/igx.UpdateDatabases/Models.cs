using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace igx.UpdateDatabases
{
    public class Result
    {
        public string Server { get; set; }
        
        public string DatabaseName { get; set; }
        
        public string UrlPrefix { get; set; }
        
        public DateTime StartedOn { get; set; }
        
        public DateTime? CompletedOn { get; set; }
        
        public string Message { get; set; }
        
        public List<DatabaseResult> Queries { get; set; }
    }

    public class DatabaseResult
    {
        public string QueryText { get; set; }
        
        public JArray Results { get; set; }
    }
}
