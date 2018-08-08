import { Component, OnInit, OnDestroy } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { CustomAPIService } from '../../../services/custom-api.service';
import { ApiService } from '../../../models/custom-api.model';
import { Router, ActivatedRoute } from '@angular/router';
import { RightSidebarItem } from '../../../models/rightsidebar.model';
import { RightSidebarService } from '../../../services/right-sidebar.service';

@Component({
    selector: 'd3s-admin-customapi-service-detail',
    providers: [CustomAPIService],
    template: ` 
                <div class="row">
                    <div class="col s12">                    
                        <div class="tile tile-detail">
                            <header>Service: {{service?.Name}}</header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <div class="row" *ngIf="!isLoading">
                                <div class="col l6 s12">
                                    <div class="row">
                                        <div class="col l6 s12">
                                            <div class="FieldName">URI Segment</div>
                                            <div>{{service?.UriPrefix}}</div>
                                        </div>
                                        <div class="col l6 s12">
                                            <div class="FieldName"># Endpoints</div>
                                            <div>{{numberOfEndpoints}}</div>
                                        </div>
                                        <div class="col s12">
                                            <div class="FieldName">Description</div>
                                            <div [innerHtml]="service.Description"></div>
                                        </div>
                                    </div>
                                </div>
                                <div class="col l6 s12">
                                    &nbsp;<!-- service metrics chart goes here-->
                                </div>
                            </div>
                        </div>
                    </div>
                </div>  
                <div class="row" *ngIf="!isLoading">
                    <div class="col s12">     
                        <d3s-admin-api-endpoints [service]="service" [(numberOfEndpoints)]="numberOfEndpoints"></d3s-admin-api-endpoints>
                    </div>
                </div>
                `
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
        this.areaName = "Custom API";
        this.setCommonItems();        
    }

    ngOnInit(): void {
        this.sub = this.route.params.subscribe(params => {
            this.serviceId = +params['serviceId']; // (+) converts string 'id' to a number            
            this.isLoading = true;
            this.customAPIService.getService(this.serviceId).then(res => {
                this.service = res;
                this.isLoading = false;           
                this.headerBreadcrumbService.clearBreadcrumbs();
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Administration'));
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Custom API', '/admin/customapi'));
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(`${this.service.Name}`));

                this.setCommonItems();
                this.setCommonRightSideBar(false);
                this.rightSidebarService.showItem(new RightSidebarItem('Namespaces', 'namespaces', ['fa-address-card'], `/admin/customapi/${this.serviceId}/namespaces`))
            });
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
    }    
}