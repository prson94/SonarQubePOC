using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities;
using d360.core.entities.Graph;
using d360.core.enums;

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


