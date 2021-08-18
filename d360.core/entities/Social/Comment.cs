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
        public long AssetID { get; set; }

        [DataMember]
        public CommentType CommentType { get; set; }

        [DataMember]
        public bool IsDeleted { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        [IgnoreDataMember, ForeignKey("AssetID")]
        public virtual Asset Asset { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class CommentDetails: BaseObject
    {
        [DataMember]
        public int count { get; set; }
        [DataMember] 
        public int page { get; set; }
        [DataMember] 
        public int pageSize { get; set; }
        [DataMember] 
        public List<CommentDetail> comments { get; set; }
    }


    [DataContract]
    public class CommentDetail
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public int CreatedBy { get; set; }

        [DataMember]
        public int UpdatedBy { get; set; }

        [DataMember]
        public DateTime CreatedOn { get; set; }

        [DataMember]
        public DateTime UpdatedOn { get; set; }

        [DataMember]
        public string Body { get; set; }

        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public Guid AssetUid { get; set; }

        [DataMember]
        public Guid AssetTypeUid { get; set; }

        [DataMember]
        public CommentType CommentType { get; set; }

        [DataMember]
        public bool IsDeleted { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember, NotMapped]
        public string ResourceName { get; set; }

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
        public Guid Uid { get; set; }
        public string Body { get; set; }
        public List<Guid> Tags { get; set; }
    }

    public class CommentNotification
    {
        public string RecipientName { get; set; }
        public string RecipientEmail { get; set; }
        public string CommenterName { get; set; }
        public string Subject { get; set; }
        public string CommentUrl { get; set; }
        public string AssetUrl { get; set; }
        public long? CommentedOnAssetId { get; set; }
        public bool IsHtml { get; set; }
    }
}
