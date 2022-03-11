using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class AssetCrossReference
    {
        [DataMember]
        public Guid uid { get; set; }
        
        [DataMember, Column(TypeName = "varchar"), StringLength(250)]
        public string DataSource { get; set; }
        
        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Type { get; set; }
        
        [DataMember, Column(TypeName = "varchar"), StringLength(250)]
        public string ExternalID { get; set; }
        
        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string FieldHash { get; set; }
    }
}
