import { Component } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettings, CompanyImage, SearchType, SettingsHelper, CompanyRebuildJobStatusApiModel, CompanyRebuildJobStatusState, CompanySettingEnum, SettingsPutModel } from '../../../models/settings.model';
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
    providers: [SiteMenuService],
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
    addedRecords: HelpMenu[] = [];
    companySettings: CompanySettings = new CompanySettings();
    searchTypes: SearchType[];
    companyLogo: CompanyImage = new CompanyImage();
    companyIcon: CompanyImage = new CompanyImage();
    homePageImage: CompanyImage = new CompanyImage()
    groups: SelectItem[];
    sub: any;
    routeValidationMessage = "";
    disableExcel: boolean = false;

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
        protected settingsService: CompanySettingsService,
        private featureFlagService: FeatureFlagsService,
        private searchService: SearchService,
        secondaryNavService: SecondaryNavService,
        private helpMenuService: HelpMenuService,
        titleService: Title,
        private messagesService: MessagesObservableService
    ) {

        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);        
        this.areaName = StringConstants.Section_Settings;
        this.setCommonItems();

        this.distributedCacheEnabled = featureFlagService.flags[FeatureFlags.DistributedCacheFlag];
        this._featureFlagSubscription = featureFlagService.flagChange.subscribe((flags) => {
            this.distributedCacheEnabled = flags[FeatureFlags.DistributedCacheFlag].current;
        });

        this.load();
    }

    load(): void {
        this.isLoading = true;

        this.companyLogo = new CompanyImage();
        this.companyIcon = new CompanyImage();
        this.homePageImage = new CompanyImage();

         // Translate to settings object for page editing.
        this.companySettings.AllowedOrigins = this.getStringSetting(CompanySettingEnum.AllowedOrigins);
        this.companySettings.AssetDefinitionColumnWidth = this.getNumberSetting(CompanySettingEnum.AssetDefinitionColumnWidth);

        this.companySettings.BrowserTitlePrefix = this.getStringSetting(CompanySettingEnum.BrowserTitlePrefix);
        this.companySettings.CurrentIconPath = this.getStringSetting(CompanySettingEnum.CompanyIcon);
        this.companySettings.CurrentLogoPath = this.getStringSetting(CompanySettingEnum.CompanyLogo);
        this.companySettings.DefaultRoute = this.getStringSetting(CompanySettingEnum.DefaultRoute);

        this.companySettings.DefaultSearchTypes = this.getStringSetting(CompanySettingEnum.DefaultSearchTypes);
        this.searchService.getSearchCategories(true, true).subscribe(cat => {
            this.searchTypes = SettingsHelper.searchTypeStringToList(this.companySettings.DefaultSearchTypes, cat);
        });
        this.companySettings.DiagramMaxAvoidNodesLinkCount = this.getNumberSetting(CompanySettingEnum.DiagramMaxAvoidNodesLinkCount);
        this.companySettings.DisableCommunityPosting = this.getBooleanSetting(CompanySettingEnum.DisableCommunityPosting);
        this.companySettings.DisableIssueManagement = this.getBooleanSetting(CompanySettingEnum.DisableIssueManagement);
        this.companySettings.FramingDomains = this.getStringSetting(CompanySettingEnum.FramingDomains);
        this.companySettings.EnableOrganizations = this.getBooleanSetting(CompanySettingEnum.EnableOrganizations);
        this.companySettings.HideData3SixtyUsers = this.getBooleanSetting(CompanySettingEnum.HideData3SixtyUsers);
        this.companySettings.HideHeaderBarControls = this.getBooleanSetting(CompanySettingEnum.HideHeaderBarControls);
        this.companySettings.HomePageBackgroundImage = this.getStringSetting(CompanySettingEnum.HomePageBackgroundImage);
        this.companySettings.HomePageTitleColor = this.getStringSetting(CompanySettingEnum.HomePageTitleColor);
        this.companySettings.HomePageTitleSize = this.getStringSetting(CompanySettingEnum.HomePageTitleSize);

        let ipCollection = this.settingsService.getSettingById(CompanySettingEnum.IpRestriction).IpAddressSetting.Value;
        if (!ipCollection) {
            ipCollection = [];
        }
        this.companySettings.IpRestrictions = [];
        ipCollection.forEach((ip) => {
            this.companySettings.IpRestrictions.push({ End: ip.End, Name: ip.Name, Start: ip.Start });
        });

        this.companySettings.MaxDropdownItems = this.getNumberSetting(CompanySettingEnum.MaxDropdownItems);
        this.companySettings.MaxExcelExportRows = this.getNumberSetting(CompanySettingEnum.MaxExcelExportRows);
        this.companySettings.ShowAllUsersAPIKey = this.getBooleanSetting(CompanySettingEnum.ShowAllUsersAPIKey);
        this.companySettings.ShowHomeActivityTile = this.getBooleanSetting(CompanySettingEnum.ShowHomeActivityTile);
        this.companySettings.ShowHomeAssignmentTile = this.getBooleanSetting(CompanySettingEnum.ShowHomeAssignmentTile);
        this.companySettings.ShowHomeBoardTile = this.getBooleanSetting(CompanySettingEnum.ShowHomeBoardTile);
        this.companySettings.ShowHomePageTitle = this.getBooleanSetting(CompanySettingEnum.ShowHomePageTitle);
        this.companySettings.SiteNav.forEach(s => {
            s.IsCustom = (s.Name.indexOf('#') != 0)
        });
        this.companySettings.WorkflowCatchAllGroup = this.getNumberSetting(CompanySettingEnum.WorkflowCatchAllGroup);
        this.companySettings.WorkflowDigestEmailDays = this.getNumberSetting(CompanySettingEnum.WorkflowDigestEmailDays);
        this.companySettings.WriteActionDescription = this.getBooleanSetting(CompanySettingEnum.WriteActionDescription);
        this.companySettings.RequestCertificationDraft = this.getStringSetting(CompanySettingEnum.RequestCertificationDraft);

        this.settingsService.getGroups()
            .subscribe(x => {
                this.groups = x.map(x => {
                    return { label: x.label, value: +x.value }
                });
                this.groups.unshift({ label: '[Administrators]', value: 0 });
                this.isLoading = false;
            });
        this.resetSaveButton();

        this.settingsService.getRebuildRequestStatuses()
            .subscribe(data => {
                this.rebuildStatuses = data;
            });
    }

    resetSaveButton() {
        this.secondaryNavService.clearButtons();
        this.SaveButton = new DynamicButton("Save Changes");
        this.secondaryNavService.showButton(this.SaveButton);
        this.SaveButton.dynamicCallback = () => {
            this.SaveButton.disabled = true;
            this.SaveButton.isLoading = true;
            this.save();
        };
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
        this.helpMenuService.deleteHelpMenuItems(this.deletedRecords).subscribe((r) => {
        });
        this.helpMenuService.updateHelpMenuItems(this.items).subscribe((r) => {
        });
        this.helpMenuService.addHelpMenuItems(this.addedRecords).subscribe((r) => { });
        

        //#region Translate to settings array for v2 API.

        let settings: SettingsPutModel[] = [];

        settings.push({
            SettingID: CompanySettingEnum.AllowedOrigins,
            StringSetting: { Value: this.companySettings.AllowedOrigins },
            BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.AssetDefinitionColumnWidth,
            NumberSetting: { Value: this.companySettings.AssetDefinitionColumnWidth },
            BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, StringSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.BrowserTitlePrefix,
            StringSetting: { Value: this.companySettings.BrowserTitlePrefix  },
            BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        if (this.companyIcon.dataUrl && !this.companySettings.SetIconToDefault) {
            settings.push({
                SettingID: CompanySettingEnum.CompanyIcon,
                StringSetting: { Value: this.companyIcon.dataUrl },
                BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
            });
        }
        else if (this.companySettings.SetIconToDefault) {
            settings.push({
                SettingID: CompanySettingEnum.CompanyIcon,
                StringSetting: { Value: null },
                BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
            });
        }
        if (this.companyLogo.dataUrl && !this.companySettings.SetLogoToDefault) {
            settings.push({
                SettingID: CompanySettingEnum.CompanyLogo,
                StringSetting: { Value: this.companyLogo.dataUrl },
                BooleanSetting: null,
                GuidSetting: null,
                IpAddressSetting: null,
                NumberSetting: null
            });
        }
        else if (this.companySettings.SetLogoToDefault) {
            settings.push({
                SettingID: CompanySettingEnum.CompanyLogo,
                StringSetting: { Value: null },
                BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
            });
        }
        settings.push({
            SettingID: CompanySettingEnum.DefaultRoute,
            StringSetting: { Value: this.companySettings.DefaultRoute },
            BooleanSetting: null,
            GuidSetting: null,
            IpAddressSetting: null,
            NumberSetting: null
        });
        let defaultSearchTypes = SettingsHelper.searchTypeListToString(this.searchTypes);
        settings.push({
            SettingID: CompanySettingEnum.DefaultSearchTypes,
            StringSetting: { Value: defaultSearchTypes },
            BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.DisableCommunityPosting,
            BooleanSetting: { Value: this.companySettings.DisableCommunityPosting },
            StringSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.DisableIssueManagement,
            BooleanSetting: { Value: this.companySettings.DisableIssueManagement },
            StringSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.EnableOrganizations,
            BooleanSetting: { Value: this.companySettings.EnableOrganizations },
            StringSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });        
        settings.push({
            SettingID: CompanySettingEnum.FramingDomains,
            StringSetting: { Value: this.companySettings.FramingDomains },
            BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.HideData3SixtyUsers,
            BooleanSetting: { Value: this.companySettings.HideData3SixtyUsers },
            StringSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.HideHeaderBarControls,
            BooleanSetting: { Value: this.companySettings.HideHeaderBarControls },
            StringSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        if (this.homePageImage.dataUrl && !this.companySettings.ClearHomePageBackgroundImage) {
            settings.push({
                SettingID: CompanySettingEnum.HomePageBackgroundImage,
                StringSetting: { Value: this.homePageImage.dataUrl },
                BooleanSetting: null,
                GuidSetting: null,
                IpAddressSetting: null,
                NumberSetting: null
            });
        }
        else if (this.companySettings.ClearHomePageBackgroundImage) {
            settings.push({
                SettingID: CompanySettingEnum.HomePageBackgroundImage,
                StringSetting: { Value: null },
                BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
            });
        }
        settings.push({
            SettingID: CompanySettingEnum.HomePageTitleColor,
            StringSetting: { Value: this.companySettings.HomePageTitleColor },
            BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.HomePageTitleSize,
            StringSetting: { Value: this.companySettings.HomePageTitleSize },
            BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.IpRestriction,
            IpAddressSetting: { Value: this.companySettings.IpRestrictions },
            BooleanSetting: null, GuidSetting: null, NumberSetting: null, StringSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.MaxDropdownItems,
            NumberSetting: { Value: this.companySettings.MaxDropdownItems },
            BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, StringSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.MaxExcelExportRows,
            NumberSetting: { Value: this.companySettings.MaxExcelExportRows },
            BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, StringSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.ShowAllUsersAPIKey,
            BooleanSetting: { Value: this.companySettings.ShowAllUsersAPIKey },
            StringSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.ShowHomeActivityTile,
            BooleanSetting: { Value: this.companySettings.ShowHomeActivityTile },
            StringSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.ShowHomeAssignmentTile,
            BooleanSetting: { Value: this.companySettings.ShowHomeAssignmentTile },
            StringSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.ShowHomeBoardTile,
            BooleanSetting: { Value: this.companySettings.ShowHomeBoardTile },
            StringSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.ShowHomePageTitle,
            BooleanSetting: { Value: this.companySettings.ShowHomePageTitle },
            StringSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.WriteActionDescription,
            BooleanSetting: { Value: this.companySettings.WriteActionDescription },
            GuidSetting: null, IpAddressSetting: null, NumberSetting: null, StringSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.WorkflowCatchAllGroup,
            NumberSetting: { Value: this.companySettings.WorkflowCatchAllGroup },
            BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, StringSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.WorkflowDigestEmailDays,
            NumberSetting: { Value: this.companySettings.WorkflowDigestEmailDays },
            BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, StringSetting: null
        });
        settings.push({
            SettingID: CompanySettingEnum.RequestCertificationDraft,
            StringSetting: { Value: this.companySettings.RequestCertificationDraft },
            BooleanSetting: null, GuidSetting: null, IpAddressSetting: null, NumberSetting: null
        });

        //#endregion

        this.settingsService.putSettings(settings)
            .subscribe(
                (data) => {
                    this.isLoading = false;
                    if (data && data.type === "error") {
                        this.resetSaveButton();
                        this.messagesService.showError(data.title, data.message);
                    }
                    else {
                        window.location.reload();
                    }
                }
            );
    }

    validateExcelSize() {
        if ((this.companySettings.MaxExcelExportRows < 0) || (this.companySettings.MaxExcelExportRows > 100000)) {
            this.disableExcel = true;
        }
        else {
            this.disableExcel = false;
        }
        this.secondaryNavService.clearButtons();
        this.SaveButton = new DynamicButton("Save Changes");
        this.secondaryNavService.showButton(this.SaveButton);
        this.SaveButton.disabled = this.disableExcel;
        this.SaveButton.dynamicCallback = () => {
            this.SaveButton.disabled = true;
            this.SaveButton.isLoading = true;
            this.save();
        };
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
        this.settingsService.postRebuildRequest(model.jobToken)
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
