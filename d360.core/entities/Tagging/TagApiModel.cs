using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class TagApiModel
    {

        [DataMember]
        public Guid uid { get; set; }
        [DataMember, StringLength(250)]
        public string Value { get; set; }
        [DataMember]
        public int UseCount { get; set; }
        [DataMember]
        public Guid? CreatedByUid { get; set; }
        [DataMember]
        public DateTime CreatedOn { get; set; }

        [DataMember]
        public Guid? UpdatedByUid { get; set; }
        [DataMember]
        public DateTime UpdatedOn { get; set; }
    }

    public class TagApiUpsertModel
    {
        public string Value { get; set; }
    }

    public class AssetTagApiModel
    {
        [DataMember]
        public Guid AssetUID { get; set; }
        [DataMember]
        public Guid TagUID { get; set; }
        [DataMember]
        public string TagName { get; set; }

    }

    public class AssetTagSuccessApiModel
    {
        [DataMember]
        public Guid? Uid { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public bool Success { get; set; }
    }


    public class TagApiDeleteModel
    {
        [DataMember]
        public Guid uid { get; set; }
        [DataMember]
        public bool cascade { get; set; }
    }

    public class TagStatusModel
    {
        [DataMember]
        public bool IsTaggingEnabled { get; set; }
    }

    public class AssetTagList
    {
        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public string Breadcrumbs { get; set; }

        [DataMember]
        public string Url { get; set; }
    }


    public class TagDetailApiModel
    {
        public int pageSize { get; set; }
        public int pageNum { get; set; }
        public int? total { get; set; }
        public List<TagDetail> items { get; set; } = new List<TagDetail>();
    }

    public class TagDetail
    {
        public string DisplayValue { get; set; }
        public int AssetID { get; set; }
        public Guid AssetUid { get; set; }
        public Guid AssetTypeUid { get; set; }
        public string AssetType { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public List<TagDetailItem> Tags { get; set; } = new List<TagDetailItem>();
    }

    public class TagDetailItem
    {
        public Guid uid { get; set; }
        public string Value { get; set; }
    }

    public class TagPermissionItem
    {
        public Guid uid { get; set; }
        public string Value { get; set; }
        public bool CanDelete { get; set; }
    }

}
