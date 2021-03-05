declare var CompanySettings;

export class CurrentCompanySettings {
    static settings: any = CompanySettings;

    static disableCommunityPosting: boolean = CurrentCompanySettings.settings.DisableCommunityPosting === 'true';
    static defaultSearchTypes: string = CurrentCompanySettings.settings.DefaultSearchTypes;
    static headerBackgroundColor = CurrentCompanySettings.settings.HeaderBackgroundColor;
    static hideData3SixtyUsers = CurrentCompanySettings.settings.HideData3SixtyUsers;    
    static companyIcon = CurrentCompanySettings.settings.CompanyIcon;
    static companyLogo = CurrentCompanySettings.settings.CompanyLogo;
    static enableOrganizations = CurrentCompanySettings.settings.EnableOrganizations;
}