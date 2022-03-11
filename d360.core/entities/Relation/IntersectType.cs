using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.enums;
using d360.core.queue;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectType : BaseIntObject, IIntObject, IUpdatedMetadata, IEventTrackedEntity
    {
        [DataMember]
        public Guid uid { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Subject { get; set; }

        [DataMember]
        public int SubjectID { get; set; }

        [DataMember]
        public Guid? SubjectUid { get; set; }

        [DataMember]
        public Cardinality SubjectCardinality { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public Guid? ObjectUid { get; set; }

        [DataMember]
        public Cardinality ObjectCardinality { get; set; }

        [DataMember]
        public bool? IsSystem { get; set; }

        [DataMember]
        public int? PredicateID { get; set; }

        [ForeignKey("PredicateID")]
        public virtual Predicate Predicate { get; set; }

        [ForeignKey("IntersectTypeID")]
        public virtual ICollection<Intersect> Intersects { get; set; }

        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.IntersectType,
                ObjectID = ID,
                ObjectType = SystemObjects.IntersectType,
                ObjectTypeID = 0
            };
        }
    }

    public class IntersectTypeApiPredicateViewModel
    {
        public Guid Uid { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PredicateType Type { get; set; }

        public string FriendlyTypeName
        {
            get
            {
                if (Type == 0)
                {
                    return null;
                }
                else
                {
                    return Type.GetName();
                }
            }
        }

        public string Name { get; set; }

        public string Inverse { get; set; }
    }

    public class IntersectTypeApiEdgeViewModel
    {
        public Guid Uid { get; set; }

        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public AssetTypeClass Class { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Cardinality Cardinality { get; set; }
    }

    [NotMapped]
    public class IntersectTypeApiViewNamed : IntersectType
    {
        [DataMember]
        public string Name { get; set; }
    }

    public class IntersectTypeApiViewModel : BaseObject
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public Guid Uid { get; set; }

        [DataMember, JsonConverter(typeof(StringEnumConverter))]
        public State State { get; set; }

        [DataMember]
        public bool IsSystem { get; set; }

        [DataMember]
        public IntersectTypeApiPredicateViewModel Predicate { get; set; }

        [DataMember]
        public IntersectTypeApiEdgeViewModel Subject { get; set; }

        [DataMember]
        public IntersectTypeApiEdgeViewModel Object { get; set; }
        [DataMember]
        public bool? HasFieldTypes { get; set; }
    }
}
