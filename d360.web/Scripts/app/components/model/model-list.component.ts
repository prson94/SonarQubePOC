import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { ModelsService } from '../../services/models.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Model } from '../../models/model.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-model-list',
    providers: [ModelsService],    
    template: `                 
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>                                                
                        <div class="tile tile-detail" *ngIf="!isLoading">                            
                            <header>{{modelGroup}} Models
                                <d3s-tile-actions [hasAdd]="false" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>         
                            <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                                                   
                            <p-dataTable #dt sortField="TaxonomyTypeClass" sortOrder="1" [globalFilter]="gb"  [value]="models | modelType: modelGroup" scrollable="true" scrollWidth="100%" selectionMode="single" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" pageLinks="3" [selection]="selected" (selectionChange)="selected=$event;objectID=selected.ID"  (onRowDblclick)="selected=$event.data;showModel(selected);" >
                                <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>                                
                                <p-column field="Name" header="Name" sortable="true" [style]="{width:'200px'}" [filter]="!showSimpleFilter">
                                    <ng-template let-item="rowData" pTemplate type="body">
                                            <a (click)="showModel(item)">{{item.Name}}</a>
                                    </ng-template>
                                </p-column>                                                                                                                                                        
                                <p-column field="Description" header="Description" sortable="true" [style]="{width:'500px'}"  [filter]="!showSimpleFilter">
                                    <ng-template let-col let-data="rowData" pTemplate type="body">
                                        <span [innerHtml]="data?.Description"></span>
                                    </ng-template>                                                        
                                </p-column>                                
                            </p-dataTable>      
                        </div>
                    </div>
                </div>
                `
})

export class ModelListComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private modelGroup: string;
    private models: Model[] = [];
    private selected: Model;
    
    constructor(
                private route: ActivatedRoute,
                private router: Router,
                rightSidebarService: RightSidebarService,
                protected titleService: Title,
                protected headerBreadcrumbService: HeaderBreadcrumbService,
                protected modelsService: ModelsService) {
        super();
        this.rightSidebarService = rightSidebarService;
        this.setObjectInfo('TaxonomyType', -1);
        this.setCommonRightSideBar(true);

        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/TaxonomyType/${this.selected.ID}`
            });
        }

        if (this.ownershipSidebar) {
            this.ownershipSidebar.hasDynamicUrl = true;
            this.ownershipSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/ownership/TaxonomyType/${this.selected.ID}`
            });
        }
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.modelGroup = params['group'];

            this.headerBreadcrumbService.clearCurrentObjectInfo();                      
            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Models', this.modelGroup ? `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION}` : undefined));

            if (this.modelGroup) {
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.modelGroup));
            }

            this.setBrowserTitle(this.titleService, `${this.modelGroup ? this.modelGroup + ' ' : ''}Models`);

            this.loadModels();
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();        
    }

    loadModels() {
        this.isLoading = true;
        this.modelsService.getModels()
            .then(result => {
                this.isLoading = false;
                this.models = result;                
                this.models = _.sortBy(this.models, 'TaxonomyTypeClass');                     
                if (this.models.length && this.models.length > 0) this.selected = this.models[0];
            });
    }

    showModelType(model: Model) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('TAXONOMYTYPECLASS', 0, undefined, model.TaxonomyTypeClass));
    }

    showModel(model: Model) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('TAXONOMYTYPE', model.ID));        
    }

};