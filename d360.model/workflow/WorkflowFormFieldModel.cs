using System;

namespace d360.model.workflow
{
    public class WorkflowFormFieldModel
    {
        public string ID { get; set; }

        public string Label { get; set; }

        public string Value { get; set; }

        public string FieldType { get; set; }

        public string IntersectTypeID { get; set; }
    }

    public class WorkflowAssignmentSummary
    {
        public int Version { get; set; }

        public string StepName { get; set; }

        public string ObjectName { get; set; }

        public string TypeName { get; set; }

        public bool SendFormEmail { get; set; }
    }

	public class UsersOutstandingWorkflows
	{
		public string Name { get; set; }
		public int Id { get; set; }
		public int Version { get; set; }
		public string Step { get; set; }
		public int StepId { get; set; }
		public int Total { get; set; }
		public int New { get; set; }
		public Guid WorkflowTypeUid { get; set; }
		public Guid? WorkflowItemStepUid { get; set; }
	}
}
