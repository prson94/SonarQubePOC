
using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface ITagRepository
    {
        Task<TagApiModelWrapper> GetTags(IEnumerable<KeyValuePair<string, string>> queryParams);
        bool DeleteTag(Guid uid);
        bool DeleteTags(List<TagApiDeleteModel> models);
        TagApiModel CreateTag(TagApiModel model);
        TagApiModel UpdateTag(Guid uid, TagApiModel model, Tag tag);
        bool DoesTagExists(string value);
        bool DoesTagExists(TagApiModel model);
        Tag GetTagByUid(Guid uid);
        List<dynamic> GetAssetsPathForTag(Guid tagUid);
        IEnumerable<TagApiModel> ConsolidateTags(string parentUid, List<string> childrenUids);
        List<dynamic> SearchTags(string tag);
    }
}
