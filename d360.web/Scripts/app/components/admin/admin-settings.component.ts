///<reference path="../../es6-shim.d.ts"/>
import {Component, NgZone} from '@angular/core';
import {Http, HTTP_PROVIDERS, Headers} from '@angular/http';
import {PageHeader} from '../../services/page-header.service'

@Component({
    selector: 'admin-settings',
    viewProviders: [HTTP_PROVIDERS],
    templateUrl: 'scripts/app/components/admin/admin-settings.component.html',
    styleUrls: ['scripts/app/components/admin/admin-settings.component.css']
})

export class AdminSettingsComponent {
    isLoading = false;
    http: Http;
    pageHeader: PageHeader;

    disableCommunityPosting: boolean;
    disableIssuePosting: boolean;
    subjectAreaName: string;
    subjectAreaNodeName: string;
    ipRestrictions = new Array<IpRestriction>();
    companyLogo = new CompanyImage();
    setLogoDefault = false;
    companyIcon = new CompanyImage();
    setIconDefault = false;
    currentLogoPath: string;
    currentIconPath: string;

    searchTypes: Array<SearchType> = [
        { title: "Attribute", value: "Attribute", selected: false },
        { title: "Fusion", value: "FusionAttributes", selected: false },
        { title: "Fusion Type", value: "FusionType", selected: false },
        { title: "Glossary", value: "Artifact", selected: false },
        { title: "Group", value: "Group", selected: false },
        { title: "Model", value: "Taxonomy", selected: false },
        { title: "Reference", value: "Domain", selected: false },
        { title: "User", value: "Users", selected: false },
    ];

    constructor(http: Http, pageHeader: PageHeader) {
        this.http = http;
        this.pageHeader = pageHeader;

        this.pageHeader.title = 'Settings';
        this.pageHeader.description = 'Manage system-wide settings for your environment.';

        this.load();
        //console.log(this);
    }


    addIpRestriction(): void {
        this.ipRestrictions.push(new IpRestriction());
    }

    removeIpRestriction(i: number): void {
        this.ipRestrictions.splice(i, 1);
    }

    onLogoFileChange(event): void {
        if (!event) {
            this.companyLogo.file = null;
            this.companyLogo.setDataUrl();
            return;
        }

        var files = event.srcElement.files;
        this.companyLogo.file = files[0];
        this.companyLogo.setDataUrl();
    }

    onIconFileChange(event): void {
        if (!event) {
            this.companyIcon.file = null;
            this.companyIcon.setDataUrl();
            return;
        }

        var files = event.srcElement.files;
        this.companyIcon.file = files[0];
        this.companyIcon.setDataUrl();
    }

    load(): void {

        this.http.get('/form/CompanySettings')
            .map(data => data.json())
            .subscribe(settings => { 
                //console.log(settings);
                this.disableCommunityPosting = settings.DisableCommunityPosting;
                this.disableIssuePosting = settings.DisableIssuePosting;
                this.subjectAreaName = settings.ArtifactType_TaxonomyTypeID;
                this.subjectAreaNodeName = settings.ArtifactType_TaxonomyTypeIDNodes;
                this.currentLogoPath = settings.CurrentCompanyLogoPath;
                this.currentIconPath = settings.CurrentCompanyIconPath;

                settings.IpRestrictions.forEach(r => this.ipRestrictions.push({ name: r.Name, start: r.Start, end: r.End }));
                settings.DefaultSearchTypes.split(',').forEach(s => {
                    var f = this.searchTypes.findIndex(f => f.value == s);
                    var t = this.searchTypes[f];
                    if (t) t.selected = true;
                });
            });
    }

    save(): void {
        this.isLoading = true;
        var headers = new Headers();
        headers.append('Content-Type', 'application/json');

        var defaultSearchTypes = "";
        var selectedSearchTypes = this.searchTypes.filter(s => s.selected).forEach(s => defaultSearchTypes += s.value + ',');


        var data = {
            DisableCommunityPosting: this.disableCommunityPosting,
            DisableIssuePosting: this.disableIssuePosting,
            CompanyLogo: this.companyLogo.dataUrl,
            SetLogoToDefault: this.setLogoDefault,
            CompanyIcon: this.companyIcon.dataUrl,
            SetIconToDefault: this.setIconDefault,
            ArtifactType_TaxonomyTypeID: this.subjectAreaName,
            ArtifactType_TaxonomyTypeIDNodes: this.subjectAreaNodeName,
            IpRestrictions: this.ipRestrictions,
            DefaultSearchTypes: defaultSearchTypes
        };

        console.log(data);
        this.http.put('/form/UpdateCompanySettings', JSON.stringify(data), { headers: headers })
            .map(data => data.json())
            .subscribe(
            data => console.log(data), //done
            err => console.log(err), //fail
            () => this.isLoading = false //always
        );

    }
}

class IpRestriction {
    name: string;
    start: string;
    end: string;
} 

class CompanyImage {
    file: File;
    isLoading = false;
    dataUrl: string;

    public setDataUrl(): void {
        this.isLoading = true;
        var fileReader = new FileReader();
        if (this.file) {
            fileReader.onloadend = (e: any) => {
                this.isLoading = false;
                this.dataUrl = fileReader.result;
            }
            fileReader.readAsDataURL(this.file);
        } else {
            this.dataUrl = "";
            this.isLoading = false;
        }
    }

}

class SearchType {
    title: string;
    value: string;
    selected: boolean = false;

    constructor(title: string, value: string) {
        this.title = title;
        this.value = value;
    }
}