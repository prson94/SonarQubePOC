using d360.core.enums.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.queue
{

    public class EventObjectInfo
    {
        public SystemObjects Object { get; set; }

        public int ObjectID { get; set; }

        public SystemObjects ObjectType { get; set; }

        public int ObjectTypeID { get; set; }
        public int? Score { get; set; }
        public List<int> ChangedFieldIds { get; set; } = new List<int>();

        public int AssetTypeID { get; set; }
    }

    public class EventInfo
    {
        public string DomainPrefix { get; set; }

        public int CompanyID { get; set; }

        public int ResourceID { get; set; }
                
        public ChangeType Action { get; set; }

        public EventObjectInfo Object { get; set; }

        public long WorkflowItemID { get; set; }

        public long ItemStepID { get; set; }

        public long VersionStepTransitionID { get; set; }  
    }

    public class AssetEventInfo
    {
        public int CompanyID { get; set; }
        public AssetEventType Type { get; set; }
        public List<string> ChangedFieldNames { get; set; }
        public Guid Uid { get; set; }
    }

    public enum AssetEventType
    {
        Node,
        Edge,
        Path
    }
}
