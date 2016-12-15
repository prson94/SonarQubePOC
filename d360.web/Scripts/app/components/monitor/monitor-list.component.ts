import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, WorkflowService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Issue, IssueDetail } from '../../models/workflow.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-monitor-list',
    template: ` 
                <div class="tile tile-detail">
                    <header>Monitor
                        <d3s-tile-actions [hasAdd]="false" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" [hasExport]="true" (exportClick)="export()"></d3s-tile-actions>                            
                    </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                        <p-dataTable #dt [globalFilter]="gb" [value]="issues" selectionMode="single" [(selection)]="selected" scrollable="true" scrollWidth="100%" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions">                                                
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column field="ActivityName" header="Status" sortable="custom" (sortFunction)="columnSort($event)" [style]="{'width':'250px'}" [filter]="!showSimpleFilter">
                                <template let-col let-data="rowData" pTemplate type="body">
                                    <d3s-tooltip  *ngIf="!data.AllowAction" objectType="WorkflowTypeRelation" [objectId]="data.WorkflowID" tooltipType="preview">{{data.ActivityName}}</d3s-tooltip>                                    
                                    <d3s-tooltip *ngIf="data.AllowAction" objectType="WorkflowTypeRelation" [objectId]="data.WorkflowID" tooltipType="preview"><a (click)="handleIssue(data)">{{data.ActivityName}}</a></d3s-tooltip>
                                </template>
                            </p-column>
                            <p-column field="Criticality" header="Criticality" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="IssueTypeName" header="Type" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="Name" header="Item Name" [sortable]="true" [filter]="!showSimpleFilter">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <d3s-tooltip [objectType]="item.Object" [objectId]="item.ObjectID" [tooltipType]="'Preview'">{{item.Name}}</d3s-tooltip>
                                </template>
                            </p-column>
                            <p-column field="Object" header="Type" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="Issue" header="Issue Description" [sortable]="false" [filter]="!showSimpleFilter">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <span [innerHtml]="item.Issue"></span>
                                </template>
                            </p-column>
                            <p-column field="RaisedBy" header="Created By" [sortable]="true" [filter]="!showSimpleFilter">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <d3s-tooltip [objectType]="'Resource'" [objectId]="item.RaisedByResourceID" [tooltipType]="'Preview'">{{item.RaisedBy}}</d3s-tooltip>
                                </template>
                            </p-column>
                            <p-column field="DateStarted" header="Created On" [sortable]="true" [filter]="!showSimpleFilter">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <span>{{item.DateStarted | date : 'shortDate'}}</span>
                                </template>
                            </p-column>
                            <p-column field="DateCompleted" header="Closed On" [sortable]="true" [filter]="!showSimpleFilter">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <span>{{item.DateCompleted | date : 'shortDate'}}</span>
                                </template>
                            </p-column>                            
                            <p-column field="EllapsedDays" header="Days Open" [sortable]="true" [filter]="!showSimpleFilter"></p-column>   
                            <p-column field="Notes" header="Closing Notes" [sortable]="true" [filter]="!showSimpleFilter"></p-column>    
                            <p-column [style]="{width:'28px'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools">
                                        <i class="fa fa-check-circle-o" style="pointer:cursor" *ngIf="item.AllowAction" (click)="handleIssue(item)"></i>
                                    </div>
                                </template>
                            </p-column>
                            <p-column [style]="{width:'28px'}">
                                <template let-data="rowData" pTemplate type="body">
                                    <div class="RowTools">
                                        <d3s-tooltip objectType="Issue" [objectId]="data.IssueID" tooltipType="preview"><i class="fa fa-info" aria-hidden="true"></i></d3s-tooltip>
                                    </div>
                                </template>
                            </p-column>
                        </p-dataTable>        
                    </span>
                </div>
              `,
    providers: [WorkflowService],      
})

export class MonitorListComponent extends BaseComponent implements OnInit {

    private issues: IssueDetail[] = [];
    private selected: IssueDetail;

    private showEditor: boolean = false;

    constructor(protected workflowService: WorkflowService, protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService, protected router: Router) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Monitor');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Monitor', SiteUrlHelpers.SITE_URL_MONITOR_ROOT));

        this.load();
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getAllIssueDetails()
            .then(result => {
                this.issues = result;
                this.isLoading = false;                
            });
    }

    private handleIssue(issue: IssueDetail) {
        this.showEditor = true;
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_VIEW_ITEM}/3/${issue.WorkflowID}`);
    }

    private export() {
        this.workflowService.exportAllIssueDetails();
    }

    private columnSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.issues = _.orderBy(this.issues, [item => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);
    }
};