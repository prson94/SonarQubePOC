using System.Collections.Generic;
using AutoFixture;

namespace igx.UnitTests
{
	public static class AutoFixtureExtensions
	{
		public static IEnumerable<T> CreateEnumerable<T>(this IFixture fixture, int count = 3)
		{
			for (int i = 0; i < 3; i++)
			{
				yield return fixture.Create<T>();
			}
		}
	}
}