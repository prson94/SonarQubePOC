using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace igx.tests
{
    public class GenericIgcPagingModel
    {
        public int numTotal { get; set; }
        public string next { get; set; }
        public int pageSize { get; set; }
        public int end { get; set; }
        public int begin { get; set; }
    }

    public class IgcModels
    {
        public GenericIgcPagingModel paging { get; set; }
    }

    public class IgcDynamicArrayModels : IgcModels
    {
        public JArray items { get; set; }
    }

    [TestClass]
    public class IgcDataTests
    {
        [TestMethod]
        public void GetRelationshipCounts_Success()
        {
            var fields = new List<string>() {
                "writes_to_(design)",
                "assigned_to_terms",
                "reads_from_(user_defined)",
                "labels",
                "reads_from_(static)",
                "impacted_by",
                "custom_System of Record Designation",
                "impacts_on",
                "reads_from_(design)",
                "writes_to_(operational)",
                "writes_to_(user_defined)",
                "writes_to_(static)",
                "implements_rules",
                "reads_from_(operational)",
                "governed_by_rules"
            };
            var json = File.OpenText("igc_sample.json").ReadToEnd();
            var obj = JObject.Parse(json);
            foreach (var prop in obj.Properties())
            {
                if (fields.Contains(prop.Name))
                {
                    var model = JsonConvert.DeserializeObject<IgcModels>(prop.ToString());
                    var count = model.paging.numTotal;
                }
            }
        }
    }
}
