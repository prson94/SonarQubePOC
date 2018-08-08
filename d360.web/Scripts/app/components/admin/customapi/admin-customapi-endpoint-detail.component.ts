import { Component, OnInit, OnDestroy } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { CustomAPIService } from '../../../services/custom-api.service';
import { ApiService, ApiEndpoint, ApiVersion } from '../../../models/custom-api.model';
import { Router, ActivatedRoute } from '@angular/router';
import { RightSidebarService } from '../../../services/right-sidebar.service';

@Component({
    selector: 'd3s-admin-customapi-service-detail',
    providers: [CustomAPIService],
    template: ` 
                <div class="row">
                    <div class="col s12">                    
                        <div class="tile tile-detail">
                            <header>Endpoint: {{endpoint?.Name}}</header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <div class="row" *ngIf="!isLoading">
                                <div class="col l6 s12">
                                    <div class="row">
                                        <div class="col l6 s12">
                                            <div class="FieldName">URI Segment</div>
                                            <div>{{endpoint?.UriPrefix}}</div>
                                        </div>
                                        <div class="col l6 s12">
                                            <div class="FieldName"># Versions</div>
                                            <div>{{numberOfVersions}}</div>
                                        </div>
                                        <div class="col l6 s12">
                                            <div class="FieldName">Service</div>
                                            <div>{{service?.Name}}</div>
                                        </div>
                                        <div class="col l6 s12">
                                            <div class="FieldName">Path so far</div>
                                            <div>{{service?.UriPrefix}}/{{endpoint?.UriPrefix}}</div>
                                        </div>                                        
                                        <div class="col s12">
                                            <div class="FieldName">Description</div>
                                            <div [innerHtml]="service.Description"></div>
                                        </div>
                                    </div>
                                </div>
                                <div class="col l6 s12">
                                    &nbsp;<!-- endpoint service metrics chart goes here-->
                                </div>
                            </div>
                        </div>
                    </div>
                </div>  
                <div class="row" *ngIf="!isLoading">
                    <div class="col l4 s12">     
                        <d3s-admin-api-endpoint-versions [endpoint]="endpoint" [(selected)]="version" [(numberOfVersions)]="numberOfVersions"></d3s-admin-api-endpoint-versions>
                    </div>
                    <div class="col l8 s12" *ngIf="version!=null">
                        <div class="row">
                            <div class="col s12">
                                <d3s-admin-api-endpoint-version-fields [version]="version"></d3s-admin-api-endpoint-version-fields>
                            </div>
                            <div class="col s12">
                                <d3s-admin-api-endpoint-version-uritypes [version]="version"></d3s-admin-api-endpoint-version-uritypes>
                            </div>
                        </div>
                    </div>
                </div>
                `
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
        private messagesService: MessagesService,
        titleService: Title
    ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Custom API";
        this.setCommonItems();
    }

    ngOnInit(): void {
        this.sub = this.route.params.subscribe(params => {
            this.serviceId = +params['serviceId']; // (+) converts string 'id' to a number            
            this.endpointId = +params['endpointId']; // (+) converts string 'id' to a number            
            this.isLoading = true;
            this.customAPIService.getService(this.serviceId).then(res => {
                this.service = res;

                this.clearSidebar();
                this.headerBreadcrumbService.clearBreadcrumbs();
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Administration'));
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Custom API', '/admin/customapi'));
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(`${this.service.Name}`, `/admin/customapi/${this.service.ID}/details`));
                this.customAPIService.getEndpoint(this.endpointId).then(res => {
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