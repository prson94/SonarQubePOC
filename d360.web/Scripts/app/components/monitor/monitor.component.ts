import { Component, OnInit, OnChanges, Input, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { GridFilterExpression, GridFilterFieldType } from '../../models/grid-definition.model';
import { AuthenticationService } from '../../services/authentication.service';
import { WorkflowMonitorListComponent } from '../workflowmonitor/worflowmonitor-list.component';

@Component({
    selector: 'd3s-monitor',
    template: ` 

<div class="row">
    <div class="col s12 m6">
        <div class="tile tile-detail">
            <p-tabView (onChange)="tabClick($event)" [activeIndex]="activeIndex">
                <p-tabPanel header="Workflow Items">
                    <ng-template pTemplate="content">
                        <div class="row">
                            <div *ngIf="!isLoading">
                                <d3s-workflowmonitor-list [showHeader]="false" (selectionChange)="listChange($event)" [predefinedFilters]="predefinedFilters"></d3s-workflowmonitor-list>  
                            </div>
                        </div>
                    </ng-template>
                </p-tabPanel>
                <p-tabPanel header="Workflow Versions">
                    <ng-template pTemplate="content">
                        <div class="row">
                            <d3s-monitor-workflow-version 
                                [showHeader]="false"
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
                    </ng-template>
                </p-tabPanel>
            </p-tabView>
        </div>     
    </div>
    <div class="col s12 m6">
        <ng-container *ngIf="tabIsLoaded('items')">
            <div [hidden]="!tabIsActive('items')">
                <div class="tile tile-detail" [hidden]="itemId == null">
                    <d3s-workflow-monitor-step-list [itemId]="itemId" (selectionChange)="stepChange($event)"></d3s-workflow-monitor-step-list>
                </div>
                <div class="tile tile-detail" [hidden]="!detailVisible">
                    <d3s-workflow-monitor-step-details [itemStepId]="itemStepId" [(visible)]="detailVisible"></d3s-workflow-monitor-step-details>
                </div>
                <div class="tile tile-detail" [hidden]="!isAdmin">
                    <header>Delete Workflow Items</header>
                    <div class="row" style="padding-left:10px;">
                        <p>You have selected {{seletedCount}} workflow items.</p>
                        <p>The deletion of workflow items cannot be undone.</p>
                        <div>
                            <button pButton type="button" (click)="deleteItems()" label="Delete {{seletedCount}} workflow items" [disabled]="seletedCount < 1"></button>
                        </div>
                        <p>Press the {{getMetaKey()}}-key to select multiple items.</p>
                    </div>
                </div>
            </div>
        </ng-container>
        <ng-container *ngIf="tabIsLoaded('monitor')">
            <div [hidden]="!tabIsActive('monitor')">
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
        </ng-container>
    </div>
</div>
              `,
    providers: [],
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

    itemStepId: number = null;
    itemId: number = null;
    detailVisible = false;
    private isAdmin: boolean = false;
    seletedCount: number = 0;
    @ViewChild(WorkflowMonitorListComponent)
    private workflowListComponent: WorkflowMonitorListComponent;

    sub: any;
    querySub: any;
    type: number;
    tabs: any[] = [
        { key: 'items', loaded: false },
        { key: 'monitor', loaded: false },
    ];
    
    activeIndex = 0;
    activeTab = this.tabs[this.activeIndex];

    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected router: Router,
        protected route: ActivatedRoute,
        rightSidebarService: RightSidebarService,
        private authenticationService: AuthenticationService) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        this.isAdmin = this.authenticationService.isAdmin;
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
                    this.activeTab.loaded = true;
                }
            }
        });

        this.activeTab.loaded = true;

        if (this.objectType != null && this.objectId != null && this.selectedWorkflowTypes == null) {
            this.selectAll = true;
            this.isFiltered = true;

            let fieldPrefix = "Object";
            if (this.objectType.indexOf('Type') > -1)
                fieldPrefix += "Type";

            let assetFilter = new GridFilterExpression();
            assetFilter.field = fieldPrefix;
            assetFilter.fieldtype = GridFilterFieldType.Normal;
            assetFilter.value = this.objectType;
            assetFilter.condition = "EQUAL";
            this.predefinedFilters.push(assetFilter);

            assetFilter = new GridFilterExpression();
            assetFilter.field = fieldPrefix + "ID";
            assetFilter.fieldtype = GridFilterFieldType.Normal;
            assetFilter.value = this.objectId.toString();
            assetFilter.condition = "EQUAL";
            this.predefinedFilters.push(assetFilter);
            this.filtersLoaded = true;
        }

        if (!this.isFiltered) {
            this.filtersLoaded = true;
            this.clearSidebar();
            this.setBrowserTitle(this.titleService, 'Workflow');

            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.clearCurrentObjectInfo();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Workflow', SiteUrlHelpers.SITE_URL_MONITOR_ROOT));
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

    listChange($event) {
        if (Array.isArray($event)) {
            this.seletedCount = $event.length;
            if (this.seletedCount == 1) {
                this.itemId = $event[0].Id;
            } else {
                this.itemId = null;
                this.detailVisible = false;
            }
        } else {
            this.itemId = null;
            this.detailVisible = false;
        }
    }

    tabClick(e: any) {
        this.activeIndex = e.index;
        this.activeTab = this.tabs[e.index];
        this.activeTab.loaded = true;
        //console.log(e);
    }

    tabIsLoaded(key: string) {
        return this.tabs.find(t => t.key == key).loaded || false;
    }

    tabIsActive(key: string) {
        return this.activeTab.key == key;
    }

    stepChange($event) {
        if ($event) {
            this.itemStepId = $event.ID;
            this.detailVisible = true;
        } else {
            this.itemStepId = null;
            this.detailVisible = false;
        }

    }

    private getMetaKey() {
        if (window.navigator && window.navigator.platform.indexOf("Mac") >= 0) {
            return "\u2318";
        } else {
            return "Crtl"
        }
    }

    deleteItems() {
        this.workflowListComponent.deleteItems();
    }
}