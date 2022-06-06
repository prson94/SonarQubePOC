using AutoFixture;

namespace igx.UnitTests
{
	public static class FixtureProvider
	{
		public static IFixture Create()
		{
			var result = new Fixture();
			result.Behaviors.Add(new OmitOnRecursionBehavior());
			return result;
		}
	}
}
