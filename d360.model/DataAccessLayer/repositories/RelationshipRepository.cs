using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities;

namespace d360.model.DataAccessLayer { 
    public class RelationshipRepository : IRelationshipRepository
    {
        ICompanyContext companyContext;
        public RelationshipRepository(ICompanyContext companyContext)
        {
            this.companyContext = companyContext;
        }
        public IntersectType GetRelationshipByUID(Guid relationshipTypUid)
        {
            return this.companyContext.Filter<IntersectType>(i => i.uid == relationshipTypUid).SingleOrDefault();
        }
    }
}
