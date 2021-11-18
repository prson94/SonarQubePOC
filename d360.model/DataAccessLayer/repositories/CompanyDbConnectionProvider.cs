using System.Data;

namespace d360.model.DataAccessLayer.repositories
{
    internal sealed class CompanyDbConnectionProvider : ICompanyDbConnectionProvider
    {
        private ICompanyContext CompanyContext { get; }

        public CompanyDbConnectionProvider(ICompanyContext companyContext)
        {
            CompanyContext = companyContext;
        }

        public IDbConnection Connection => CompanyContext.Database.Connection;
    }
}