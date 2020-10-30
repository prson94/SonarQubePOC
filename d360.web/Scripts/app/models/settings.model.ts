import { SiteNav } from './site-menu.model';
import { Observable } from 'rxjs';
import { StringConstants } from '../static/string-constants';

export interface ICompanySettingsService {
    getSettings(): Observable<CompanySettings>;
    putSettings(companySettings: CompanySettings): Observable<any>;
}

export class CompanySettings {
    DisableCommunityPosting: boolean;
    DisableIssuePosting: boolean;
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
    ShowDefaultHelpVideos: boolean;
    ShowHomeAssignmentTile: boolean;
    ShowHomeBoardTile: boolean;
    ShowHomeActivityTile: boolean;
    ShowHomePageTitle: boolean;
    HomePageTitleSize: string;
    HomePageTitleColor: string;
    HomePageBackgroundImage: string;
    ClearHomePageBackgroundImage: boolean = false;
    BrowserTitlePrefix: string;
    WorkflowDigestEmailEnabled: boolean = false;
    MaxDropdownItems: number;
    WriteActionDescription: boolean;
    CurrentCompanyLogoPath: string;
    LineageVersion: number;
    FusionEnabled: boolean = true;
    MaxExcelExportRows: number;
    AllowedOrigins: string;
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

export class SettingsPutModel {
    SettingID: number;
    StringSetting: StringSetting;
    NumberSetting: NumberSetting;
    BooleanSetting: BooleanSetting;
    IpAddressSetting: IpAddressSetting;
}


export class SearchType {
    title: string;
    value: string;
    selected: boolean = false;

    constructor(title: string, value: string) {
        this.title = title;
        this.value = value;
    }

}

export module SettingsHelper {
    export function getSearchTypesList(): SearchType[] {
        return [
            { title: StringConstants.AssetTypeClass_Business + "s", value: "BusinessAsset", selected: false },
            { title: StringConstants.AssetTypeClass_Technical + "s", value: "TechnicalAsset", selected: false },
            { title: "Models", value: "Taxonomy", selected: false },
            { title: "Policies", value: "Policy", selected: false },
            { title: "Rules", value: "Rule", selected: false },
            { title: "Reference Lists", value: "Reference", selected: false },
            { title: "Grammatic Types", value: "Synonym", selected: false },
            { title: "Fusion", value: "FusionAttributes", selected: false },
            { title: "Fusion Types", value: "FusionType", selected: false },
            { title: "Groups", value: "Group", selected: false },
            { title: "Users", value: "Resource", selected: false }
        ];
    }

    export function searchTypeListToString(list: SearchType[]): string {
        return list.filter(l => l.selected).map(l => l.value).join(',');
    }

    export function searchTypeStringToList(searchTypes: string): SearchType[] {
        let t = getSearchTypesList();
        searchTypes.split(',').forEach(i => {
            let k = t.find(j => j.value == i);
            if (k)
                k.selected = true
        });
        return t;
    }

}

