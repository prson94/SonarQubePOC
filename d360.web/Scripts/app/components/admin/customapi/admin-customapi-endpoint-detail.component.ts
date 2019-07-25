import {Component, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {Title} from '@angular/platform-browser';

import {Breadcrumb} from '../../../models/breadcrumb.model';
import {ApiEndpoint, ApiService, ApiVersion} from '../../../models/custom-api.model';

import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {CustomAPIService} from '../../../services/custom-api.service';
import {RightSidebarService} from '../../../services/right-sidebar.service';

import {AdminBaseComponent} from '../admin-base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-customapi-service-detail',
    providers: [CustomAPIService],
    templateUrl: './admin-customapi-endpoint-detail.component.html'
})

export class AdminCustomAPIEndpointDetailComponent extends AdminBaseComponent implements OnInit, OnDestroy {

    public service: ApiService = null;
    public endpoint: ApiEndpoint = null;
    private sub: any;
    private serviceId: number;
    private endpointId: number;
    public numberOfVersions: number = 0;
    public version: ApiVersion = null;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        protected customAPIService: CustomAPIService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        rightSidebarService: RightSidebarService,
        private messagesService: MessagesObservableService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Custom API";
        this.setCommonItems();
    }

    ngOnInit(): void {
        this.sub = this.route.params.subscribe(params => {
            this.isLoading = true;

            this.serviceId = +params['serviceId']; // (+) converts string 'id' to a number            
            this.endpointId = +params['endpointId']; // (+) converts string 'id' to a number            

            this.customAPIService.getService(this.serviceId).subscribe(
                res => {
                    this.service = res;

                    this.clearSidebar();
                    this.headerBreadcrumbService.clearBreadcrumbs();

                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Administration'));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Custom API', '/admin/customapi'));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(`${this.service.Name}`, `/admin/customapi/${this.service.ID}/details`));

                    this.customAPIService.getEndpoint(this.endpointId).subscribe(
                        res => {
                            this.endpoint = res;

                            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(`${this.endpoint.Name}`));

                            this.isLoading = false;
                        });
                });
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}
