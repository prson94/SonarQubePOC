namespace repositories.azure
{
	public class Social : Repository, ISocial
	{
		public Social(DapperConnectionProvider provider) : base(provider) { }
	}
}
