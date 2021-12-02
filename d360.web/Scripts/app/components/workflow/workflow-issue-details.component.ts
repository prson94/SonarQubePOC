import {Component, Input, Output, EventEmitter, OnInit} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-workflow-issue-details',
    template: `          
            <div class="row" *ngIf="!isLoading">
                <header><d3s-tile-actions [hasAdd]="false" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
                <div class="col s12"> 
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                    <p-table #dt [value]="issues" [scrollable]="true" scrollWidth="100%" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['ActivityName','IssueTypeName','Body','Name','RaisedBy','DateStarted','EllapsedDays']" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selected">
                        <ng-template pTemplate="colgroup" let-columns>
                            <colgroup>
                                <col style="width:100px">
                                <col style="width:200px">
                                <col style="width:300px">
                                <col style="width:200px">
                                <col style="width:250px">
                                <col style="width:250px">
                                <col >
                                <col style="width:40px">
                                <col style="width:28px">
                            </colgroup>
                        </ng-template>
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'ActivityName'" style="width: 100px">
                                    Status
                                    <d3s-sortIcon [field]="'ActivityName'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'IssueTypeName'" style="width: 200px">
                                    Type
                                    <d3s-sortIcon [field]="'IssueTypeName'"></d3s-sortIcon>
                                </th>
                                <th style="width: 300px">Description</th>
                                <th style="width: 200px">Item Name</th>
                                <th [pSortableColumn]="'RaisedBy'" style="width: 250px">
                                    Reported By
                                    <d3s-sortIcon [field]="'RaisedBy'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'DateStarted'" style="width: 250px">
                                    Created
                                    <d3s-sortIcon [field]="'DateStarted'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'EllapsedDays'">
                                    Days Open
                                    <d3s-sortIcon [field]="'EllapsedDays'"></d3s-sortIcon>
                                </th>
                                <th style="width: 40px"></th>
                                <th style="width: 28px"></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th><d3s-column-filter [field]="'ActivityName'" [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'IssueTypeName'" [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'Body'" [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'RaisedBy'" [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'DateStarted'" [datatype]="'text'"></d3s-column-filter></th>
                                <th><d3s-column-filter [field]="'EllapsedDays'" [datatype]="'text'"></d3s-column-filter></th>
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr (dblclick)="openIssue(item);" [pSelectableRow]="item">
                                <td>
                                    <a (click)="openIssue(item)" *ngIf="item.WorkflowItemID > 0">{{item.ActivityName}}</a>
                                    <span *ngIf="!item.WorkflowItemID || item.WorkflowItemID <= 0">{{item.ActivityName}}</span>
                                </td>
                                <td>{{item.IssueTypeName}}</td>
                                <td>
                                    <span *ngIf="item.Body" [innerHtml]="item?.Body"></span>
                                </td>
                                <td>
                                    <d3s-preview-tooltip [objectType]="item.Object" [objectId]="item.ObjectID">{{item.Name}}</d3s-preview-tooltip>
                                </td>
                                <td>{{item.RaisedBy}}</td>
                                <td>
                                    <span>{{item.DateStarted | date: 'shortDate'}}</span>
                                </td>
                                <td>{{item.EllapsedDays}}</td>
                                <td>
                                    <div class="RowTools" *ngIf="item.Activity > 0">
                                        <a style="cursor:pointer;" (click)="openIssue(item)"><i class="fa fa-check-circle-o"></i></a>
                                    </div>
                                </td>
                                <td>
                                    <div class="RowTools">
                                        <d3s-preview-tooltip objectType="Issue" [objectId]="item.IssueID" icon="info"></d3s-preview-tooltip>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>
                </div>
            </div>                                    
        `,
    providers: [WorkflowService]
})

export class WorkflowIssueDetailsComponent extends BaseComponent implements OnInit {
    private issues: any[] = [];
    private selected: any;    

    @Input() objectID: number = 0;
    @Input() objectType: string;
        
    @Output() countsChanged = new EventEmitter();

    constructor(
        protected settingsService: CompanySettingsService,
        private workflowService: WorkflowService,
        protected router: Router) {
        super(settingsService);
    }

    ngOnInit() {
        this.loadIssues();
    }

    private loadIssues() {
        this.isLoading = true;

        this.workflowService.getIssues(this.objectID, this.objectType)
            .subscribe(result => {
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