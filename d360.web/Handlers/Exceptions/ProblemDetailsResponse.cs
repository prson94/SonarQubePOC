using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace d360.web.Handlers.Exceptions
{
	public sealed class ProblemDetailsResponse
	{
		public ProblemDetailsResponse()
		{
			Extra = new Dictionary<string, object>();
		}

		/// <summary>
		/// Problem Type.
		/// </summary>
		[JsonProperty("type")]
		public string Type { get; set; }

		/// <summary>
		/// Title.
		/// </summary>
		[JsonProperty("title")]
		public string Title { get; set; }

		/// <summary>
		/// Status code.
		/// </summary>
		[JsonProperty("status")]
		public int Status { get; set; }

		/// <summary>
		/// Details of the problem.
		/// </summary>
		[JsonProperty("detail")]
		public string Detail { get; set; }

		/// <summary>
		/// Request method
		/// </summary>
		[JsonProperty("method")]
		public string Method { get; set; }

		/// <summary>
		/// Request url
		/// </summary>
		[JsonProperty("instance")]
		public string Instance { get; set; }

		/// <summary>
		/// Extra problem properties.
		/// </summary>
		[JsonExtensionData]
		public IDictionary<string, object> Extra { get; }

		[Obsolete, JsonProperty("message")]
		// leave this for back compatibility with old error message
		public string Message => Detail;
	}
}
