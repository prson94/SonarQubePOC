using Newtonsoft.Json.Linq;
using System.Net;

namespace d360.core.search
{
	public class JsonResponseModel
	{
		public JObject Data { get; set; }
		public HttpStatusCode Status { get; set; }
		public string StatusMessage { get; set; }

		public bool IsSuccessStatusCode => ((int)Status >= 200) && ((int)Status <= 299);
	}
}
