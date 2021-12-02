using System;
using System.Data.Entity;
using System.Threading.Tasks;
using d360.core.entities;

namespace d360.model.DataAccessLayer
{
    public interface IResponsibilityTypeRepository
    {
        Task<ResponsibilityType> GetByUidAsync(Guid uid);
    }

    internal sealed class ResponsibilityTypeRepository : IResponsibilityTypeRepository
    {
        private ICompanyContext CompanyContext { get; }

        public ResponsibilityTypeRepository(ICompanyContext companyContext)
        {
            CompanyContext = companyContext;
        }

        public Task<ResponsibilityType> GetByUidAsync(Guid uid)
        {
            return CompanyContext.Table<ResponsibilityType>().FirstOrDefaultAsync(x => x.UID == uid);
        }
    }
}
