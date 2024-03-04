using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
	[Table("AssetCrossReference")]
    public class AssetCrossReference
    {
        public Guid uid { get; set; }
        
        public string DataSource { get; set; }
        
        public string Type { get; set; }
        
        public string ExternalID { get; set; }
        
        public string FieldHash { get; set; }
    }
}
