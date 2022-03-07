namespace d360.extensions
{
    public interface ISecurityContextProvider
    {
        string CompanyPrefix { get; set; }
        
        int ClientID { get; set; }
        
        int CompanyID { get; set; }
        
        int DomainSettingID { get; set; }
        
        int ResourceID { get; set; }
        
        bool IsAdministrator { get; set; }
    }
}
