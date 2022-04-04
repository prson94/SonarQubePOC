using System;
using System.Collections.Generic;

using d360.core.entities.Graph;

namespace d360.model.DataAccessLayer
{
    public interface IGraphFilterRepository
    {
        List<GraphFilter> GetGraphFiltersByUser(int ownerId);
        
        GraphFilter GetGraphFilterByUid(Guid uid);
        
        bool DeleteGraphFilter(GraphFilter model);
        
        bool CreateGraphFilter(GraphFilter model);
        
        bool UpdateGraphFilter(GraphFilter model);
    }
}
