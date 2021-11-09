import {Component, OnDestroy, OnInit} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {ActivatedRoute, Router} from '@angular/router';

import {Breadcrumb} from '../../../models/breadcrumb.model';
import {ApiService} from '../../../models/custom-api.model';
import {SecondaryNavItem} from '../../../models/secondaryNav.model';

import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {CustomAPIService} from '../../../services/custom-api.service';
import {SecondaryNavService} from '../../../services/right-sidebar.service';

import {AdminBaseComponent} from '../admin-base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-customapi-service-detail',
    providers: [CustomAPIService],
    templateUrl: './admin-customapi-service-detail.component.html'
})

export class AdminCustomAPIServiceDetailComponent extends AdminBaseComponent implements OnInit, OnDestroy {

    public service: ApiService = null;
    private sub: any;
    private serviceId: number;

    public numberOfEndpoints: number = 0;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        protected customAPIService: CustomAPIService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        secondaryNavService: SecondaryNavService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
    }

    ngOnInit(): void {
        this.sub = this
            .route
            .params
            .subscribe(params => {
                this.isLoading = true;

                this.serviceId = +params['serviceId']; // (+) converts string 'id' to a number

                this.customAPIService.getService(this.serviceId).subscribe(
                    res => {
                        this.isLoading = false;
                        this.service = res;
                        this.adminHeading = 'Integration';
                        this.areaName ='Custom API';
                        this.areaLink = '/admin/customapi';
                        this.tabTitle = 'Service';
                        this.setCommonSecondaryNavTabs(false);
                        this.setCommonItems();
                        this.secondaryNavService.setCurrentArea(this.service.Name, 'fa-cog', this.tabTitle);
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(`${this.service.Name}`));

                        this.secondaryNavService.showItem(
                            new SecondaryNavItem(
                                'Namespaces',
                                'namespaces',
                                ['fa-address-card'],
                                `/admin/customapi/${this.serviceId}/namespaces`
                            )
                        )
                    }
                );
            });
    }

    ngOnDestroy() {
        this.clearSidebar();
        if (this.sub) {
            this.sub.unsubscribe();
        }
        
    }
}
