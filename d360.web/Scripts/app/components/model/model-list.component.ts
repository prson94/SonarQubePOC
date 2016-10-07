import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, ModelsService, RightSidebarService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Model } from '../../models/model.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-model-list',
    providers: [ModelsService],    
    template: `                 
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="selected?.ID" [objectName]="selected?.Name" [objectType]="'TaxonomyTypeClass'"></d3s-audit>                
                        <div class="row" *ngIf="!isLoading && isOwnershipVisible">
                            <div class="col s12">
                                <div class="tile tile-detail">   
                                    <d3s-people-responsibilities-tile [objectID]="selected?.ID" [objectType]="'TaxonomyTypeClass'" [title]="'Ownership of ' + selected?.Name"></d3s-people-responsibilities-tile>
                                </div>
                            </div>
                        </div>
                        <div class="tile tile-detail" *ngIf="!isLoading && !isAuditVisible && !isOwnershipVisible">                            
                            <header>{{modelGroup}} Models
                                <d3s-tile-actions [hasAdd]="false" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>         
                            <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                                                                   
                            <p-dataTable #dt [globalFilter]="gb"  [value]="models | modelType: modelGroup" scrollable="true" scrollWidth="100%" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showModel();" >
                                <p-column field="Name" header="Name" [sortable]="true" [style]="{width:'200px'}" [filter]="!showSimpleFilter"></p-column>                                                                                                                        
                                <p-column field="TaxonomyTypeClass" [hidden]="modelGroup" header="Classification" [sortable]="true" [style]="{width:'200px'}"  [filter]="!showSimpleFilter"></p-column>
                                <p-column field="Description" header="Description" [sortable]="true" [style]="{width:'500px'}"  [filter]="!showSimpleFilter">
                                    <template let-col let-data="rowData" pTemplate type="body">
                                        <div [innerHtml]="data?.Description"></div>
                                    </template>                                                        
                                </p-column>
                                <p-column field="MaximumDepth" header="Max Depth" [sortable]="true" [style]="{width:'100px'}"  [filter]="!showSimpleFilter"></p-column>                                
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
        super(rightSidebarService);
        this.setCommonRightSideBar(true, true);
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
                if (this.models.length && this.models.length > 0) this.selected = this.models[0];
            });
    }

    showModel() {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${this.selected.ID}/structure`)
    }

};