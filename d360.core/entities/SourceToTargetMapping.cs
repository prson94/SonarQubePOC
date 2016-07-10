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

    public class SourceTargetItem
    {
        [DataMember]
        public int FusionID { get; set; }

        [DataMember]
        public string Name { get; set; }
    }

    public class SourceToTargetSaveModel
    {
        public int SourceIntersectID { get; set; }
        public int TargetIntersectID { get; set; }
        public List<SourceTargetRule> Rules { get; set; }
    }

    public class SourceTargetRule : BaseIntObject, IIntObject
    {
        [DataMember]
        public  int FocalObjectID { get; set; }
        [DataMember, Column(TypeName = "varchar"), StringLength(150)]
        public string FocalObject { get; set; }
        [DataMember]
        public int SourceObjectID { get; set; }
        [DataMember, Column(TypeName = "varchar"), StringLength(150)]
        public string SourceObject { get; set; }
        [DataMember]
        public int TargetObjectID { get; set; }
        [DataMember, Column(TypeName = "varchar"), StringLength(150)]
        public string TargetObject { get; set; }
        [DataMember]
        public int Sequence { get; set; }

        [NotMapped, DataMember]
        public List<SourceTargetItem> Sources { get; set; }
        [NotMapped, DataMember]
        public List<SourceTargetItem> Targets { get; set; }

        [DataMember]
        public string Transformation { get; set; }
    }

    public class IntersectMapSourceTargetRule : BaseIntObject, IIntObject
    {
        [DataMember]
        public int RuleID { get; set; }
        [DataMember]
        public int IntersectMapID { get; set; }
    }
}
