using System.Linq;
using AutoFixture;

namespace igx.UnitTests
{
	public static class FixtureProvider
	{
		public static IFixture Create()
		{
			var result = new Fixture();
			result.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
				.ForEach(b => result.Behaviors.Remove(b));
			result.Behaviors.Add(new OmitOnRecursionBehavior());
			return result;
		}
	}
}
