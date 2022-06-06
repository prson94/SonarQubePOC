using System.Collections.Generic;
using AutoFixture;

namespace igx.UnitTests
{
	public static class AutoFixtureExtensions
	{
		public static IEnumerable<T> CreateEnumerable<T>(this IFixture fixture, int count = 3)
		{
			for (var i = 0; i < count; i++)
			{
				yield return fixture.Create<T>();
			}
		}
	}
}
