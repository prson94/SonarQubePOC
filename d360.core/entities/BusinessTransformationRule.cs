using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class BusinessTransformationRule : BaseIntObject, IIntObject
    {
        [DataMember]
        public int FocalObjectID { get; set; }
        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string FocalObject { get; set; }
        [DataMember]
        public int SourceObjectID { get; set; }
        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string SourceObject { get; set; }
        [DataMember]
        public int TargetObjectID { get; set; }
        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string TargetObject { get; set; }
        [DataMember]
        public string Transformation { get; set; }

        [DataMember, NotMapped]
        public string SourceName { get; set; }
        [DataMember, NotMapped]
        public string SourceTypeName { get; set; }
        [DataMember, NotMapped]
        public string SourceForeColor { get; set; }
        [DataMember, NotMapped]
        public string SourceBackColor { get; set; }
        [DataMember, NotMapped]
        public string TargetName { get; set; }
        [DataMember, NotMapped]
        public string TargetTypeName { get; set; }
        [DataMember, NotMapped]
        public string TargetForeColor { get; set; }
        [DataMember, NotMapped]
        public string TargetBackColor { get; set; }

    }
}
