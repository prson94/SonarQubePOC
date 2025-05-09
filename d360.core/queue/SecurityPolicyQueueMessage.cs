using System;

namespace d360.core.queue
{
	public class SecurityPolicyArgs
	{
		public Guid? AssetUid { get; set; }
		public Guid? ExecutionUid { get; set; }
		public Guid? PolicyUid { get; set; }
		public bool IsDeleteAction { get; set; } = false;
	}

	public class SecurityPolicyQueueMessage
	{
        public int CompanyID { get; set; }

		public Guid? AssetUid { get; set; }
		public Guid? ExecutionUid { get; set; }
		public Guid? PolicyUid { get; set; }
		public bool IsDeleteAction { get; set; } = false;
	}

}
