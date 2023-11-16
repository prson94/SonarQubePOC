using d360.core.entities;
using repositories;
using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer.repositories
{
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
