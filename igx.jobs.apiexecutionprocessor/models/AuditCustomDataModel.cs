using System.Collections.Generic;

namespace igx.jobs.apiexecutionprocessor
{
	public class AuditCustomDataModel
	{
		public string ActionObject { get; set; }
		public int ActionObjectID { get; set; }
		public string ActionObjectValue { get; set; }
		public int ResourceID { get; set; }
		public List<AuditCustomDataFieldModel> Fields { get; set; }
	}
}
