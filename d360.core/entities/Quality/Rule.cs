using d360.core.entities.Contracts;
using d360.core.queue;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class RuleModel
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string SourceID { get; set; }

        [DataMember]
        public Dictionary<string, string> Fields { get; set; }

    }


    [DataContract(Namespace = NAMESPACE)]
    public class Rule : BaseCreatedAndUpdatedIntObject, IIntObject, IFieldsObject, ICreatedObject, IUpdatedObject, ICreatedMetadata, IUpdatedMetadata, IEventTrackedEntity
    {
        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string KeyHash { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string FieldHash { get; set; }

        [DataMember]
        public int RuleTypeID { get; set; }

        [DataMember]
        public string SourceID { get; set; }

        [DataMember, ForeignKey("RuleTypeID")]
        public RuleType RuleType { get; set; }
                
        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.Rule,
                ObjectID = ID,
                ObjectType = SystemObjects.RuleType,
                ObjectTypeID = RuleTypeID
            };
        }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel { Type = SystemObjects.RuleType, Object = SystemObjects.Rule, TypeID = RuleTypeID };
        }
    }
}
