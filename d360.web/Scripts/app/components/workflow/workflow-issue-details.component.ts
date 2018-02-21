import {Component, Input, Output, EventEmitter, OnInit} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-workflow-issue-details',
    template: `          
            <div class="row" *ngIf="!isLoading && issues.length > 0">
                <header>Open Actions<d3s-tile-actions [hasAdd]="false" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
                <div class="col s12"> 
                    <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                       
                    <p-dataTable #dt [globalFilter]="gb" scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="defaultPagingOptions" [value]="issues" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [(selection)]="selected" (onRowDblclick)="openIssue($event.data);" >
                        <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                        <p-column field="ActivityName" header="Status" sortable="true" [style]="{'width':'100px'}" [filter]="!showSimpleFilter">
                            <ng-template let-col let-data="rowData" pTemplate type="body">
                                <a (click)="openIssue(data)" *ngIf="data.WorkflowItemID > 0">{{data.ActivityName}}</a>
                                <span *ngIf="!data.WorkflowItemID || data.WorkflowItemID <= 0">{{data.ActivityName}}</span>
                            </ng-template>
                        </p-column>
                        <p-column field="Criticality" header="Criticality" sortable="true" [style]="{'width':'100px'}" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="IssueTypeName" header="Type" sortable="true" [style]="{'width':'200px'}" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="Body" header="Description" [sortable]="false" [style]="{'width':'300px'}" [filter]="!showSimpleFilter">
                            <ng-template let-col let-issue="rowData" pTemplate type="body">
                                <span [innerHtml]="issue?.Body"></span>
                            </ng-template>
                        </p-column> 
                        <p-column field="Name" header="Item Name" [sortable]="false" [style]="{'width':'200px'}" [filter]="!showSimpleFilter">
                            <ng-template let-col let-issue="rowData" pTemplate type="body">                                
                                <d3s-preview-tooltip [objectType]="issue.Object" [objectId]="issue.ObjectID">{{issue.Name}}</d3s-preview-tooltip>
                            </ng-template>
                        </p-column>           
                        <p-column field="RaisedBy" header="Reported By" sortable="true" [style]="{'width':'250px'}" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="DateStarted" header="Created" sortable="true"  [style]="{'width':'250px'}" [filter]="!showSimpleFilter">
                            <ng-template let-col let-data="rowData" pTemplate type="body">
                                <span>{{data.DateStarted | date: 'shortDate'}}</span>
                            </ng-template>
                        </p-column>                        
                        <p-column field="EllapsedDays" header="Days Open" sortable="true" [filter]="!showSimpleFilter"></p-column>
                        <p-column  *ngIf="hasCertifyButton" [style]="{width:'40px'}">
                            <ng-template let-issue="rowData" pTemplate type="body">
                                <div class="RowTools" *ngIf="issue.Activity > 0">                                
                                    <a style="cursor:pointer;" (click)="openIssue(issue)"><i class="fa fa-check-circle-o"></i></a>                                    
                                </div>
                            </ng-template>
                        </p-column>    
                        <p-column [style]="{width:'28px'}">
                                <ng-template let-data="rowData" pTemplate type="body">
                                    <div class="RowTools">                                        
                                        <d3s-preview-tooltip objectType="Issue" [objectId]="data.IssueID" icon="info"></d3s-preview-tooltip>
                                    </div>
                                </ng-template>
                            </p-column>                        
                    </p-dataTable>   
                </div>
            </div>            
            <div style="min-height:100px" *ngIf="!isLoading && issues.length == 0">
                <h4 *ngIf="objectName">No actions currently exist for <b>{{objectName}}</b>.</h4>
                <h4 *ngIf="!objectName">No actions assigned.</h4>
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
        this.loadIssues();
    }

    private loadIssues() {
        this.isLoading = true;

        this.workflowService.getIssues(this.objectID, this.objectType)
                .then(result => {
                    this.issues = result;
                    if (this.issues.length && this.issues.length > 0) this.selected = this.issues[0];
                    this.isLoading = false;                    
                });        
    }
    
    private openIssue(issue) {        
        if (issue.WorkflowItemID > 0)
            this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_V2_VIEW_STATUS}/${issue.WorkflowItemID}`);
        else
            this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_VIEW_ITEM}/3/${issue.WorkflowID}`);
    }
}