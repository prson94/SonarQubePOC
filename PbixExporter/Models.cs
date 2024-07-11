using System.Collections.Generic;

namespace PbixExporter
{
	public class GroupModel
	{
		public string groupId { get; set; }
		public List<string> reportIds { get; set; }
		public List<string> datasetIds { get; set; }
	}
}
