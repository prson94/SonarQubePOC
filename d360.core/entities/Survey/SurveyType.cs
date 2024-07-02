using d360.core.entities.Contracts;
using d360.core.queue;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
	[DataContract]
    public class SurveyType : BaseIntObject, IIntObject, ICreatedMetadata, IUpdatedMetadata, IEventTrackedEntity
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

		[DataMember]
		public int AssetTypeID { get; set; }

		[DataMember]
        public int ValidForDays { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Uid { get; set; }

        public DateTime? CreatedOn { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        [ForeignKey("SurveyTypeID")]
        public virtual ICollection<QuestionType> QuestionTypes { get; set; }

        [ForeignKey("SurveyTypeID")]
        public virtual ICollection<Survey> Surveys { get; set; }

		[ForeignKey("AssetTypeID"), IgnoreDataMember]
		public virtual AssetType AssetType { get; set; }

		public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.SurveyType,
                ObjectID = ID,
                ObjectType = SystemObjects.SurveyType,
                ObjectTypeID = 0
            };
        }
    }
}
