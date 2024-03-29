using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.enums.Workflow;

namespace d360.core.entities.Workflow
{
    [DataContract, Table("ItemStepStateDetail", Schema = "workflow")]
    public class WorkflowItemStepStateDetail : BaseIntObject, IIntObject
    {
		[DataMember]
		public long itemStepID { get; set; }

		[DataMember]
		public StepState State { get; set; }

		[DataMember]
		public string Message { get; set; }

		[DataMember]
		public string Details { get; set; }	
    }
}
