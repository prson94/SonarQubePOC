using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using d360.core.enums.Workflow;

namespace d360.core.entities.Workflow
{
	[DataContract, Table("ItemStepTransitionState", Schema = "workflow")]
	public class WorkflowItemStepTransitionState : BaseObject
	{
		[DataMember, Key, Column(Order = 1)]
		public long FromItemStepID { get; set; }

		[DataMember, Key, Column(Order = 2)]
		public long VersionStepTransitionID { get; set; }

		[DataMember]
		public bool Passed { get; set; }

		[DataMember]
		public StepState? State { get; set; }


		[IgnoreDataMember, ForeignKey("FromItemStepID")]
		public virtual WorkflowItemStep FromItemStep { get; set; }

		[IgnoreDataMember, ForeignKey("VersionStepTransitionID")]
		public virtual WorkflowVersionStepTransition VersionStepTransition { get; set; }
	}
}
