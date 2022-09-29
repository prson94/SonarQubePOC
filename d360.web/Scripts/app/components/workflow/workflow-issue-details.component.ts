import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
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
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" i18n-placeholder placeholder="Search..." class="grid-simple-filter">
                    <p-table #dt [value]="issues" [scrollable]="true" scrollWidth="100%" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['ActivityName','IssueTypeName','Body','Name','RaisedBy','DateStarted','EllapsedDays']" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'ActivityName'" style="flex: 0 0 100px">
                                    <ng-container i18n>Status</ng-container>
                                    <d3s-sortIcon [field]="'ActivityName'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'IssueTypeName'" style="flex: 0 0 200px">
                                    <ng-container i18n>Type</ng-container>
                                    <d3s-sortIcon [field]="'IssueTypeName'"></d3s-sortIcon>
                                </th>
                                <th style="flex: 0 0 300px">Description</th>
                                <th style="flex: 0 0 200px">Item Name</th>
                                <th [pSortableColumn]="'RaisedBy'" style="flex: 0 0 250px">
                                    <ng-container i18n>Reported By</ng-container>
                                    <d3s-sortIcon [field]="'RaisedBy'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'DateStarted'" style="flex: 0 0 250px">
                                    <ng-container i18n>Created</ng-container>
                                    <d3s-sortIcon [field]="'DateStarted'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'EllapsedDays'" style="min-width:100px">
                                    <ng-container i18n>Days Open</ng-container>
                                    <d3s-sortIcon [field]="'EllapsedDays'"></d3s-sortIcon>
                                </th>
                                <th style="flex: 0 0 40px"></th>
                                <th style="flex: 0 0 28px"></th>
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
                                <td style="flex: 0 0 100px">
                                    <a (click)="openIssue(item)" *ngIf="item.WorkflowItemID > 0">{{item.ActivityName}}</a>
                                    <span *ngIf="!item.WorkflowItemID || item.WorkflowItemID <= 0">{{item.ActivityName}}</span>
                                </td>
                                <td style="flex: 0 0 200px">{{item.IssueTypeName}}</td>
                                <td style="flex: 0 0 300px">
                                    <span *ngIf="item.Body" [innerHtml]="item?.Body"></span>
                                </td>
                                <td style="flex: 0 0 200px">
                                    <d3s-preview-tooltip [objectType]="item.Object" [objectId]="item.ObjectID">{{item.Name}}</d3s-preview-tooltip>
                                </td>
                                <td style="flex: 0 0 250px">{{item.RaisedBy}}</td>
                                <td style="flex: 0 0 250px">
                                    <span>{{item.DateStarted | date: 'shortDate'}}</span>
                                </td>
                                <td style="min-width:100px">{{item.EllapsedDays}}</td>
                                <td style="flex: 0 0 40px">
                                    <div class="RowTools" *ngIf="item.Activity > 0">
                                        <a style="cursor:pointer;" (click)="openIssue(item)"><i class="fa fa-check-circle-o"></i></a>
                                    </div>
                                </td>
                                <td style="flex: 0 0 28px">
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
	@Input() uid: string;

    private issues: any[] = [];
    private selected: any;


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

		this.workflowService.getIssuesByAssetUid(this.uid)
            .subscribe((result) => {
                this.issues = result;
                if (this.issues.length && this.issues.length > 0) {this.selected = this.issues[0];}
                this.isLoading = false;
            });
    }

    private openIssue(issue) {
        if (issue.WorkflowItemID > 0)
            {this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_V2_VIEW_STATUS}/${issue.WorkflowItemID}`);}
        else
            {this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_VIEW_ITEM}/3/${issue.WorkflowID}`);}
    }
}