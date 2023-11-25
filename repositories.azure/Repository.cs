namespace repositories.azure
{
	public abstract class Repository
	{
		public Platform Platform { get { return Platform.Azure; } }

		public DapperConnectionProvider ConnectionProvider { get; set; }

		public Repository(DapperConnectionProvider provider)
		{
			ConnectionProvider = provider;		
		}
	}
}
