using System;
using d360.core;


namespace d360.extensions.info
{
    public class UriSecurityContextProvider : ISecurityContextProvider
    {
        public UriSecurityContextProvider()
        {
            
        }

        public int CompanyID { get; set; }

        public int DomainSettingID { get; set; }

        public int ResourceID { get; set; }

        public bool IsAdministrator { get; set; }

        public string CompanyPrefix { get; set; }
    }
}
