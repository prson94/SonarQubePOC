using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using d360.core;
using d360.core.entities;


namespace d360.extensions.info.uri
{
    public class SecurityContextProvider : ISecurityContextProvider
    {
        public string RawCompanyID { get; set; }
        public string RawUserID { get; set; }

        public CurrentCompanyInfo GetCurrentCompanyInfo()
        {
            return new CurrentCompanyInfo { Type = CompanyIdentifierType.Uri, Identifier = RawCompanyID.Substring(0, RawCompanyID.IndexOf(".")).ToLower() };
        }

        public CurrentUserInfo GetCurrentUserInfo()
        {
            return new CurrentUserInfo { Type = UserIdentifierType.Username, Identifier = RawUserID.ToLower() };
        }
    }
}
