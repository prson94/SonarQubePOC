using System.Web.Http;
using System.Web.Http.Results;
using FluentAssertions;

namespace igx.UnitTests
{
	public static class FluentAssertExtensions
	{
		public static TResult ShouldBeOKContent<TResult>(this IHttpActionResult actionResult)
		{
			var okResult = actionResult.Should().BeOfType<OkNegotiatedContentResult<TResult>>().Subject;
			return okResult.Content;
		}
	}
}
