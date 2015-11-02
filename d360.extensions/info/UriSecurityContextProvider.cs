using System;
using d360.core;


namespace d360.extensions.info
{
    public class UriSecurityContextProvider : ISecurityContextProvider
    {
        public UriSecurityContextProvider()
        {
            //UserIDType = UserIdentifierType.ID;
        }

        //public string RawCompanyID { get; set; }
        //public CompanyIdentifierType CompanyIDType { get { return CompanyIdentifierType.ID; } }
        //public string RawUserID { get; set; }
        //public UserIdentifierType UserIDType { get; set; }

        public int CompanyID
        {
            get; set;
        }

        public int ResourceID
        {
            get; set;
        }

        public bool IsAdministrator
        {
            get; set;
        }

        public string CompanyPrefix
        {
            get;

            set;
        }
    }
}
