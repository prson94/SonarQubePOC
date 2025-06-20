namespace repositories.azure
{
	public partial class Workflow : Repository, IWorkflow
	{
		public Workflow(DapperConnectionProvider provider) : base(provider) { }
	}
}
