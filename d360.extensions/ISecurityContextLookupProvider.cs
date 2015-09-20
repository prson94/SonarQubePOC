using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities;

namespace d360.extensions
{
    public interface ISecurityContextLookupProvider
    {
        string ConnectionString { get; }

        Company GetCompany();
        int GetCompanyID();

        Resource GetResource();
        int GetResourceID();

        bool GetResourceAdminFlag();
    }
}
