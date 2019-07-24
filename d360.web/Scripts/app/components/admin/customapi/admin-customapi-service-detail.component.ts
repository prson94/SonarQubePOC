import {Component, OnDestroy, OnInit} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {ActivatedRoute, Router} from '@angular/router';

import {Breadcrumb} from '../../../models/breadcrumb.model';
import {ApiService} from '../../../models/custom-api.model';
import {RightSidebarItem} from '../../../models/rightsidebar.model';

import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {MessagesService} from '../../../services/messages.service';
import {CustomAPIService} from '../../../services/custom-api.service';
import {RightSidebarService} from '../../../services/right-sidebar.service';

import {AdminBaseComponent} from '../admin-base.component';

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
        rightSidebarService: RightSidebarService,
        private messagesService: MessagesService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
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
                        this.setCommonItems();
                        this.rightSidebarService.setCurrentArea('Custom API', 'fa-cog', this.service.Name);
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(`${this.service.Name}`));
                        this.setCommonRightSideBar(false);

                        this.rightSidebarService.showItem(
                            new RightSidebarItem(
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
        this.sub.unsubscribe();
    }
}
