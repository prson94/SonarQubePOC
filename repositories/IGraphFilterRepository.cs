using d360.core.entities.Graph;
using System;
using System.Collections.Generic;

namespace repositories
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
