using System;
using System.Collections.Generic;

using d360.core.enums;

namespace d360.core.queue
{
    public enum IndexMode
    {
        Basic = 0,
        WithFields = 1,
        WithTags = 2,
        WithResponsibility = 4
    }

    public class IndexObjectModel : QueueObject
    {
        public int ID { get; set; }
        
        public long AssetID { get; set; }

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

        public Dictionary<string, List<int>> NoRead { get; set; }

        public string[] AssetPath { get; set; }

        public IndexMode IndexFlags { get; set; } = IndexMode.Basic;

        /// <summary>
        /// Returns the unique id for this search item
        /// </summary>
        /// <returns></returns>
        public string getObjectID()
        {
            if (!string.IsNullOrEmpty(ItemUniqueID))
            {
                if (Category == AssetTypeClass.BusinessAsset.ToString() || Category == AssetTypeClass.TechnicalAsset.ToString())
                {
                    return $"{SystemObjects.Artifact.ToString()}|{ItemUniqueID}";
                }

                return $"{Category}|{ItemUniqueID}";
            }

            return $"{Category}|{ID}";
        }

        /// <summary>
        /// Performs a shallow copy of the object
        /// </summary>
        /// <returns>Shallow copy of IndexObjectModel</returns>
        public IndexObjectModel ShallowCopy()
        {
            return (IndexObjectModel)MemberwiseClone();
        }
    }

    public enum ReindexBatchOperation
    {
        Update,
        Delete
    }

    public class ReindexModel : QueueObject
    {
        public string Category { get; set; }
        
        public Guid? AssetTypeUid { get; set; }
        
        public Guid? AssetUid { get; set; }
        
        public List<Guid> BatchUids { get; set; }
        
        public ReindexBatchOperation BatchOperation { get; set; } = ReindexBatchOperation.Update;
    }

    public class RebuildAssetGraphModel : QueueObject
    {
        //No additional properties
    }
}
