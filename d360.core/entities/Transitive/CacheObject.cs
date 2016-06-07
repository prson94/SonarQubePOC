using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [Table("Object", Schema = "cache")]
    public partial class CacheObject
    {
        [Key, Column(Order =1)]
        public string Object { get; set; }

        [Key, Column(Order = 2)]
        public int ObjectID { get; set; }

        public string ObjectType { get; set; }

        public int ObjectTypeID { get; set; }

    }
}
