using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.core.entities;

namespace d360.model.interfaces
{
    public interface IGroupRepository : IRepository<Group, int>
    {
        IQueryable<Resource> GetUsers(int id);
    }
}
