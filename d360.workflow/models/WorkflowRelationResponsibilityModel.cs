using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.workflow.models
{
    [DataContract]
    public class WorkflowRelationResponsibilityModel
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public WorkflowType WorkflowType { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public string Parent { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        public string ParentName { get; set; }

        [DataMember]
        public bool Enabled { get; set; }

        public string Fields { get; set; }

        [DataMember]
        public int ResponsibilityTypeID { get; set; }

        [DataMember]
        public string ResponsibilityType { get; set; }

        [DataMember]
        public string WorkflowTypeName { get { return WorkflowType.ToString(); } }

        [DataMember]
        public string WorkflowTypeDisplayName { get { return WorkflowType.GetWorkflowTypeDisplayName(); } }

        [DataMember]
        public Dictionary<string, string> Properties 
        { 
            get 
            {
                var d = new Dictionary<string, string>();
                foreach (var x in XElement.Parse(Fields).Elements())
                {
                    d.Add(x.Name.LocalName, x.Value);
                }
                return d;
            } 
        }
    }
}
