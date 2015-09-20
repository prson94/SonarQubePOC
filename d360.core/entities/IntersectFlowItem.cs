using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectFlowItem : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        public int IntersectFlowID { get; set; }

        [DataMember]
        public int IntersectID { get; set; }

        [DataMember]
        public int FromIntersectNodeID { get; set; }

        [DataMember]
        public int ToIntersectNodeID { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        #endregion

        #region Navigation Properties

        [ForeignKey("IntersectFlowID"), IgnoreDataMember]
        public IntersectFlow IntersectFlow { get; set; }

        [ForeignKey("IntersectID"), IgnoreDataMember]
        public Intersect Intersect { get; set; }

        [ForeignKey("FromIntersectNodeID"), IgnoreDataMember]
        public IntersectNode FromIntersectNode { get; set; }

        [ForeignKey("ToIntersectNodeID"), IgnoreDataMember]
        public IntersectNode ToIntersectNode { get; set; }

        #endregion


    }
}
