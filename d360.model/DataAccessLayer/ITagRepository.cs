
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using d360.core.entities;

namespace d360.model.DataAccessLayer
{
    public interface ITagRepository
    {
        Task<TagApiModelWrapper> GetTags(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        Task<dynamic> GetTagsForExcel(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool DeleteTags(List<TagApiDeleteModel> models);
        
        TagApiModel CreateTag(TagApiUpsertModel model);
        
        TagApiModel UpdateTag(Guid uid, TagApiUpsertModel model, Tag tag);
        
        bool DoesTagExists(string value);
        
        bool DoesTagExists(Guid tagUid);
        
        bool DoesTagExists(Guid tagUid, TagApiUpsertModel model);
        
        Tag GetTagByUid(Guid uid);
        
        Tag GetTagByName(string name);
        
        Tag GetTagById(int tagId);
        
        bool DoesAssetTagExists(int tagId, long assetId);
        
        int? GetAssetTagDetails(int tagId, long assetId);
        
        AssetTag CreateAssetTag(int tagId, long assetId);
        
        bool IsAuthorizedToDeleteAssetTag(int tagId, long assetId);
        
        bool IsAuthorizedToEditTag(Guid tagUid);
        
        AssetTag GetAssetTag(int tagId, long assetId);
        
        bool DeleteAssetTag(int tagId, long assetId);
        
        List<AssetTagList> GetAssetsPathForTag(Guid tagUid);
        
        IEnumerable<TagApiModel> ConsolidateTags(string parentUid, List<string> childrenUids);
        
        List<dynamic> SearchTags(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        TagDetailApiModel GetDetails(Guid tagUid, IEnumerable<KeyValuePair<string, string>> keyValuePairs);

        IEnumerable<dynamic> GetTooltip(Guid tagUid, Guid? assetUid);
        
        IEnumerable<Tag> GetTagsForAsset(long assetId);
    }
}
