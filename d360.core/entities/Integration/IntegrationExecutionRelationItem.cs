using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("ExecutionRelationItem", Schema = "integration")]
    public class IntegrationExecutionRelationItem : BaseGuidObject
    {
        [DataMember]
        public long ExecutionID { get; set; }

        [DataMember]
        public int SynchedAssetTypeID { get; set; }

        [DataMember]
        public string SubjectSourceID { get; set; }

        [DataMember]
        public string ObjectSourceID { get; set; }

        [DataMember]
        public int IntersectTypeID { get; set; }

        [IgnoreDataMember, ForeignKey("ExecutionID")]
        public virtual IntegrationExecution Execution { get; set; }
    }
}
