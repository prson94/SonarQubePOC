using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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

    public class RelationshipModels : List<RelationshipModel> { }

    public class RelationshipModel
    {
        public SystemObjects StartType { get; set; }
        public int StartID { get; set; }
        public SystemObjects EndType { get; set; }
        public int EndID { get; set; }

        public int? ClassificationID { get; set; }
        public int? RoleID { get; set; }
    }
}
