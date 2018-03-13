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

@Component({
    selector: 'd3s-fusion-attribute-details',
    template: `  <d3s-loading [isLoading]="isLoading"></d3s-loading>                                                  
                 <div class="row" *ngIf="!isLoading">    
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-fusion-attribute-item-details [fusionAttributeId]="id" [name]="name" [objectType]="type" (close)="close()" [hasClose]="true"></d3s-fusion-attribute-item-details>                            
                        </div>           
                    </div>
                 </div>
                `,
    providers: [PermissionsService],
})

export class FusionAttributeDetailsComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private type: string = '';
    private id: number = -1;
    private name: string = '';

    constructor(        
        private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService,
        private titleService: Title,
        private headerBreadcrumbService: HeaderBreadcrumbService,
        private location: Location,
        private permissionsService: PermissionsService
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
            this.setObjectInfo(this.type, this.id);
            this.setCommonRightSideBar(true, true, false, true, true, true, false);
            this.loadPermissions(this.permissionsService, StringConstants.ObjectFusionAttribute, this.id);
        });
    }


    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    private close() {
        this.location.back();
    }
};