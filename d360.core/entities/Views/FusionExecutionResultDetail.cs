using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionExecutionResultDetail : BaseObject
    {        
        [DataMember]
        public string FusionAttribute { get; set; }
        [DataMember]
        public string FusionAttributeType { get; set; }
        [DataMember]
        public int ExecutionID { get; set; }
        [DataMember]
        public int FusionAttributeID { get; set; }
        [DataMember]
        public string Body { get; set; }
        [DataMember]
        public int FieldTypeID { get; set; }
        [DataMember]
        public string FieldName { get; set; }
        [DataMember]
        public string Action { get; set; }
        [DataMember]
        public string OldValue { get; set; }
        [DataMember]
        public string NewValue { get; set; }
        [DataMember]
        public int FusionID { get; set; }
        [DataMember]
        public string Fusion { get; set; }
        [DataMember]
        public int FusionTypeID { get; set; }
        [DataMember]
        public string FusionType { get; set; }
    }
}
