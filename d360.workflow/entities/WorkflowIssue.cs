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
using d360.core.enums;

namespace d360.workflow.entities
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class WorkflowIssue : BaseObject
    {
        [DataMember, Key]
        public Guid WorkflowID { get; set; }

        [DataMember]
        public int CommentID { get; set; }

        [DataMember]
        public int CreatingResourceID { get; set; }

        [DataMember]
        public DateTime DateStarted { get; set; }

        [DataMember]
        public DateTime? DateCompleted { get; set; }

        [DataMember]
        public bool IsCompleted { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public string RaisedBy { get; set; }

        [DataMember]
        public int? ObjectID{ get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public string Comments { get; set; }

        [DataMember]
        public int IssueType { get; set; }

        [DataMember]
        public string IssueTypeName { get; set; }

        [DataMember]
        public int IssueID { get; set; }

        [DataMember]
        public IssueCriticality Criticality { get; set; }

        [DataMember]
        public string CriticalityName { get; set; }
        
    }
}
