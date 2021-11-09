import { SiteNav } from './site-menu.model';
import { Observable } from 'rxjs';
import { StringConstants } from '../static/string-constants';

export class CompanySettings {
    AllowedOrigins: string;
    AssetDefinitionColumnWidth: number;
    BrowserTitlePrefix: string;
    ClearHomePageBackgroundImage: boolean = false;
    CompanyIcon: string;
    CompanyLogo: string;
    CurrentIconPath: string;
    CurrentLogoPath: string;
    DefaultIconPath: string = "/favicon.ico";
    DefaultLogoPath: string = "/Content/images/PreciselyLogo@2x.png";
    DefaultRoute: string;
    DefaultSearchTypes: string;
    DiagramMaxAvoidNodesLinkCount: number;
    DisableCommunityPosting: boolean;
    DisableIssueManagement: boolean;
    EnableOrganizations: boolean;
    EnableSearchExactMatch: boolean;
    EnableShoppingCart: boolean;
    FramingDomains: string;
    HideData3SixtyUsers: boolean;
    HideHeaderBarControls: boolean;
    HomePageBackgroundImage: string;
    HomePageTitleColor: string;
    HomePageTitleSize: string;
    IpRestrictions = new Array<IpRestriction>();
    MaxDropdownItems: number;
    MaxExcelExportRows: number;
    SetIconToDefault = false;
    SetLogoToDefault = false;
    ShowAllUsersAPIKey: boolean;
    ShowHomeActivityTile: boolean;
    ShowHomeAssignmentTile: boolean;
    ShowHomeBoardTile: boolean;
    ShowHomePageTitle: boolean;
    SiteNav = new Array<SiteNav>();
    SubjectAreaNodeName: string;
    WorkflowCatchAllGroup: number;
    WorkflowDigestEmailDays: number = 0;
    WriteActionDescription: boolean;
}

export class CompanyImage {
    file: File;
    isLoading = false;
    dataUrl: any;

    public setDataUrl(): void {
        this.isLoading = true;
        var fileReader = new FileReader();
        if (this.file) {
            fileReader.onloadend = (e: any) => {
                this.isLoading = false;
                this.dataUrl = e.target.result;
            }
            fileReader.readAsDataURL(this.file);
        } else {
            this.dataUrl = null;
            this.isLoading = false;
        }
    }

}

export enum CompanyRebuildJobToken {
    AssetGraph = 1,
    DisplayValues = 2,
    SearchIndex = 3
}

export enum CompanyRebuildJobStatusState {
    Active = 1,
    Inactive = 2
}

export class CompanyRebuildJobStatusApiModel {
    jobToken: CompanyRebuildJobToken;
    jobTokenName: string;
    jobTokenDescription: string;
    state: CompanyRebuildJobStatusState;
    lastStartedOn: Date;
    lastCompletedOn: Date;
    validationMessage: string;
}

export class AppSettingsEnum {
    static HelpBaseUri: string = "HelpBaseUri";
    static AppInsightsKey: string = "AppInsightsInstrumentationKey";
}

export enum CompanySettingEnum {
    DisableCommunityPosting = 1,
    CompanyLogo = 2,
    CompanyIcon = 3,
    IpRestriction = 4,
    HideData3SixtyUsers = 9,
    HeaderBackgroundColor = 10,
    DefaultSearchTypes = 13,
    DisableIssueManagement = 17,
    EnableOrganizations = 19,
    EnableShoppingCart = 20,
    EnableSagacity = 21,
    DefaultRoute = 22,
    SearchExactMatch = 23,
    CustomCSSLocation = 24,
    AzureADTenant = 25,
    AzureGraphAPIKey = 26,
    AzureApplicationId = 27,
    ShowResources = 28,
    ShowFollowersSidebar = 29,
    ShowOwnersSidebar = 30,
    ShowImpactSidebar = 31,
    ShowLineageSidebar = 32,
    BrowserTitlePrefix = 33,
    SessionTimeout = 34,
    ShowFavorites = 37,
    ShowSocialScoreBar = 38,
    ShowHomeAssignmentTile = 39,
    ShowHomeBoardTile = 40,
    ShowHomeActivityTile = 41,
    ShowHomePageTitle = 42,
    HomePageTitleSize = 43,
    HomePageTitleColor = 44,
    HomePageBackgroundImage = 45,
    ActionMessage = 47,
    WorkflowFromName = 48,
    WorkflowFromEmail = 49,
    ShowCustomAPIAdmin = 50,
    HasRegisterLink = 52,
    JwtAuthority = 54,
    PowerBIClientId = 55,
    PowerBIGroupId = 56,
    ShowAllUsersAPIKey = 57,
    WorkflowCatchAllGroup = 58,
    MaxDropdownItems = 60,
    WriteActionDescription = 61,
    UseNewMarkitLineageGeneration = 62,
    RequestCertificationDraft = 64,
    UseAsTransformationLimit = 69,
    MaxExcelExportRows = 71,
    ShowNavigationChildren = 72,
    GovernanceRoleReferenceListUid = 73,
    ApiTimeout = 74,
    EnableJsonAttribute = 75,
    AllowedOrigins = 76,
    FramingDomains = 77,
    WorkflowDigestEmailDays = 78,
    ShowChangeLogTab = 79,
    ShowCommentsTab = 80,
    AssetDataProfileLifespan = 81,
    AssetDefinitionColumnWidth = 82,
    HideHeaderBarControls = 83,
    DiagramMaxAvoidNodesLinkCount = 84
}

export class StringSetting {
    Value: string;
}

export class NumberSetting {
    Value: number;
}

export class BooleanSetting {
    Value: boolean;
}

export class IpRestriction {
    Name: string;
    Start: string;
    End: string;
}

export class IpAddressSetting {
    Value: IpRestriction[];
}

export class GuidSetting {
    Value: string;
}

export class AppSettingModel {
    Name: string;
    Value: any;
}

export class SettingsGetModel {
    SettingID: number;
    Locked: boolean;
    Name: string;
    Description: string;
    StringSetting: StringSetting;
    NumberSetting: NumberSetting;
    BooleanSetting: BooleanSetting;
    IpAddressSetting: IpAddressSetting;
    GuidSetting: GuidSetting;

    ScalarValue: any; //populated when pulled down via initializer
}

export class SettingsPutModel {
    SettingID: number;
    StringSetting: StringSetting;
    NumberSetting: NumberSetting;
    BooleanSetting: BooleanSetting;
    IpAddressSetting: IpAddressSetting;
    GuidSetting: GuidSetting;
}

export class SearchType {
    title: string;
    value: string;
    selected: boolean = false;
    visible: boolean = true;

    constructor(title: string, value: string) {
        this.title = title;
        this.value = value;
    }
}

export module SettingsHelper {
    export function getSearchTypesList(): SearchType[] {
        return [
            new SearchType(StringConstants.AssetTypeClass_Business + "s", "BusinessAsset"),
            new SearchType(StringConstants.AssetTypeClass_Technical + "s", "TechnicalAsset"),
            new SearchType("Diagram Assets", "Diagram"),
            new SearchType("Models", "Model"),
            new SearchType("Policies", "Policy"),
            new SearchType("Rules", "Rule"),
            new SearchType("Reference Lists", "Reference"),
            new SearchType("Grammatic Types", "Synonym"),
            new SearchType("Groups", "Group"),
            new SearchType("Users", "User"),
        ];
    }

    export function searchTypeListToString(list: SearchType[]): string {
        return list.filter(l => l.selected).map(l => l.value).join(',');
    }

    export function searchTypeStringToList(searchTypes: string, list: SearchType[] = undefined): SearchType[] {
        let t = (list === undefined) ? getSearchTypesList() : list;
        searchTypes.split(',').forEach(i => {
            let k = t.find(j => j.value == i);
            if (k)
                k.selected = true
        });
        return t;
    }

}

