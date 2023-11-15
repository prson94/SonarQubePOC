using System;
using System.Threading.Tasks;

using d360.core.entities;

namespace repositories
{
    public interface IResponsibilityTypeRepository
    {
        Task<ResponsibilityType> GetByUidAsync(Guid uid);
    }
}
