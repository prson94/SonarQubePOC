import {Component, Input, Output, EventEmitter, OnInit} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/index';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-issue-details',
    template: `            
            <div class="row" *ngIf="!isLoading && issues.length > 0">
                <header>Open Issues<d3s-tile-actions [hasAdd]="false" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
                <div class="col s12"> 
                    <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                       
                    <p-dataTable #dt [globalFilter]="gb" scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="defaultPagingOptions" [value]="issues" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [(selection)]="selected" (onRowDblclick)="openIssue($event.data);" >
                        <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                        <p-column field="ActivityName" header="Status" sortable="custom" (sortFunction)="columnSort($event)" [style]="{'width':'250px'}" [filter]="!showSimpleFilter">
                            <template let-col let-data="rowData" pTemplate type="body">
                                <d3s-tooltip *ngIf="data.Activity <= 0" objectType="WorkflowTypeRelation" [objectId]="data.WorkflowID" tooltipType="preview">{{data.ActivityName}}</d3s-tooltip>
                                <d3s-tooltip *ngIf="data.Activity > 0" objectType="WorkflowTypeRelation" [objectId]="data.WorkflowID" tooltipType="preview"><a (click)="openIssue(data)">{{data.ActivityName}}</a></d3s-tooltip>
                            </template>
                        </p-column>
                        <p-column field="CriticalityName" header="Criticality" sortable="true" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="IssueTypeName" header="Type" sortable="true" [style]="{'width':'150px'}" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="Issue" header="Issue" [sortable]="false" [style]="{'width':'250px'}" [filter]="!showSimpleFilter">
                            <template let-col let-issue="rowData" pTemplate type="body">
                                <span [innerHtml]="issue?.Issue"></span>
                            </template>
                        </p-column>             
                        <p-column field="ObjectName" header="Item Name">
                            <template let-col let-issue="rowData" pTemplate type="body">
                                <d3s-tooltip [objectType]="issue.Object" [objectId]="issue.ObjectID" tooltipType="preview">{{issue.ObjectName}}</d3s-tooltip>
                            </template>
                        </p-column>           
                        <p-column field="ResourceName" header="Reported By" sortable="custom" (sortFunction)="columnSort($event)"  [style]="{'width':'250px'}" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="DateStarted" header="Created" sortable="custom" (sortFunction)="columnSort($event)"  [style]="{'width':'250px'}" [filter]="!showSimpleFilter">
                            <template let-col let-data="rowData" pTemplate type="body">
                                <span>{{data.DateStarted | date: 'shortDate'}}</span>
                            </template>
                        </p-column>                        
                        <p-column field="EllapsedDays" header="Days Open" sortable="true" [filter]="!showSimpleFilter"></p-column>
                        <p-column  *ngIf="hasCertifyButton" [style]="{width:'40px'}">
                            <template let-issue="rowData" pTemplate type="body">
                                <div class="RowTools" *ngIf="issue.Activity > 0">                                
                                    <a style="cursor:pointer;" (click)="openIssue(issue)"><i class="fa fa-check-circle-o"></i></a>                                    
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
                </div>
            </div>            
            <div style="min-height:100px" *ngIf="!isLoading && issues.length == 0">
                <h4 *ngIf="objectName">No issues currently exist for <b>{{objectName}}</b>.</h4>
                <h4 *ngIf="!objectName">No issues assigned.</h4>
            </div>
            <div style="padding:10px">
                <button *ngIf="hasCloseButton" pButton type="button" (click)="close.emit();" label="Close" style="width: 150px;"></button>
            </div>  
            
        `,
    providers: [WorkflowService]
})

export class WorkflowIssueDetailsComponent extends BaseComponent implements OnInit {
    private issues: any[] = [];
    private selected: any;
    private loaded: boolean = false;    
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;
    @Input() hasCloseButton: boolean = false;
    @Input() hasCertifyButton: boolean = false;

    @Output() close = new EventEmitter();
    @Output() countsChanged = new EventEmitter();

    constructor(private workflowService: WorkflowService, protected router: Router) {
        super();
    }

    ngOnInit() {
        if (!this.loaded)
            this.loadIssues();
    }

    private loadIssues() {
        this.isLoading = true;
        this.workflowService.getIssues(this.objectID, this.objectType)
            .then(result => {
                this.issues = result;
                if (this.issues.length && this.issues.length > 0) this.selected = this.issues[0];
                this.isLoading = false;
                this.loaded = true;
            });
    }
       

    private columnSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.issues = _.orderBy(this.issues, [item => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);
    }

    private openIssue(issue) {
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_VIEW_ITEM}/3/${issue.WorkflowID}`);
    }
}