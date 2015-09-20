using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    /// <summary>
    /// Loaded when the stored procedure GetRelationships is executed.  This is the type of result that is returned.
    /// </summary>
    [DataContract(Namespace = NAMESPACE)]
    public class GetRelationshipModel: BaseObject
    {
        [DataMember]
        public int? IntersectID { get; set; }
        [DataMember]
        public string ObjectType { get; set; }
        [DataMember]
        public int ObjectID { get; set; }
        [DataMember]
        public string ObjectName { get; set; }
        [DataMember]
        public string TypeName { get; set; }
        [DataMember]
        public string Url { get; set; }
    }
}
