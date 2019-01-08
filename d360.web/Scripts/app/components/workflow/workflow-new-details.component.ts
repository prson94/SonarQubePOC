import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { WorkflowType, WorkflowAssignmentDetail, WorkflowAssignmentSummary, BulkWorkflowFormModel } from '../../models/workflow.model';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { WorkflowService } from '../../services/workflow.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-workflow-new-detail',
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading && !showBulkFormEditor">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <header>{{workflow?.Name}}<d3s-tile-actions [hasAdd]="false" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
                            <div class="row" style="margin-bottom: 5px">
                                <div class="col l2 m4 s12">
                                       <span class="FieldName FieldDisplayName">Version:&nbsp;</span>
                                        <span  *ngIf="assignmentSummary" class="FieldDisplayContent">{{assignmentSummary.Version}}</span>
                                </div>
                            </div>
                            <div class="row" style="margin-bottom: 5px">
                                <div class="col l4 m6 s12">
                                       <span class="FieldName FieldDisplayName">Step:&nbsp;</span>
                                        <span *ngIf="assignmentSummary" class="FieldDisplayContent">{{assignmentSummary.StepName}}</span>
                                </div>
                            </div>
                            <div class="row" style="margin-bottom: 5px">
                                <div class="col l4 m6 s12">
                                       <span class="FieldName FieldDisplayName">Object:&nbsp;</span>
                                        <span *ngIf="assignmentSummary" class="FieldDisplayContent">{{assignmentSummary.ObjectName}}</span>
                                </div>
                            </div>
                            <div class="row" style="margin-bottom: 5px">
                                <div class="col l4 m6 s12">
                                        <span class="FieldName FieldDisplayName">Type:&nbsp;</span>
                                        <span *ngIf="assignmentSummary" class="FieldDisplayContent">{{assignmentSummary.TypeName}}</span>
                                </div>
                            </div>
                           

                            <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                            <p-table #dt [value]="items" selectionMode="multiple" [globalFilterFields]="['Name','StartedOn','StartedBy']" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="[10, 25, 50, 100, 500]" [(selection)]="selection">
                                <ng-template pTemplate="header">
                                    <tr>
                                        <th style="width: 35px"><p-tableHeaderCheckbox></p-tableHeaderCheckbox></th>
                                        <th [pSortableColumn]="'Name'">
                                            Name
                                            <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'StartedOn'">
                                            Started On
                                            <d3s-sortIcon [field]="'StartedOn'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'StartedBy'">
                                            Started By
                                            <d3s-sortIcon [field]="'StartedBy'"></d3s-sortIcon>
                                        </th>
                                        <th style="width: 35px"></th>
                                        <th style="width: 35px"></th>
                                    </tr>
                                    <tr [hidden]="showSimpleFilter">
                                        <th></th>
                                        <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                        <th><d3s-column-filter [field]="'StartedOn'" [datatype]="'text'"></d3s-column-filter></th>
                                        <th><d3s-column-filter [field]="'StartedBy'" [datatype]="'text'"></d3s-column-filter></th>
                                        <th></th>
                                        <th></th>
                                    </tr>
                                </ng-template>
                                <ng-template pTemplate="body" let-item>
                                    <tr [pSelectableRow]="item">
                                        <td><p-tableCheckbox [value]="item"></p-tableCheckbox></td>
                                        <td>
                                            <a (click)="open(item)" *ngIf="!item.IssueObject"><d3s-preview-tooltip [objectType]="item.Object" [objectId]="item.ObjectID">{{item.ObjectName}}</d3s-preview-tooltip></a>
                                            <a (click)="open(item)" *ngIf="item.IssueObject && item.IssueObjectID"><d3s-preview-tooltip [objectType]="item.IssueObject" [objectId]="item.IssueObjectID">{{item.IssueObjectName ? item.IssueObjectName : "unknown"}}</d3s-preview-tooltip></a>
                                        </td>
                                        <td>
                                            <span>{{item.StartedOn | date: 'shortDate'}}</span>
                                        </td>
                                        <td>{{item.StartedBy}}</td>
                                        <td>
                                            <div class="RowTools">
                                                <d3s-preview-tooltip [objectType]="item.Object" [objectId]="item.ObjectID" icon="info"></d3s-preview-tooltip>
                                            </div>
                                        </td>
                                        <td>
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="open(item)" title="Complete Form"><i class="fa fa-pencil-square-o"></i></a>
                                            </div>
                                        </td>
                                    </tr>
                                </ng-template>
                                <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                    <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                    <d3s-grid-selection-info
                                        [includeSelectLinks]="false"
                                        [model]="items"
                                        [selection]="selection"
                                        (onSelectAllClick)="selection = items"
                                        (onSelectNoneClick)="selection = []"
                                    >
                                    </d3s-grid-selection-info>
                                </ng-template>
                            </p-table>  
                            <div style="padding:10px">
                                <button *ngIf="hasCloseButton" pButton type="button" (click)="close();" label="Close" style="width: 150px;"></button>
                                <button pButton type="button" (click)="bulkRespond();" label="Bulk Respond" style="width: 150px;" [disabled]="selection == null || selection.length < 1 || !isMe"></button>
                            </div>  
                        </div>
                    </div>
                </div>  
                <div *ngIf="!isLoading && showBulkFormEditor">
                    <d3s-workflow-bulk-form [model]="bulkEditorModel" (onClose)="showBulkFormEditor = false;" (onComplete)="isLoading = true; close();" ></d3s-workflow-bulk-form>
                </div>
                `,
    providers: [WorkflowService]
})

export class WorkflowNewDetailComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() workflowTypeId: number;
    @Input() hasCloseButton: boolean = true;
    
    private resourceID: number = null;
    private version: number;
    private stepId: number;
    private assignmentSummary: WorkflowAssignmentSummary;
    private isMe:boolean = true;
    private sub: any;
    private tempWorkflowtype = WorkflowType;
    private items: WorkflowAssignmentDetail[];
    private workflow: any;
    private selection: WorkflowAssignmentDetail[] = [];
    private showBulkFormEditor = false;
    private bulkEditorModel: BulkWorkflowFormModel;
    private fromMail: boolean = false;

    constructor(private route: ActivatedRoute,
        private location: Location,
        private router: Router,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected workflowService: WorkflowService
    )
    {
        super();
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearCurrentObjectInfo();

        this.sub = this.route.params.subscribe(params => {
            this.isLoading = true;
            this.workflowTypeId = +params['workflowTypeId'];
            this.resourceID = +params['resourceID'];
            this.version = +params['version'];
            this.stepId = +params['stepId'];
            this.fromMail = params['fromMail'] === '1' ? true : false;

            this.isMe = this.resourceID ? this.resourceID == CurrentResourceID: true;

            this.headerBreadcrumbService.clearBreadcrumbs();    

            this.load();
        });
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getAssignedWorkflowInstancesByTypeId(this.workflowTypeId, this.resourceID, this.version, this.stepId)
            .then(res => {
                this.selection = [];
                this.items = res.items;
                this.workflow = res.workflow;
            })
            .then(() => this.workflowService.getAssignedWorkflowInstancesSummary(this.workflowTypeId, this.resourceID, this.version, this.stepId))
            .then(res => {
                    this.isLoading = false;
                    this.assignmentSummary = res.item;
         });
               
    }

    private bulkRespond() {
        if (this.selection != null) {
            if (this.selection.length >= 2) {
                console.log(this.selection.map(i => i.ItemStepID));

                this.bulkEditorModel = new BulkWorkflowFormModel();
                this.bulkEditorModel.ItemStepIDs = this.selection.map(i => i.ItemStepID);

                this.showBulkFormEditor = true;
            } else if (this.selection.length == 1) {
                this.open(this.selection[0]);
            }
        }
    }

    private close() {
        if (this.fromMail) {
            this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_HOME_ROOT}`);
        }
        this.location.back();
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    private open(item: WorkflowAssignmentDetail) {        
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_FORM}/${this.workflowTypeId}/${item.ItemStepID}/${item.ItemID}`);
    }
};