using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities.Membership
{
    public class GroupApiModels
    {
        [DataMember]
        public IEnumerable<dynamic> items { get; set; }

        [DataMember]
        public int Total { get; set; }
    }

    public class GroupResponseResult : IExecutionItem
    {
        public int ItemNumber { get; set; }

        public Guid? uid { get; set; }

        public Guid? ExecutionItemUid { get; set; }

        public string Message { get; set; }

        public bool Success { get; set; }
    }
}
