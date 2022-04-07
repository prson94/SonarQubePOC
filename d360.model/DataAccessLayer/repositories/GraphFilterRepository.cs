using System;
using System.Collections.Generic;
using System.Linq;

using d360.core.entities.Graph;

namespace d360.model.DataAccessLayer
{
    public class GraphFilterRepository : IGraphFilterRepository
    {
        internal ICompanyContext Company;

        public GraphFilterRepository(ICompanyContext context)
        {
            Company = context;
        }

        public List<GraphFilter> GetGraphFiltersByUser(int ownerId)
        {
            return Company.GraphFilters.Where(f => f.OwnedBy == ownerId).ToList();
        }

        public GraphFilter GetGraphFilterByUid(Guid uid)
        {
            return Company.GraphFilters.Where(i => i.Uid == uid).SingleOrDefault();
        }

        public bool DeleteGraphFilter(GraphFilter model)
        {
            return Company.Delete(model);
        }

        public bool CreateGraphFilter(GraphFilter model)
        {
            if (model.OwnedBy == 0)
            {
                model.OwnedBy = Company.CurrentResourceID;
            }

            model.Uid = Guid.NewGuid();
            return Company.Add(model);
        }

        public bool UpdateGraphFilter(GraphFilter model)
        {
            if (model.OwnedBy == 0)
            {
                model.OwnedBy = Company.CurrentResourceID;
            }

            if (model.IsDefault)
            {
                Company.Execute($@"UPDATE [graph].[Filter] SET IsDefault = 0 WHERE IsDefault = 1 AND OwnedBy = @OwnedBy", new { OwnedBy = Company.CurrentResourceID });
            }

            return Company.Update(model);
        }
    }
}
