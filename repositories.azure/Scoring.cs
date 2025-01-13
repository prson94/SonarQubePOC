namespace repositories.azure
{
	public class Scoring : Repository, IScoring
	{
		public Scoring(DapperConnectionProvider provider) : base(provider) { }
	}
}
