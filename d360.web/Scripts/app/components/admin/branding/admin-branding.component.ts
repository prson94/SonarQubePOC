import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import { DomSanitizer, Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import * as _ from 'lodash';
import { forkJoin } from 'rxjs';
import { BrandingService, Theme } from '../../../services/branding.service';
import { FeatureFlags, FeatureFlagsService } from '../../../services/featureflags.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { StringConstants } from '../../../static/string-constants';
import { AdminBaseComponent } from '../admin-base.component';

@Component({
    selector: "admin-branding",
    templateUrl: "admin-branding.component.html",
    encapsulation: ViewEncapsulation.None,
    providers: [BrandingService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["./admin-branding.component.less"]
})

export class AdminBrandingComponent extends AdminBaseComponent implements OnInit {
    themes: Theme[] = [];

    sidePanelOpen: boolean = false;
    sidePanelLoading: boolean = false;
    sidePanelStorageKey: string = "gov-branding-side-panel";
    sidePanelTab: string = 'detail';
    selectedRow: Theme;

    isEditorVisible: boolean = false;
    showDelete: boolean = false;
    deleteInProgress: boolean = false;

    isSetCurrentThemeVisible: boolean = false;
    settingCurrentThemeInProgress: boolean = false;

    preselectThemeName: string = "";

    menuItemsDefaultOptions = [
        { title: 'Download' },
        { title: 'Edit' },
        { title: 'Duplicate' },
        { title: 'Set as Current Theme' },
        { title: 'Delete' },
    ];

    constructor(
        private brandingService: BrandingService,
        private messagesService: MessagesObservableService,
        protected router: Router,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef,
        public sanitizer: DomSanitizer,
        featureFlagService?: FeatureFlagsService) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);

        this.areaName = StringConstants.Section_Branding;
        this.setCommonItems();
    }

    ngOnInit() {
        this.isLoading = true;
        this.brandingService.getThemes().subscribe((res: Theme[]) => {
            this.themes = res;
            this.selectedRow = null;
            this.themes.forEach((t) => {
                t.svg = this.svgUrl(t);
                var menuItems = [];

                t.hasDownloadOption ? menuItems.push(this.menuItemsDefaultOptions[0]) : null;
                t.hasEditOption ? menuItems.push(this.menuItemsDefaultOptions[1]) : null;
                t.hasDuplicateOption ? menuItems.push(this.menuItemsDefaultOptions[2]) : null;
                t.hasSetAsCurrentThemeOption ? menuItems.push(this.menuItemsDefaultOptions[3]) : null;
                t.hasDeleteOption ? menuItems.push(this.menuItemsDefaultOptions[4]) : null;

                t.menuItems = menuItems;

            });
            this.isLoading = false;

            if (this.preselectThemeName) {
                var preselectedItem = this.themes.filter((x) => x.name === this.preselectThemeName);
                if (preselectedItem.length > 0) {
                    this.selectedRow = preselectedItem[0];
                }
                this.preselectThemeName = "";
            }

            this.cdRef.markForCheck();
        });
    }

    svgUrl(theme: Theme) {
        return `/api/v2/environment/themes/${theme.uid}.svg?width=158&cache=${theme.updatedOn}`;
    }

    onSave() {
        this.isEditorVisible = false;
        this.ngOnInit();
    }

    onCancel() {
        this.isEditorVisible = false;
    }

    clickMenuItem($event, item) {
        this.selectedRow = item;
        var value = $event.value ?? $event;
        switch (value) {
            case 'Edit':
                this.isEditorVisible = true;
                break;
            case 'Delete':
                this.showDelete = true;
                break;
            case 'Duplicate':
                this.duplicateSelectedTheme();
                break;
            case 'Download':
                this.download();
                break;
            case 'Set as Current Theme':
                this.isSetCurrentThemeVisible = true;
                break;
        }
    }

    linkClicked($event) {
        this.clickMenuItem($event, this.selectedRow);
    }

    delete() {
        this.deleteInProgress = true;
        this.brandingService.deleteTheme(this.selectedRow.uid)
            .subscribe((res) => {
                if (res) {
                    this.showDelete = false;
                }
                this.deleteInProgress = false;
                this.ngOnInit();
                this.cdRef.markForCheck();
            });
    }

    setAsCurrentTheme() {
        this.settingCurrentThemeInProgress = true;
        this.brandingService.setAsCurrentTheme(this.selectedRow.uid)
            .subscribe((res) => {
                if (res) {
                    this.isSetCurrentThemeVisible = false;
                    location.reload();
                }
                this.settingCurrentThemeInProgress = false;
                this.ngOnInit();
                this.cdRef.markForCheck();
            });
    }

    duplicateSelectedTheme() {
        var theme = _.cloneDeep(this.selectedRow._orig);
        let uid = theme.uid;
        theme.name = this.getUniqueName(this.selectedRow.name, 0);
        theme.isCurrent = false;
        theme.uid = "";
        theme.headerLogoUri = theme.homeBackgroundUri = theme.iconUri = null;

        this.isLoading = true;
        this.cdRef.markForCheck();
        this.preselectThemeName = theme.name;

        forkJoin(
            this.brandingService.getThemeCustomCSS(theme),
            this.brandingService.getBase64Data(uid))
            .subscribe((data) => {
                var res = data[1];
                theme.headerLogo = res.headerLogo;
                theme.homeBackground = res.homeBackground;
                theme.icon = res.icon;
                if (data[0]) {
                    theme.customCss = window.btoa(data[0]);
                }

                this.brandingService.saveTheme(theme)
                    .subscribe((res) => {
                        this.ngOnInit();
                        this.cdRef.markForCheck();
                    });
            });
    }

    getUniqueName(name: string, idx: number) {
        var checkName = name;
        if (idx > 0) {
            checkName += ` (${idx})`;
        }
        if (this.themes.some((x) => x.name === checkName)) {
            return this.getUniqueName(name, idx + 1);
        }
        return checkName;
    }

    download() {
        var toExport = _.cloneDeep(this.selectedRow);

        //delete properties we do not wish to export
        delete toExport["defaultThemeUid"];
        delete toExport["menuItems"];
        delete toExport["_orig"];
        delete toExport["svg"];
        delete toExport["headerLogo"];
        delete toExport["homeBackground"];
        delete toExport["icon"];
        delete toExport["headerLogoUri"];
        delete toExport["homeBackgroundUri"];
        delete toExport["iconUri"];
        delete toExport["customCssUri"];

        //load base64 data of properties and export
        this.brandingService.getBase64Data(toExport.uid)
            .subscribe((res) => {
                if (res["headerLogo"]) {
                    toExport.headerLogo = res["headerLogo"];
                }

                if (res["homeBackground"]) {
                    toExport.homeBackground = res["homeBackground"];
                }

                if (res["icon"]) {
                    toExport.icon = res["icon"];
                }

                if (toExport.customCss) {
                    toExport.customCss = btoa(toExport.customCss);
                }

                var sJson = JSON.stringify(toExport, null, 4);
                var element = document.createElement('a');
                element.setAttribute('href', "data:text/json;charset=UTF-8," + encodeURIComponent(sJson));
                element.setAttribute('download', this.getExportName(toExport));
                element.style.display = 'none';
                document.body.appendChild(element);
                element.click(); // simulate click
                document.body.removeChild(element);
            });
    }

    getExportName(theme: Theme): string {
        var dateParts = new Date(theme.updatedOn).toDateString().split(" ");
        return `${theme.name}_${this.getEnvironment()}_${dateParts[2] + dateParts[1] + dateParts[3]}.json`;
    }

    getEnvironment(): string {
        var url = window.location.href.toLowerCase();
        if (url.indexOf(".dev.") !== -1) {
            return "DEV";
        }
        if (url.indexOf(".eng.") !== -1) {
            return "QA";
        }
        if (url.indexOf(".uat.") !== -1) {
            return "UAT";
        }
        if (url.indexOf(".preview.") !== -1) {
            return "PREVIEW";
        }
        return "PROD";
    }

    isThemeUploading: boolean = false;
    file: File;
    themeToLoad: Theme;

    shouldRename: boolean = false;
    canReplace: boolean = false;

    existingThemeName: string = "";
    existingThemeUid: string = "";

    @ViewChild('uploadInput') fileInputEl: ElementRef;
    onFileSelected(event) {
        this.themeToLoad = null;
        this.file = event.target.files[0];
        let fileReader = new FileReader();
        fileReader.onload = (e) => {
            try {
                this.themeToLoad = JSON.parse(fileReader.result as string);
                this.themeToLoad.uid = null;
                this.themeToLoad.isCurrent = null;
                this.checkUploadTheme();
            }
            catch {
                var error = { error: `Cannot upload file because this is not a valid D360 Govern branding JSON definition` };
                this.showHttpErrorMessage(this.messagesService, error as HttpErrorResponse);
            }
        };

        fileReader.readAsText(this.file);
    }

    resetUpload() {
        this.isThemeUploading = false;
        this.file = null;
        this.themeToLoad = null;
        this.shouldRename = false;
        this.existingThemeName = "";
        this.existingThemeUid = "";
        this.fileInputEl.nativeElement.value = "";
    }

    checkUploadTheme() {
        var _toValidate = _.cloneDeep(this.themeToLoad);
        _toValidate.name = "temp_" + Date.now().toString();
        this.brandingService.validateTheme(_toValidate)
            .subscribe((res) => {
                if (res) {
                    var existingItem = this.themes.find((x) => x.name === this.themeToLoad.name);

                    if (existingItem) {
                        this.shouldRename = true;
                        this.existingThemeName = existingItem.name;
                        this.existingThemeUid = existingItem.uid;
                        if (!existingItem.isCurrent && !existingItem.isDefaultTheme) {
                            this.canReplace = true;
                        }

                    }
                    else {
                        this.uploadTheme();
                    }
                    this.cdRef.markForCheck();
                }
            }, (error) => {
                this.resetUpload();

                error.Message = error.error.message;
                this.showMessageForApiResponse(this.messagesService, error);
            }
            );


        this.cdRef.markForCheck();
    }

    uploadTheme(replaceExisting: boolean = false) {
        this.isThemeUploading = true;
        this.preselectThemeName = this.themeToLoad.name;
        if (replaceExisting) {
            this.themeToLoad.uid = this.existingThemeUid;
        }
        this.brandingService.saveTheme(this.themeToLoad)
            .subscribe((res) => {
                this.ngOnInit();
                this.isThemeUploading = false;
                this.resetUpload();
                this.cdRef.markForCheck();
            });
    }

    triggerRename() {
        this.checkUploadTheme();
    }
}
