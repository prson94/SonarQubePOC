declare var CompanySettings;

export class CurrentCompanySettings {
    static settings: any = CompanySettings;

    static disableCommunityPosting: boolean = CurrentCompanySettings.settings.DisableCommunityPosting === 'true';
    static defaultSearchTypes: string = CurrentCompanySettings.settings.DefaultSearchTypes;
    static headerBackgroundColor = CurrentCompanySettings.settings.HeaderBackgroundColor;
    static headerProfileLinkColor = CurrentCompanySettings.settings.HeaderProfileLinkColor;
    static hideData3SixtyUsers = CurrentCompanySettings.settings.HideData3SixtyUsers;
    static artifactType_TaxonomyTypeID = CurrentCompanySettings.settings.ArtifactType_TaxonomyTypeID;
    static artifactType_TaxonomyTypeIDNodes = CurrentCompanySettings.settings.ArtifactType_TaxonomyTypeIDNodes;
    static companyIcon = CurrentCompanySettings.settings.CompanyIcon;
    static companyLogo = CurrentCompanySettings.settings.CompanyLogo;
    static enableOrganizations = CurrentCompanySettings.settings.EnableOrganizations;
}