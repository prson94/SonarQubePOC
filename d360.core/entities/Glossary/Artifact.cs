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
        public Artifact()
        {
            Visible = true;
        }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ArtifactType_Name", Description = "ArtifactType_Description")]
        public int ArtifactTypeID { get; set; }
               
        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Parent_Name", Description = "Parent_Description")]
        public int? ParentID { get; set; }

        [DataMember]
        public string SourceID { get; set; }

        public bool Visible { get; set; }

        [IgnoreDataMember]
        public virtual ArtifactType ArtifactType { get; set; }

        [IgnoreDataMember]
        public virtual Artifact Parent { get; set; }

        [ForeignKey("ParentID"), IgnoreDataMember]
        public virtual ICollection<Artifact> Children { get; set; }

        [IgnoreDataMember]
        public virtual ICollection<Fusion> OwnedFusions { get; set; }

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
