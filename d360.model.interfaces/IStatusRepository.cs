using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.core.entities;
using d360.core.enums;

namespace d360.model.interfaces
{
    public interface IStatusRepository : IRepository<Status, int>
    {
        List<Status> GetByType(SystemObjects type);
    }
}
