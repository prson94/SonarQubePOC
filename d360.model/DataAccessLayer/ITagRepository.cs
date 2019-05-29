
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
        TagApiModel UpdateTag(Guid uid, TagApiModel model);
    }
}
