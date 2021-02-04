using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;
using System.ComponentModel.DataAnnotations;
using System;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
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
        [DataMember, Key, Column(Order = 1)]
        public Emoji Emoji { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int Count { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class CommentVoteDetail : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public Emoji emoji { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public Guid resourceUid { get; set; }

        [DataMember]
        public string userDisplayName { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class CommentVoterDetail : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public Guid resourceUid { get; set; }

        [DataMember]
        public string userDisplayName { get; set; }
    }
}
