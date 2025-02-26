using System;
using System.Collections.Generic;
using System.Text;

namespace d360.core.enums.Workflow
{
	public enum StepState
	{
		[Description("Pending")]
		Pending = 1,

		[Description("Complete")]
		Complete = 2,

		[Description("Error")]
		Error = 3,

		[Description("Failed")]
		Failed = 4,

		[Description("Invalid Recipient")]
		InvalidRecipient = 5,

		[Description("HTTP Request Error")]
		HTTPRequestError = 6,

		[Description("No Valid Transitions")]
		NoValidTransitions = 7,

		[Description("Invalid Initiator")]
		InvalidInitiator = 8,

		[Description("No Valid Assignee")]
		NoValidAssignee = 9,

		[Description("No Valid Asset")]
		NoValidAsset = 10,
	}
}
