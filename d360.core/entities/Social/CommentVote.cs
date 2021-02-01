using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), NotMapped]
    public class CommentVote : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public int CommentID { get; set; }

        [DataMember]
        public int ResourceID { get; set; }

        [DataMember]
        public Emoji Emoji { get; set; }

        #endregion

    }

    [DataContract(Namespace = NAMESPACE)]
    public class CommentAggregateVoteDetail : BaseObject
    {
        [DataMember]
        public Emoji Emoji { get; set; }

        [DataMember]
        public int Count { get; set; }
    }
}
