using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities;
using d360.core;

namespace d360.extensions
{
    public interface ISecurityContextProvider
    {
        string CompanyPrefix { get; set; }
        int CompanyID { get; set; }
        int ResourceID { get; set; }
        bool IsAdministrator { get; set; }
        //string RawCompanyID { get; set; }
        //CompanyIdentifierType CompanyIDType { get; }
        //string RawUserID { get; set; }
        //UserIdentifierType UserIDType { get; }
    }
}
