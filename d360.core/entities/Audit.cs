using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Global_Audit", Schema = "reporting")]
    public class Audit : BaseObject
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public int ResourceID { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public string Action { get; set; }

        [DataMember]
        public string ActionObject { get; set; }

        [DataMember]
        public int ActionObjectID { get; set; }

        [DataMember]
        public string ActionObjectTypeName { get; set; }

        [DataMember]
        public string ActionObjectName { get; set; }

        [DataMember]
        public string ActionDescription { get; set; }


        [IgnoreDataMember, ForeignKey("ResourceID")]
        public virtual GlobalReportingResource Resource { get; set; }

        [IgnoreDataMember, ForeignKey("AuditID")]
        public virtual ICollection<AuditField> AuditFields { get; set; }
    }
}
