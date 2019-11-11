using System;
using System.Collections.Generic;

namespace d360.core.queue
{
    public class IndexObjectModel : QueueObject
    {
        public int ID { get; set; }

        public string ItemUniqueID { get; set; }

        public string RelativeUrl { get; set; }

        /// <summary>
        /// This is the name of the underlying asset type, such as:
        /// Business Term, Application, etc.
        /// </summary>
        public string AssetType { get; set; }

        /// <summary>
        /// This value contains the asset type class
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Asset UID
        /// </summary>
        public Guid? Uid { get; set; }

        public Guid? AssetTypeUid { get; set; }

        public Dictionary<string, string> Fields { get; set; }

        public Dictionary<string, string> Tags { get; set; }

        /// <summary>
        /// Returns the unique id for this search item
        /// </summary>
        /// <returns></returns>
        public string getObjectID()
        {
            if (!string.IsNullOrEmpty(ItemUniqueID))
                return $"{Category}|{ItemUniqueID}";
            return $"{Category}|{ID}";
        }
    }

    public class ReindexModel : QueueObject
    {
        //No additional properties
    }

    public class RebuildAssetGraphModel : QueueObject
    {
        //No additional properties
    }
}
