using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.core.entities;
using d360.core.enums;

namespace d360.model.interfaces
{
    public interface IResourceRepository : IRepository<Resource, int>
    {
        bool IsUserFollowing(int? resourceID, SystemObjects type, int objectID);
        bool UpdateFollowStatus(int? resourceID, SystemObjects type, int objectID);
        int GetRelatedItemCount(int id);
    }

    public interface IResourceTypeRepository : IRepository<ResourceType, int>
    {
    }
}
