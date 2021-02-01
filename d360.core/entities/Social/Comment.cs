using d360.core.entities.Contracts;
using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Comment : BaseCreatedAndUpdatedIntObject, IIntObject
    {
        [DataMember]
        public string Body { get; set; }

        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public Guid AssetUid { get; set; }

        [DataMember]
        public CommentType CommentType { get; set; }

        [DataMember]
        public bool IsDeleted { get; set; }

        [DataMember]
        public int? ParentID { get; set; }
    }


    [DataContract(Namespace = NAMESPACE)]
    public class CommentDetail : Comment
    {
        [DataMember, NotMapped]
        public string CreatedOnUTCString { get { return ((CreatedOn == null) ? null : ((DateTime)UpdatedOn).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")); } }

        [DataMember, NotMapped]
        public string UpdatedOnUTCString { get { return ((UpdatedOn == null) ? null : ((DateTime)UpdatedOn).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")); } }

        [DataMember, NotMapped]
        public string ResourceName { get; set; }

        [DataMember, NotMapped]
        public bool CreatorIsOwner { get; set; }

        [DataMember]
        public string AssetPath { get; set; }

        [DataMember]
        public string Url { get; set; }

        [IgnoreDataMember]
        public string TagsJson { get; set; }

        [IgnoreDataMember]
        public string EmojisJson { get; set; }

        [DataMember]
        public List<CommentRelationDetail> Tags { get { return JsonConvert.DeserializeObject<List<CommentRelationDetail>>(TagsJson); } }

        [DataMember]
        public List<CommentAggregateVoteDetail> Emojis { get { return JsonConvert.DeserializeObject<List<CommentAggregateVoteDetail>>(EmojisJson); } }

        [DataMember, NotMapped]
        public ICollection<CommentDetail> Comments { get; set; }
    }

    public interface IApiComment
    {
        string Body { get; set; }
        List<Guid> Tags { get; set; }
    }

    public class CommentApiPostModel: IApiComment
    {
        public Guid AssetUid { get; set; }
        public Guid? ParentUid { get; set; }
        public string Body { get; set; }
        public List<Guid> Tags { get; set; }
    }

    public class CommentApiPutModel: IApiComment
    {
        public string Body { get; set; }
        public List<Guid> Tags { get; set; }
    }
}
