import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-resource-change-pwd',
    template: `            
            <div class="row">
                <div class="col s12">
                    <div class="tile tile-detail">  
                                <d3s-resource-password (onClose)="onCloseEvent()"></d3s-resource-password>      
                    </div>                    
                </div>
            </div>
        `,
})

export class ResourceChangePwdComponent extends BaseComponent implements OnInit {

    constructor(
        private headerBreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        private titleService: Title,
        private router: Router) {
        super(settingsService);
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb($localize`Change Your Password`));
        this.setBrowserTitle(this.titleService, $localize`Change Your Password`);
    }

    public onCloseEvent() {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_HOME_ROOT}`);
    }
}