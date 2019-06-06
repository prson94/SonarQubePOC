using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities;

namespace d360.model.DataAccessLayer
{
   public interface IRelationshipRepository
    {
        IntersectType GetRelationshipByUID(Guid relationshipTypUid);
    }
}
