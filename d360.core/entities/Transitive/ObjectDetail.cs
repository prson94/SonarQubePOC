using System;
using System.ComponentModel.DataAnnotations.Schema;

using d360.core.enums;

namespace d360.core.entities
{
    public class ObjectDetail
    {
        public int ID { get; set; }
        
        public long? AssetID { get; set; }
        
        public Guid? UID { get; set; }
        
        public int? AssetTypeID { get; set; }
        
        public Guid? AssetTypeUid { get; set; }
        
        public string Name { get; set; }
        
        public string TextPath { get; set; }
        
        public string Description { get; set; }
        
        public int? ParentID { get; set; }
        
        public string ParentType { get; set; }
        
        public string Url { get; set; }
        
        public string Status { get; set; }
        
        public int TypeID { get; set; }
        
        public string Type { get; set; }
        
        public string TypeName { get; set; }
        
        public AssetTypeClass Class { get; set; }
        
        public string IconBackColor { get; set; }
        
        public string IconForeColor { get; set; }
        
        public string IconText { get; set; }

        [NotMapped]
        public string PluralizedName { get; set; }
    }
}
