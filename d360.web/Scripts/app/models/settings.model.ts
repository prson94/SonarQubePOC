import { SiteNav } from './site-menu.model';
import { Observable } from 'rxjs';
import { StringConstants } from '../static/string-constants';

export interface ICompanySettingsService {
    getSettings(): Observable<CompanySettings>;
    putSettings(companySettings: CompanySettings): Observable<any>;
}

export class CompanySettings {
    DisableCommunityPosting: boolean;
    DisableIssueManagement: boolean;
    SubjectAreaNodeName: string;
    IpRestrictions = new Array<IpRestriction>();
    SiteNav = new Array<SiteNav>();
    CompanyLogo: string;
    SetLogoToDefault = false;
    CompanyIcon: string;
    SetIconToDefault = false;
    CurrentLogoPath: string;
    CurrentIconPath: string;
    DefaultSearchTypes: string;
    EnableOrganizations: boolean;
    EnableShoppingCart: boolean;
    HideData3SixtyUsers: boolean;
    ShowAllUsersAPIKey: boolean;
    DefaultRoute: string;
    EnableSearchExactMatch: boolean;
    WorkflowCatchAllGroup: number;
    ShowHomeAssignmentTile: boolean;
    ShowHomeBoardTile: boolean;
    ShowHomeActivityTile: boolean;
    ShowHomePageTitle: boolean;
    HomePageTitleSize: string;
    HomePageTitleColor: string;
    HomePageBackgroundImage: string;
    ClearHomePageBackgroundImage: boolean = false;
    BrowserTitlePrefix: string;
    WorkflowDigestEmailDays: number = 0;
    MaxDropdownItems: number;
    WriteActionDescription: boolean;
    CurrentCompanyLogoPath: string;
    LineageVersion: number;
    MaxExcelExportRows: number;
    AllowedOrigins: string;
    FramingDomains: string;
}

export class IpRestriction {
    Name: string;
    Start: string;
    End: string;
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

export enum CompanySettingEnum {
    GovernanceRoleReferenceListUid = 73
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

export class Value {
    Name: string;
    Start: string;
    End: string;
}

export class IpAddressSetting {
    Value: Value[];
}

export class GuidSetting {
    Value: string;
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
            new SearchType("Fusion", "FusionAttributes"),
            new SearchType("Fusion Types", "FusionType"),
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

