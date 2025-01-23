using d360.core.entities.Workflow;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace repositories.azure
{
	public partial class Workflow : Repository, IWorkflow
	{
		public Workflow(DapperConnectionProvider provider) : base(provider) { }

	}
}
