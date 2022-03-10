using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities.Workflow
{
    [DataContract(Namespace = NAMESPACE), Table("TaskProcedure", Schema = "workflow")]
    public class WorkflowTaskProcedure : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        [DataMember, StringLength(250)]
        public string Name { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(1000)]
        public string Procedure { get; set; }

        [DataMember]
        public bool PassObjectInfo { get; set; }

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
    }
}
