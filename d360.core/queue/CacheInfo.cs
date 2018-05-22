using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.queue
{
    public enum CacheInfoObject
    {
        AssetDelete = 1,
        AssetEdit,
        AssetNoRead,
        AssetResponsibility
    }

    public enum CacheUpdateSource
    {
        None = 1,
        GroupBulkLoad,
        UserBulkLoad,
        UserFirstLogin,
        ResponsibilityRuleChange
    }

    public class CacheInfo : QueueObject
    {
        public CacheInfoObject CacheObject { get; set; }

        public CacheUpdateSource Source { get; set; }

        public long? SourceID { get; set; }
    }
}
