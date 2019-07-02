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

        public Intersect GetRelationshipByUID(Guid relationshipUid)
        {
            return this.companyContext.Filter<Intersect>(i => i.uid == relationshipUid).SingleOrDefault();
        }

        public IntersectType GetRelationshipTypeByUID(Guid relationshipTypUid)
        {
            return this.companyContext.Filter<IntersectType>(i => i.uid == relationshipTypUid).SingleOrDefault();
        }
    }
}
