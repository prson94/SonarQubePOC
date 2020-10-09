using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class IssueTypeApiModel
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public Guid? UpdatedByUid { get; set; }

        [DataMember]
        public bool IsSystem { get; set; }
    }

    public class AddIssueTypeApiModel
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public bool Success { get; set; }
    }

    public class DeleteIssueTypeAPIModel
    {
        [DataMember]
        public bool cascade { get; set; }
    }

    public class IssueInsertAPIModel
    {
        public Issue Issue { get; set; }
        public List<Field> fields = new List<Field>();
        public string Comment { get; set; }
    }

    public class IssueTypeAllocationsResponse
    {
        [DataMember]
        public Guid AssetTypeUid { get; set; }
        [DataMember]
        public AssetTypeClass Class { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Path { get; set; }
    }
}
