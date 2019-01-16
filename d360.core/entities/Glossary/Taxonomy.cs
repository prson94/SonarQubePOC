using System;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.queue;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Taxonomy : BaseIntObject, IIntObject, IFieldsObject, ISearchable, IUpdatedMetadata, IEventTrackedEntity
    {
                
        [DataMember, ReadOnly(true), DatabaseGenerated(DatabaseGeneratedOption.Computed), Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Path_Name", Description = "Path_Description")]
        public string TextPath { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public int TaxonomyTypeID { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Level_Name", Description = "Level_Description")]
        public int? Level { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [DataMember]
        public string SourceID { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string KeyHash { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string FieldHash { get; set; }

        #region Navigation Properties

        [IgnoreDataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public virtual TaxonomyType TaxonomyType { get; set; }

        #endregion

        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.Taxonomy,
                ObjectID = ID,
                ObjectType = SystemObjects.TaxonomyType,
                ObjectTypeID = TaxonomyTypeID
            };
        }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel { Type = SystemObjects.TaxonomyType, Object = SystemObjects.Taxonomy, TypeID = TaxonomyTypeID };
        }
    }
}
