using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Membership
{
    public class GroupApiModel
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public Guid PrimaryOwnerUid { get; set; }

        [DataMember]
        public Guid SecondaryOwnerUid { get; set; }

        [DataMember]
        public bool IsActiveDirectoryGroup { get; set; } = false;
    }


    public class GroupApiModels 
    {
        [DataMember]
        public IEnumerable<GroupApiModel> items { get; set; }

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
