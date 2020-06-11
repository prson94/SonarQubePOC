using d360.core.enums;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Name="import")]
    public class BulkFusionImport
    {
        public BulkFusionImport()
        {
            Models = new List<Dictionary<string, string>>();
            QueryItems = new List<IDictionary<string, string>>();
            Relationships = new FusionRelationshipModels();
            Errors = new List<string>();
        }

        [DataMember]
        public List<Dictionary<string, string>> Models { get; set; }

        [DataMember]
        public List<IDictionary<string, string>> QueryItems { get; set; }

        [DataMember]
        public FusionRelationshipModels Relationships { get; set; }

        [DataMember]
        public string Version { get; set; }

        [DataMember]
        public List<string> Errors { get; set; }

        [DataMember]
        public bool ForceRefresh { get; set; } = false;
    }

    [CollectionDataContract(Name = "items")]
    public class FusionRelationshipModels : List<FusionRelationshipModel> { }

    [DataContract(Name = "item")]
    public class FusionRelationshipModel
    {
        [DataMember]
        public string StartID { get; set; }
        
        [DataMember]
        public string EndID { get; set; }

        [DataMember]
        public string Action { get; set; }

        [DataMember]
        public PredicateType PredicateType { get; set; } = PredicateType.Simple;
    }
}
