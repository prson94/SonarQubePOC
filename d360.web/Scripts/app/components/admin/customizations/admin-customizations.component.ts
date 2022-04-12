import { Component, OnInit } from '@angular/core';
import { AdminBaseComponent } from '../admin-base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SiteCustomizationsService } from '../../../services/site-customizations.service';
import { Title } from '@angular/platform-browser';
import 'codemirror/mode/css/css.js';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-customizations-component',
    providers: [SiteCustomizationsService],
    templateUrl: 'admin-customizations.component.html'
})

export class AdminCustomizationsComponent extends AdminBaseComponent implements OnInit {
    private customCss: string;

    saveLabel = $localize`Save`;

    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        secondaryNavService: SecondaryNavService,
        protected siteCustomizationsService: SiteCustomizationsService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_Branding;
        this.setCommonItems();
    }

    ngOnInit() {
        this.load();
    }

    private baseConfig = {
        lineNumbers: true,
        theme: 'eclipse',
        mode: 'css'
    };

    private load() {
        this.isLoading = true;
        this.siteCustomizationsService.getCustomCss().subscribe(res => {
            this.isLoading = false;
            this.customCss = res;
        });
    }

    saveCustomizations() {
        this.siteCustomizationsService.saveCustomCss(this.customCss).subscribe(res => {
            this.showMessageForResult(this.messagesService, res);
            window.location.reload();
        });
    }
}