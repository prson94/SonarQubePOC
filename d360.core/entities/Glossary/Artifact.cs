using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.queue;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Artifact : BaseCreatedAndUpdatedIntObject, IIntObject, IFieldsObject, ICreatedObject, ICreatedMetadata, IUpdatedObject, ISearchable, IUpdatedMetadata, IEventTrackedEntity
    {
        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ArtifactType_Name", Description = "ArtifactType_Description")]
        public int ArtifactTypeID { get; set; }       

        [DataMember]
        public string SourceID { get; set; }


        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.Artifact,
                ObjectID = ID,
                ObjectType = SystemObjects.ArtifactType,
                ObjectTypeID = ArtifactTypeID
            };
        }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel { Type = SystemObjects.ArtifactType, Object = SystemObjects.Artifact, TypeID = ArtifactTypeID };
        }
    }
}
