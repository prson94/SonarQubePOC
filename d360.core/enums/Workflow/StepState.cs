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

		[Description("Dead")]
		Failed = 4
	}
}
