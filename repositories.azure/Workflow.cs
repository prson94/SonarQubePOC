namespace repositories.azure
{
	public class Workflow : Repository, IWorkflow
	{
		public Workflow(DapperConnectionProvider provider) : base(provider) { }
	}
}
