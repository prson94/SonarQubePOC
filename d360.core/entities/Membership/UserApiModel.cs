using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Membership
{
    public class UserApiDeleteModel
    {
        public Guid Uid { get; set; }
        public CompanyResource CompanyResource { get; set; }
        public GlobalReportingResource Resource { get; set; }
    }

    public class UserApiUpsertModel : IExecutionItem
    {
        [DataMember]
        public Guid? Uid { get; set; }
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string FirstName { get; set; }
        [DataMember]
        public string LastName { get; set; }
        [DataMember]
        public string Password { get; set; }
        [DataMember]
        public bool IsAdministrator { get; set; }
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }
        [DataMember]
        public CompanyResourceState? State { get; set; }
        [DataMember]
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        public bool IsNew { get; set; }
    }

    public class UserApiUpsertResult : IExecutionItem
    {
        public int ItemNumber { get; set; }
        public Guid? uid { get; set; }
        public Guid? ExecutionItemUid { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }
}
