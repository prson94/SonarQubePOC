using System;

namespace igx.jobs.bulkloadprocessor
{
	public class CommunityUserAddResultModel
    {
        public int LoadID { get; set; }
        public int RowIndex { get; set; }
        public string UserStatus { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int EnvironmentID { get; set; }
        public int? ClientID { get; set; }
        public int? ResourceID { get; set; }
        public Guid Uid { get; set; }
        public bool? Success { get; set; }
        public string Message { get; set; }
    }
}
