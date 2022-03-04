using System;
using System.Collections.Generic;

using d360.core.enums;
using d360.core.enums.Workflow;

namespace d360.core.queue
{
    public class EventObjectInfo
    {
        public SystemObjects Object { get; set; }

        public int ObjectID { get; set; }

        public SystemObjects ObjectType { get; set; }

        public int ObjectTypeID { get; set; }

        public List<int> ChangedFieldIds { get; set; } = new List<int>();

        public int AssetTypeID { get; set; }
        public ScoreType? ScoreType { get; set; } = null;
    }

    public class EventInfo : IServiceBusMessageType
    {
        public string DomainPrefix { get; set; }

        public int CompanyID { get; set; }

        public int ResourceID { get; set; }

        public ChangeType Action { get; set; }

        public EventObjectInfo Object { get; set; }

        public long WorkflowItemID { get; set; }

        public long ItemStepID { get; set; }

        public long VersionStepTransitionID { get; set; }

        public int MessageType { get { return (int)Action; } }
    }

    public class AssetEventInfo : IServiceBusMessageType
    {
        public int CompanyID { get; set; }
        
        public AssetEventType Type { get; set; }
        
        public List<string> ChangedFieldNames { get; set; }
        
        public Guid Uid { get; set; }
        
        public ApiExecutionInfo execution { get; set; }
        
        public int MessageType { get { return (int)Type; } }
    }

    public enum AssetEventType
    {
        Node,
        Edge,
        Path,
        Execution,
        AssetType
    }
}
