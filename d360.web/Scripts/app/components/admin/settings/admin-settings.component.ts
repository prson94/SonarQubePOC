import { Component } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { ICompanySettingsService, CompanySettings, IpRestriction, CompanyImage, SearchType, SettingsHelper } from '../../../models/settings.model';
import { SiteNav } from '../../../models/site-menu.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { StateService } from '../../../services/state.service';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { FormMode } from '../../../models/form.model';

@Component({
    selector: 'admin-settings',
    providers: [CompanySettingsService, SiteMenuService],
    templateUrl: './admin-settings.component.html',
    styles: [`
        .remove {
            cursor: pointer; 
            color: maroon; 
            font-size: 1.5em;
            vertical-align: middle;
        }
        input[type=text] {
            width: 90%;
            height:25px;
        }
  `],    
})

export class AdminSettingsComponent extends AdminBaseComponent {
    
    companySettings: CompanySettings = new CompanySettings();
    searchTypes: SearchType[] = SettingsHelper.getSearchTypesList();
    companyLogo: CompanyImage = new CompanyImage();
    companyIcon: CompanyImage = new CompanyImage();
    
    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        private companySettingsService: CompanySettingsService,
        titleService: Title,
        private siteMenuService: SiteMenuService,        
        private stateService: StateService,
        private messagesService: MessagesService
    ) {

        super(headerBreadcrumbService, titleService);        
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

                this.companySettings.SiteNav.forEach(s => {
                    s.IsCustom = (s.Name.indexOf('#') != 0)
                });
                
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
                this.isLoading = false;
                window.location.reload();
            });
    }
}
