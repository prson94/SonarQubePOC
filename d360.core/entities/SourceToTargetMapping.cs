using System;
using System.Collections.Generic;
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
        public string ID { get; set; }
        [DataMember]
        public string FusionID { get; set; }

        [IgnoreDataMember]
        public int ObjectID { get
            {
                var i = 0;
                try
                {
                    i = int.Parse(this.ID.Split('|')[1]);
                }
                catch { }
                return i;
            }
        }

        [IgnoreDataMember]
        public string Object
        {
            get
            {
                return this.ID.Split('|')[0];
            }
        }

        [IgnoreDataMember]
        public int AttributeID
        {
            get
            {
                var i = 0;
                try
                {
                    i = int.Parse(this.FusionID.Split('|')[1]);
                }
                catch { }
                return i;
            }
        }

        [IgnoreDataMember]
        public string AttributeType
        {
            get
            {
                return this.FusionID.Split('|')[0];
            }
        }
    }

    public class SourceToTargetSaveModel
    {
        public string Focal { get; set; }
        public int FocalID { get; set; }
        public string Source { get; set; }
        public int SourceID { get; set; }
        public string Target { get; set; }
        public int TargetID { get; set; }
        public List<SourceTargetRule> Rules { get; set; }
    }

    public class SourceTargetRule : BaseIntObject
    {
        [DataMember]
        public  int FocalObjectID { get; set; }
        [DataMember]
        public string FocalObject { get; set; }
        [DataMember]
        public int SourceObjectID { get; set; }
        [DataMember]
        public string SourceObject { get; set; }
        [DataMember]
        public int TargetObjectID { get; set; }
        [DataMember]
        public string TargetObject { get; set; }

        [NotMapped, DataMember]
        public List<SourceTargetItem> Sources { get; set; }
        [NotMapped, DataMember]
        public List<SourceTargetItem> Targets { get; set; }

        [DataMember]
        public string Transformation { get; set; }
    }

    public class IntersectMapSourceTargetRule : BaseIntObject
    {
        [DataMember]
        public int RuleID { get; set; }
        [DataMember]
        public int IntersectMapID { get; set; }
    }
}
