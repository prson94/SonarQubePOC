import { Component, OnInit, OnChanges, Input, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ObjectDetailService } from '../../services/object-detail.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { GridFilterExpression, GridFilterFieldType } from '../../models/grid-definition.model';

@Component({
    selector: 'd3s-monitor',
    template: ` 
<p-tabView (onChange)="tabClick($event)" [activeIndex]="activeIndex">
    <p-tabPanel header="Items">
        <ng-template pTemplate="content">
            <div class="row">
                <d3s-workflow-monitor *ngIf="filtersLoaded" [predefinedFilters]="predefinedFilters"></d3s-workflow-monitor>
            </div>
        </ng-template>
    </p-tabPanel>
    <p-tabPanel header="Monitor">
        <ng-template pTemplate="content">
            <div class="row">
                <div class="col s12" [class.m6]="!expandRow">   
                    <d3s-monitor-workflow-version 
                        (onFilterChanged)="onFilterChanged($event)"
                        [selectAll]="selectAll"
                        [selectedWorkflowTypes]="selectedWorkflowTypes" 
                        (onMonitorListChanged)="onMonitorListChanged($event)" 
                        [objectType]="objectType" 
                        [objectId]="objectId" 
                        (onMonitorFilterTypesChanged)="onMonitorFilterTypesChanged($event)"
                        (onMonitorListLoadCompleted)="loadComplete($event)">
                    </d3s-monitor-workflow-version>
                </div>
                <div class="col s12 m6">
                    <div class="row">
                        <div class="col s12">
                            <div class="tile tile-detail" *ngIf="selectedWorkflowType != null">                                              
                                <object-detail [objectType]="'Monitor'" [objectID]="selectedWorkflowType?.VersionID" ></object-detail>
                            </div>
                        </div>
                    </div>
                    <div class="row">  
                        <div class="col s12">
                            <d3s-workflow-diagram *ngIf="selectedWorkflowType != null"
                                [id]="selectedWorkflowType?.TypeID" 
                                [version]="selectedWorkflowType?.Version" 
                                [filteredObject]="objectType"
                                [filteredObjectId]="objectId"
                                [readonly]="true" 
                                [hasHeader]="false"
                                [selectedStepId]="selectedWorkflowItem?.VersionStepID"
                                [monitorView]="true">
                            </d3s-workflow-diagram>
                        </div>
                    </div>
                </div>
            </div>
        </ng-template>
    </p-tabPanel>
</p-tabView>

              `,
    providers: [ObjectDetailService],
})

export class MonitorComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() objectType: string;
    @Input() objectId: number;
    selectedWorkflowTypes: any[];
    selectedWorkflowType: any = {};
    selectedWorkflowItem: any;
    selectedWorkflowItemDetail: any;
    selectAll: boolean = true;
    isFiltered: boolean = false;
    filteredTypes: any[];
    predefinedFilters: GridFilterExpression[] = [];
    filtersLoaded = false;
    expandRow: boolean = false;

    sub: any;
    querySub: any;
    type: number;
    tabs: any[] = [
        { key: 'items' },
        { key: 'monitor' },
    ];
    
    activeIndex = 0;
    activeTab = this.tabs[this.activeIndex];

    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected router: Router,
        protected route: ActivatedRoute,
        private objectDetailService: ObjectDetailService,
        rightSidebarService: RightSidebarService) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        
        this.predefinedFilters = [];
        this.isLoading = true;
        this.sub = this.route.params.subscribe(params => {
            if (params['id'] != null) {
                this.selectedWorkflowTypes = [];
                this.selectedWorkflowTypes.push(+params['id']);
                this.selectAll = false;
            }

        });

        this.querySub = this.route.queryParams.subscribe(params => {
            if (params['tab'] != null) {
                let i = this.tabs.findIndex(t => t.key == params['tab'].toLowerCase());
                if (i > -1) {
                    this.activeIndex = i;
                    this.activeTab = this.tabs[i];
                }
            }
        });

        if (this.objectType != null && this.objectId != null && this.selectedWorkflowTypes == null) {
            this.selectAll = true;
            this.isFiltered = true;

            this.objectDetailService.getObject(this.objectId, this.objectType)
                .then(o => {
                    let assetFilter = new GridFilterExpression();
                    assetFilter.field = "Asset";
                    assetFilter.fieldtype = GridFilterFieldType.Normal;
                    assetFilter.value = o.DisplayValue;
                    assetFilter.condition = "EQUAL";
                    this.predefinedFilters.push(assetFilter);
                    this.filtersLoaded = true;
                });
        }

        if (!this.isFiltered) {
            this.filtersLoaded = true;
            this.clearSidebar();
            this.setBrowserTitle(this.titleService, 'Workflow Monitor');

            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.clearCurrentObjectInfo();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Workflow Monitor', SiteUrlHelpers.SITE_URL_MONITOR_ROOT));
        }


        this.isLoading = false;
    }

    ngOnDestroy() {
        //this.clearSidebar();
        this.sub.unsubscribe();
        this.querySub.unsubscribe();
    }

    loadComplete(e: any) {
        this.expandRow = e.rows == 0;
    }

    onMonitorListChanged($event) {
        this.selectedWorkflowType = $event ? $event : {};
    }

    onFilterChanged($event) {
        this.selectedWorkflowTypes = $event ? $event : [];
    }

    onMonitorFilterTypesChanged($event) {
        this.filteredTypes = $event ? $event : [];
    }
}