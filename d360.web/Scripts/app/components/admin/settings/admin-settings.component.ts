import { Component, Input, ViewChild } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettings, CompanyImage, SearchType, SettingsHelper, CompanyRebuildJobStatusApiModel, CompanyRebuildJobStatusState } from '../../../models/settings.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { SearchService } from '../../../services/search.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { SelectItem } from 'primeng/api';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { DynamicButton } from '../../../models/secondaryNav.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { HelpMenu } from '../../../models/helpmenu.model';
import { HelpMenuService } from '../../shared/helpmenu/helpmenu.service';
import { FeatureFlags, FeatureFlagsService } from '../../../services/featureflags.service';

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
        .hiddencategory {
            color: #bdbfc6;
            font-style: italic;
        }
        input[type=text] {
            width: 90%;
            height:25px;
        }
  `],    
})

export class AdminSettingsComponent extends AdminBaseComponent {
    items: HelpMenu[] = []; 
    deletedRecords: HelpMenu[] = []; 
    companySettings: CompanySettings = new CompanySettings();
    searchTypes: SearchType[];
    companyLogo: CompanyImage = new CompanyImage();
    companyIcon: CompanyImage = new CompanyImage();
    homePageImage: CompanyImage = new CompanyImage()
    groups: SelectItem[];
    sub: any;
    routeValidationMessage = "";

    rebuildStatuses: CompanyRebuildJobStatusApiModel[] = [];

    disableRebuildAssetGraph: boolean = false;
    graphValidationMessage = "";
    
    disableRebuildDisplayValue: boolean = false;
    displayValueValidationMessage = "";

    disableRebuildIndex: boolean = false;
    indexValidationMessage = "";
    
    SaveButton: DynamicButton;

    distributedCacheEnabled: boolean;
    _featureFlagSubscription: any;

    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        private companySettingsService: CompanySettingsService,
        private featureFlagService: FeatureFlagsService,
        private searchService: SearchService,
        secondaryNavService: SecondaryNavService,
        private helpMenuService: HelpMenuService,
        titleService: Title,
        private messagesService: MessagesObservableService,
    ) {

        super(headerBreadcrumbService, titleService, secondaryNavService);        
        this.areaName = StringConstants.Section_Settings;
        this.setCommonItems();

        this.distributedCacheEnabled = featureFlagService.flags[FeatureFlags.DistributedCacheFlag];
        console.log("Cache Feature Value Is: " + this.distributedCacheEnabled);
        this._featureFlagSubscription = featureFlagService.flagChange.subscribe((flags) => {
            this.distributedCacheEnabled = flags[FeatureFlags.DistributedCacheFlag].current;
            console.log("Cache Feature Changed: " + this.distributedCacheEnabled);
        })

        this.load();
    }

    load(): void {
        this.isLoading = true;
        this.companySettingsService.getSettings()
            .subscribe(data => {
                this.companyLogo = new CompanyImage();
                this.companyIcon = new CompanyImage();
                this.homePageImage = new CompanyImage();
                delete data['EnableShoppingCart'];

                this.companySettings = data;

                this.searchService.getSearchCategories(true, true).subscribe(cat => {
                    this.searchTypes = SettingsHelper.searchTypeStringToList(this.companySettings.DefaultSearchTypes, cat);
                });
                
                this.companySettings.SiteNav.forEach(s => {
                    s.IsCustom = (s.Name.indexOf('#') != 0)
                });
                this.companySettingsService.getGroups()
                    .subscribe(x => {
                        this.groups = x.map(x => {
                            return { label: x.label, value: +x.value }
                        });
                        this.groups.unshift({ label: '[Administrators]', value: 0 });
                        this.isLoading = false;
                    });
                this.secondaryNavService.clearButtons();
                this.SaveButton = new DynamicButton("Save Changes");
                this.secondaryNavService.showButton(this.SaveButton);
                this.SaveButton.dynamicCallback = () => {
                    this.SaveButton.disabled = true;
                    this.SaveButton.isLoading = true;
                    this.save();
                };
            })

        this.companySettingsService.getRebuildRequestStatuses()
            .subscribe(data => {
                this.rebuildStatuses = data;
            });
    }

    save(): void {
        this.isLoading = true;
        this.companySettings.DefaultSearchTypes = SettingsHelper.searchTypeListToString(this.searchTypes);
        this.companySettings.CompanyIcon = this.companyIcon.dataUrl;
        this.companySettings.CompanyLogo = this.companyLogo.dataUrl;
        this.companySettings.HomePageBackgroundImage = this.homePageImage.dataUrl;

        this.items.sort((a, b) => (a.order < b.order ? -1 : 1));
        for (let i = 0; i < this.items.length; i++) {
            this.items[i].order = i;
        }
        this.helpMenuService.updateHelpMenuItems(this.items, this.deletedRecords).subscribe((r) => {
        });

        this.companySettingsService.putSettings(this.companySettings)
            .subscribe(data => {                
                this.isLoading = false;
                let type = data.type;
                if (type && type === "error") {
                    this.messagesService.showError("Problem Saving settings", data.message);
                } else {
                    window.location.reload();
                }
            });
    }

    validateRoute() {
        this.routeValidationMessage = "";

        if (this.companySettings.DefaultRoute === '' || this.companySettings.DefaultRoute === '/')
            return;

        let r = new RegExp('^(?:[a-z]+:)?//', 'i');

        if (r.test(this.companySettings.DefaultRoute))
            this.routeValidationMessage = "The value entered must be a relative url (ex: /artifact/1)";
    }

    rebuild(model: CompanyRebuildJobStatusApiModel) {
        model.state = CompanyRebuildJobStatusState.Active;
        this.companySettingsService.postRebuildRequest(model.jobToken)
            .subscribe(data => {
                if (data.type && data.type === "error") {
                    this.messagesService.showError("Problem with Rebuild", data.message);
                    model.state = CompanyRebuildJobStatusState.Inactive;
                } else {
                    model.validationMessage = data.message;
                }
            });
    }

    public isDisabled(model: CompanyRebuildJobStatusApiModel): boolean {
        return (+model.state === +CompanyRebuildJobStatusState.Active);
    }

    public rebuildJobIcon(model: CompanyRebuildJobStatusApiModel): string {
        let css = 'fa fa-gear mr8';
        if (+model.state === +CompanyRebuildJobStatusState.Active) {
            css = 'fa fa-spinner fa-spin mr8';
        }
        return css;
    }
}
