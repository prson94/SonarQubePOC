import { Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { StringConstants } from '../../static/string-constants';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { FusionAttributeService } from '../../services/fusion-attribute.service';
import { FusionAttributeValueDetails } from '../../models/fusion-attribute.model';
import { FormMode } from '../../models/form.model';

@Component({
    selector: 'd3s-fusion-attribute-details',
    template: `  <d3s-loading [isLoading]="isLoading"></d3s-loading>                                                  
                 <div class="row" *ngIf="!isLoading">    
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-object-definition-tile [objectPermissions]="permissions" [objectID]="id" [objectType]="type" [hasAttributes]="false" (formModeChange)="formModeChange($event)"></d3s-object-definition-tile>
                            <button pButton type="button" (click)="close()" *ngIf="formMode == FormMode.Default" label="Close"></button>
                        </div>           
                    </div>
                 </div>
                 `,
    providers: [PermissionsService, FusionAttributeService],
})


export class FusionAttributeDetailsComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private type: string = '';
    private id: number = -1;
    private name: string = '';
    private fusionAttributeDetail: FusionAttributeValueDetails;
    private formMode :FormMode = FormMode.Default;
    FormMode = FormMode;

    constructor(        
        private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService,
        private titleService: Title,
        private headerBreadcrumbService: HeaderBreadcrumbService,
        private permissionsService: PermissionsService,
        private fusionAttributeService: FusionAttributeService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {        
        this.setCommonRightSideBar(true, true, false, true, true, true, false);
        this.sub = this.route.params.subscribe(params => {
            this.type = params['type'];
            this.id = +params['id'];
            this.name = params['name'] ? params['name'] : 'Details';
            
            this.setBrowserTitle(this.titleService, this.name);        
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.name));
            
            this.loadPermissions(this.permissionsService, StringConstants.ObjectFusionAttribute, this.id);
        });

        this.fusionAttributeService.getFusionAttributeDetails(this.type, this.id).subscribe(
            item => {
                this.fusionAttributeDetail = item;
                this.setObjectInfo(this.type, this.id, undefined, this.fusionAttributeDetail.AssetID);
                this.setCommonRightSideBar(true, true, false, true, true, true, false);
            }
        );
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    private formModeChange($event: FormMode) {
        this.formMode = $event;
    }
    private close() {
       this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('FusionTypeWithFusionAttributeType', this.fusionAttributeDetail.FusionAttributeTypeID, this.fusionAttributeDetail.FusionID ));
    }
};