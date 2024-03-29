namespace repositories.azure
{
	public abstract class Repository
	{
		public int CurrentUserId { get; set; }

		public Platform Platform { get { return Platform.Azure; } }

		public DapperConnectionProvider ConnectionProvider { get; set; }

		protected Repository(DapperConnectionProvider provider)
		{
			ConnectionProvider = provider;		
		}
	}
}
