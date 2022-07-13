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
		[JsonIgnore]
		//[JsonProperty("status")]
		public int Status { get; set; }


		/// <summary>
		/// Details of the problem.
		/// </summary>
		//[JsonProperty("detail")]
		[JsonIgnore]
		public string Detail { get; set; }

		/// <summary>
		/// Request method
		/// </summary>
		//[JsonProperty("method")]
		[JsonIgnore]
		public string Method { get; set; }

		/// <summary>
		/// Request url
		/// </summary>
		//[JsonProperty("instance")]
		[JsonIgnore]
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
