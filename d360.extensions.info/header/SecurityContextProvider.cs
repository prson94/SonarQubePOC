using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using d360.core;
using d360.core.entities;

namespace d360.extensions.info.header
{
    public class SecurityContextProvider : ISecurityContextProvider
    {
        public string RawCompanyID { get; set; }
        public string RawUserID { get; set; }

        public CurrentCompanyInfo GetCurrentCompanyInfo()
        {
            return new CurrentCompanyInfo { Type = CompanyIdentifierType.PublicID, Identifier = RawCompanyID.Split(';')[0] };
        }

        public CurrentUserInfo GetCurrentUserInfo()
        {
            return new CurrentUserInfo { Type = UserIdentifierType.ApiKey, Identifier = RawUserID.Split(';')[1] };
        }
    }
}
