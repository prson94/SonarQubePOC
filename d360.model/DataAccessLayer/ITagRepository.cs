
using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface ITagRepository
    {
        Task<TagApiModelWrapper> GetTags(IEnumerable<KeyValuePair<string, string>> queryParams);
        bool DeleteTag(Guid uid);
        TagApiModel CreateTag(TagApiModel model);
        TagApiModel UpdateTag(Guid uid, TagApiModel model, Tag tag);
        bool DoesTagExists(string value);
        bool DoesTagExists(TagApiModel model);
        Tag GetTagByUid(Guid uid);

        bool DoesAssetTagExists(int tagId, long assetId);
        AssetTag CreateAssetTag(int tagId, long assetId);
        bool IsAuthorizedToDeleteAssetTag(int tagId, long assetId);
        AssetTag GetAssetTag(int tagId, long assetId);
        bool DeleteAssetTag(int tagId, long assetId);
        TagDetailApiModel GetDetails(Guid tagUid, IEnumerable<KeyValuePair<string,string>> keyValuePairs);
    }
}
