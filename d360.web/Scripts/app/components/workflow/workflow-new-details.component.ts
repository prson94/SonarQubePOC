import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { WorkflowType, WorkflowAssignmentDetail, WorkflowAssignmentSummary } from '../../models/workflow.model';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { WorkflowService } from '../../services/workflow.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-workflow-new-detail',
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <header>Edit {{workflow?.Name}}<d3s-tile-actions [hasAdd]="false" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
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
                          
                           
                            <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                       
                            <p-dataTable #dt [globalFilter]="gb" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" pageLinks="3" [value]="items" selectionMode="single">
                                <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                                <p-column field="ObjectName" header="Name" sortable="true" [filter]="!showSimpleFilter">
                                    <ng-template let-col let-item="rowData" pTemplate type="body">
                                        <a (click)="open(item)" *ngIf="!item.IssueObject"><d3s-preview-tooltip [objectType]="item.Object" [objectId]="item.ObjectID" >{{item.ObjectName}}</d3s-preview-tooltip></a>
                                        <a (click)="open(item)" *ngIf="item.IssueObject && item.IssueObjectID"><d3s-preview-tooltip [objectType]="item.IssueObject" [objectId]="item.IssueObjectID">{{item.IssueObjectName ? item.IssueObjectName : "unknown"}}</d3s-preview-tooltip></a>
                                    </ng-template>
                                </p-column>
                                
                                <p-column field="StartedOn" header="Started On" sortable="true" [filter]="!showSimpleFilter">
                                    <ng-template let-col let-data="rowData" pTemplate type="body">
                                        <span>{{data.StartedOn | date: 'shortDate'}}</span>
                                    </ng-template>
                                </p-column>
                                <p-column field="StartedBy" header="Started By" sortable="true"></p-column>    
                               <p-column  [style]="{width:'35px'}" >
                                    <ng-template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">                                            
                                            <d3s-preview-tooltip [objectType]="item.Object" [objectId]="item.ObjectID" icon="info"></d3s-preview-tooltip>
                                        </div>
                                    </ng-template>
                                </p-column> 
                                <p-column  [style]="{width:'35px'}">
                                    <ng-template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="open(item)" title="Complete Form"><i class="fa fa-pencil-square-o"></i></a>                                    
                                        </div>
                                    </ng-template>
                                </p-column> 
                            </p-dataTable>       
                            <div style="padding:10px">
                                <button *ngIf="hasCloseButton" pButton type="button" (click)="close();" label="Close" style="width: 150px;"></button>
                            </div>  
                        </div>
                    </div>
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

    private sub: any;
    private tempWorkflowtype = WorkflowType;
    private items: WorkflowAssignmentDetail[];
    private workflow: any;

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

            this.headerBreadcrumbService.clearBreadcrumbs();    

            this.load();
        });
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getAssignedWorkflowInstancesByTypeId(this.workflowTypeId, this.resourceID,this.version,this.stepId)
            .then(res => {
                this.isLoading = false;
                this.items = res.items;
                if (this.items && this.items.length != 0) {
                    this.assignmentSummary = new WorkflowAssignmentSummary();
                    this.assignmentSummary.Version = this.version;
                    this.assignmentSummary.ObjectName = this.items[0].Object;
                    this.assignmentSummary.StepName = this.items[0].StepName;
                    this.assignmentSummary.TypeName = this.items[0].TypeName;
                }
                this.workflow = res.workflow;
            });
    }

    private close() {
        this.location.back();
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    private open(item: WorkflowAssignmentDetail) {        
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_FORM}/${this.workflowTypeId}/${item.ItemStepID}/${item.ItemID}`);
    }
};