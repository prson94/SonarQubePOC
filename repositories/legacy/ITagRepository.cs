
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using d360.core.entities;

namespace repositories
{
    public interface ITagRepository
    {
        Task<dynamic> GetTagsForExcel(IEnumerable<KeyValuePair<string, string>> queryParams);

		string CheckTagAssetbyUids(List<Guid> uids);

		bool DoesTagExists(string value, Guid? tagTypeUid);
        
        bool DoesTagExists(Guid tagUid);

		Tag GetTagByUid(Guid uid);
        
        Tag GetTagByName(string name);
        
        Tag GetTagById(int tagId);
        
        bool DoesAssetTagExists(int tagId, long assetId);
        
        int? GetAssetTagDetails(int tagId, long assetId);
        
        bool IsAuthorizedToDeleteAssetTag(int tagId, long assetId);
        
        bool IsAuthorizedToEditTag(Guid tagUid);
        
        AssetTag GetAssetTag(int tagId, long assetId);
        
        TagDetailApiModel GetDetails(Guid tagUid, IEnumerable<KeyValuePair<string, string>> keyValuePairs);

        IEnumerable<dynamic> GetTooltip(Guid tagUid, Guid? assetUid);
        
        IEnumerable<Tag> GetTagsForAsset(long assetId, int? tagTypeId);

		Task BulkTagAssets(IEnumerable<BulkTagAsset> tags, int resourceId);

		TagType GetTagTypeByUid(Guid? uid);

		bool DoesTagTypeExists(Guid uid);
		Tag GetTagDetailIfExists(string value, Guid? tagTypeUid);
	}
}
