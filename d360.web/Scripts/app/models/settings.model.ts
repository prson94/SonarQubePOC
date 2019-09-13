import { SiteNav } from './site-menu.model';
import { Observable } from 'rxjs';

export interface ICompanySettingsService {
    getSettings(): Observable<CompanySettings>;
    putSettings(companySettings: CompanySettings): Observable<any>;
}

export class CompanySettings {
    DisableCommunityPosting: boolean;
    DisableIssuePosting: boolean;
    DisableIssueManagement: boolean;
    ArtifactType_TaxonomyTypeID: string;
    ArtifactType_TaxonomyTypeIDNodes: string;
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
    ShowAllUsersAPIKey:boolean
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
                var contents = e.target.result,
                    error = e.target.error;
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
            { title: "Attribute", value: "Attribute", selected: false },
            { title: "Fusion", value: "FusionAttributes", selected: false },
            { title: "Fusion Type", value: "FusionType", selected: false },
            { title: "Glossary", value: "Artifact", selected: false },
            { title: "Group", value: "Group", selected: false },
            { title: "Model", value: "Taxonomy", selected: false },
            { title: "Policy", value: "Policy", selected: false },
            { title: "Reference", value: "Reference", selected: false },
            { title: "User", value: "Resource", selected: false },
            { title: "Grammatic Type", value: "Synonym", selected: false },
            { title: "Data Quality", value: "Rule", selected: false }
        ];
    }

    export function searchTypeListToString(list: SearchType[]): string {
        return list.filter(l => l.selected).map(l=> l.value).join(',');
    }

    export function searchTypeStringToList(searchTypes: string): SearchType[] {
        let t = getSearchTypesList();
        searchTypes.split(',').forEach(i =>
        {
            let k = t.find(j => j.value == i);
            if (k)
                k.selected = true
        });
        return t;
    }

}
