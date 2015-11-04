using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Name="import")]
    public class BulkFusionImport
    {
        [DataMember]
        public List<Dictionary<string, string>> Models { get; set; }

        [DataMember]
        public FusionRelationshipModels Relationships { get; set; }
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
    }
}
