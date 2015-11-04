using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class CommentCategory : BaseObject
    {
        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public string Category { get; set; }

        [DataMember]
        public string Name { get; set; }

        [NotMapped]
        public ICollection<CommentCategory> Items { get; set; }

        [NotMapped]
        public int Count { get; set; }
    }
}
