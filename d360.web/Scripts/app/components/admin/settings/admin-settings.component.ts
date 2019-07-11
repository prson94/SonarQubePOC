import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute, NavigationStart } from '@angular/router';
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
import { SelectItem } from 'primeng/primeng';

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
    homePageImage: CompanyImage = new CompanyImage()
    groups: SelectItem[];
    sub: any;
    routeValidationMessage = "";
    rebuildLabel: string = "Refresh Search Index";
    disableRebuildIndex: boolean = false;
    
    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        private companySettingsService: CompanySettingsService,
        titleService: Title,
        private siteMenuService: SiteMenuService,        
        private stateService: StateService,
        private messagesService: MessagesService,
        private router: Router,
        private route: ActivatedRoute
    ) {

        super(headerBreadcrumbService, titleService);        
        this.areaName = "Settings";
        this.setCommonItems();

        this.load();
    }

    load(): void {
        this.isLoading = true;
        this.companySettingsService.getSettings()
            .subscribe(data => {
                this.companyLogo = new CompanyImage();
                this.companyIcon = new CompanyImage();
                this.homePageImage = new CompanyImage();

                this.companySettings = data;
                this.searchTypes = SettingsHelper.searchTypeStringToList(this.companySettings.DefaultSearchTypes);

                this.companySettings.SiteNav.forEach(s => {
                    s.IsCustom = (s.Name.indexOf('#') != 0)
                });
                this.companySettingsService.getGroups()
                    .subscribe(x => {
                        this.groups = x;
                        this.groups.unshift({ label: '[Administrators]', value: '0' });
                        this.isLoading = false;
                    });
             
            })
    }

    save(): void {
        this.isLoading = true;
        this.companySettings.DefaultSearchTypes = SettingsHelper.searchTypeListToString(this.searchTypes);
        this.companySettings.CompanyIcon = this.companyIcon.dataUrl;
        this.companySettings.CompanyLogo = this.companyLogo.dataUrl;
        this.companySettings.HomePageBackgroundImage = this.homePageImage.dataUrl;

        this.companySettingsService.putSettings(this.companySettings)
            .subscribe(data => {                
                this.isLoading = false;
                let type = data.type;
                if (type && type == "error") {
                    let message = data.message;
                    console.log("type: " + type + " message: " + message);
                    this.messagesService.showError("Problem Saving settings", message);
                } else {
                    window.location.reload();
                }
            });
    }

    validateRoute() {
        this.routeValidationMessage = "";

        if (this.companySettings.DefaultRoute == '' || this.companySettings.DefaultRoute == '/')
            return;

        let r = new RegExp('^(?:[a-z]+:)?//', 'i');

        if (r.test(this.companySettings.DefaultRoute))
            this.routeValidationMessage = "The value entered must be a relative url (ex: /artifact/1)";
    }

    rebuildDisplayValues() {
        this.companySettingsService.postDisplayRebuildRequest();
    }

    rebuildIndex() {
        this.disableRebuildIndex = true;
        this.companySettingsService.postIndexRebuildRequest()
            .subscribe(x => {
                if (x.type == "confirm") {
                    this.rebuildLabel = "Refresh Queued";
                } else {
                    this.disableRebuildIndex = false;
                }
            });
    }
}
