using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("ExecutionRoleItem", Schema = "integration")]
    public class IntegrationExecutionRoleItem : BaseGuidObject
    {
        [DataMember]
        public long ExecutionID { get; set; }

        [DataMember]
        public int SynchedAssetTypeID { get; set; }

        [DataMember]
        public string SourceID { get; set; }

        [DataMember]
        public string RoleName { get; set; }

        [DataMember]
        public string UserIdentifier { get; set; }

        [IgnoreDataMember, ForeignKey("ExecutionID")]
        public virtual IntegrationExecution Execution { get; set; }
    }
}
