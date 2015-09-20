using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using d360.core;
using d360.core.entities;


namespace d360.extensions.info
{
    public class UriSecurityContextProvider : ISecurityContextProvider
    {
        public UriSecurityContextProvider()
        {
            UserIDType = UserIdentifierType.Username;
        }

        public string RawCompanyID { get; set; }
        public CompanyIdentifierType CompanyIDType { get { return CompanyIdentifierType.Uri; } }
        public string RawUserID { get; set; }
        public UserIdentifierType UserIDType { get; set; }
    }
}
