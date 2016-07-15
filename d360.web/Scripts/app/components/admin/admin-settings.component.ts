///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { PageHeader } from '../../services/page-header.service'
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ICompanySettingsService, CompanySettings, IpRestriction, CompanyImage, SearchType, SettingsHelper } from '../../models/settings.model';
import { CompanySettingsService } from '../../services/settings.service';
import { AdminBaseComponent } from './admin-base.component';
import { Title } from '@angular/platform-browser';
import { TileActionsComponent } from '../tiles/tile-actions.component';

@Component({
    selector: 'admin-settings',
    providers: [CompanySettingsService],
    directives: [TileActionsComponent],
    templateUrl: 'scripts/app/components/admin/admin-settings.component.html',
    styleUrls: ['scripts/app/components/admin/admin-settings.component.css']
})

export class AdminSettingsComponent extends AdminBaseComponent {
    
    companySettings: CompanySettings = new CompanySettings();
    searchTypes: SearchType[] = SettingsHelper.getSearchTypesList();
    companyLogo: CompanyImage = new CompanyImage();
    companyIcon: CompanyImage = new CompanyImage();

    constructor(pageHeader: PageHeader, headerBreadcrumbService: HeaderBreadcrumbService, private companySettingsService: CompanySettingsService, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService);
        this.areaDescription = "Manage system-wide settings for your environment.";
        this.areaName = "Settings";
        this.setCommonItems();

        this.load();
    }


    addIpRestriction(): void {
        this.companySettings.IpRestrictions.push(new IpRestriction());
    }

    removeIpRestriction(i: number): void {
        this.companySettings.IpRestrictions.splice(i, 1);
    }

    onLogoFileChange(event): void {
        if (this.companyLogo == null)
            this.companyLogo = new CompanyImage();

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
        if (this.companyIcon == null)
            this.companyIcon = new CompanyImage();
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
        this.isLoading = true;
        this.companySettingsService.getSettings()
            .then(data => {
                this.companyLogo = new CompanyImage();
                this.companyIcon = new CompanyImage();

                this.companySettings = data;
                this.searchTypes = SettingsHelper.searchTypeStringToList(this.companySettings.DefaultSearchTypes);
                console.log(this.companySettings);
                this.isLoading = false;
            });
    }

    save(): void {
        this.isLoading = true;
        this.companySettings.DefaultSearchTypes = SettingsHelper.searchTypeListToString(this.searchTypes);
        this.companySettings.CompanyIcon = this.companyIcon.dataUrl;
        this.companySettings.CompanyLogo = this.companyLogo.dataUrl;

        this.companySettingsService.putSettings(this.companySettings)
            .then(data => {
                this.load();
                this.isLoading = false;
            });
    }
}
