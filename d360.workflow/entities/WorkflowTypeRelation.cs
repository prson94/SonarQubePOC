using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Xml.Serialization;
using d360.core.entities;
using d360.core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;


namespace d360.workflow.entities
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class WorkflowTypeRelation : BaseIntObject
    {
        [DataMember]
        public WorkflowType WorkflowType { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string Parent { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        public bool Enabled { get; set; }

        [Column("Fields")]//[DataMember]
        public string FieldsXml { get; set; }

        [DataMember]
        public int ResponsibilityTypeID { get; set; }

        [DataMember, NotMapped]
        public Dictionary<string, string> Fields
        { 
            get 
            {
                var d = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(FieldsXml))
                {
                    foreach (var x in XElement.Parse(FieldsXml).Elements())
                    {
                        d.Add(x.Name.LocalName, x.Value);
                    }
                }
                return d;
            }
            set 
            {
                var xml = new XElement("fields");
                foreach (var k in value.Keys)
                {
                    xml.Add(new XElement(k, value[k]));
                }
                FieldsXml = xml.ToString();
            }
        }

        [ForeignKey("ResponsibilityTypeID")]
        public virtual ResponsibilityType ResponsibilityType { get; set; }
    }
}
