using d360.core.enums;

namespace d360.extensions
{
    public interface ISecurityContextProvider
    {
		AuthenticationType AuthenticationType { get; set; }

		bool AllowNewUserLogin { get; set; }

        string CompanyPrefix { get; set; }

		string PrimaryCompanyPrefix { get; set; }

		int ClientID { get; set; }
        
        int CompanyID { get; set; }
        
        int DomainSettingID { get; set; }
        
        int ResourceID { get; set; }
        
        bool IsAdministrator { get; set; }
	}
}
