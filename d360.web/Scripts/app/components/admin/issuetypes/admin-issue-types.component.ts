import { Component } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { WorkflowIssueType } from '../../../models/workflow.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { WorkflowService } from '../../../services/workflow.service';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component'
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-issue-types',
    template: `
                <div class="row">
                    <div class="col l6 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Action Types
                                <d3s-tile-actions [hasAdd]="true" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" (addClick)="showAdd()"></d3s-tile-actions>                            
                            </header>  
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading && !showEditor && !showDelete">
                                <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="issueTypes" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showEditor=true;" >
                                    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                                    <p-column field="Name" header="Name" sortable="true"  [filter]="!showSimpleFilter"></p-column>
                                    <p-column field="Description" header="Description" sortable="false"  [filter]="!showSimpleFilter">
                                        <ng-template let-issueType="rowData"  pTemplate type="body">
                                            <span [innerHtml]="issueType.Description"></span>
                                        </ng-template>
                                    </p-column>
                                    <p-column [style]="{width:'40px'}">
                                        <ng-template let-issueType="rowData"  pTemplate type="body">
                                            <div class="RowTools" *ngIf="!issueType.IsSystem">
                                                <a style="cursor:pointer;" (click)="selected=issueType;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                            </div>
                                        </ng-template>
                                    </p-column>                            
                                    <p-column  [style]="{width:'40px'}">
                                        <ng-template let-issueType="rowData" pTemplate type="body">
                                            <div class="RowTools" *ngIf="!issueType.IsSystem">
                                                <a style="cursor:pointer;" (click)="selected=issueType;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                            </div>
                                        </ng-template>
                                    </p-column>    
                                </p-dataTable>      
                            </span>
                            <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'IssueType'" [title]="'Action Type'" [selection]="selected" (saveClick)="saveIssueType($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>     
                            <d3s-delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemId]="selected?.ID"
                                [method]="'callback'"
                                [prompt]="'Are you sure you want to delete the action type [' + [selected?.Name] + ']?'"                                         
                                (onCancel)="showDelete=false;"
                            ></d3s-delete-form>        
                        </div>
                    </div>               
                    <div class="col l6 s12" *ngIf="!showEditor && !showDelete && selected">                        
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                                              
                                    <d3s-field-definition-tile [objectType]="'IssueType'" [objectID]="selected?.ID" ></d3s-field-definition-tile>     
                                </div>
                            </div>
                        </div>                        
                    <div>
                </div>  
                `,
    providers: [WorkflowService],
})

export class AdminIssueTypesComponent extends AdminBaseComponent {
    issueTypes: WorkflowIssueType[] = [];
    selected: WorkflowIssueType;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;

    constructor(rightSidebarService: RightSidebarService, private workflowService: WorkflowService, protected messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Action Types";
        this.setCommonItems();
        this.theDeleteCallback = this.deleteIssueType.bind(this);
        this.setCommonRightSideBar(true);
        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/IssueType/${this.selected.ID}`
            });
        }
    }

    ngOnInit() {
        this.load();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private deleteIssueType(id: number) {
        this.isLoading = true;
        this.workflowService.deleteWorkflowIssueType(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    this.issueTypes = this.issueTypes.filter(x => x.ID != id);
                }
                this.isLoading = false;
                this.showDelete = false;
            });
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getAdminWorkflowIssueTypes()
            .then(result => {
                this.issueTypes = result;
                this.selected = this.issueTypes.length > 0 ? this.issueTypes[0] : null;
                this.isLoading = false;
            });
    }

    private showAdd() {
        this.selected = null;
        this.showEditor = true;
    }

    private saveIssueType(event) {
        this.workflowService.saveIssueType(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    if (event.item.ID == undefined) {
                        event.item.ID = Number(result.id);
                        this.issueTypes[this.issueTypes.length] = event.item;
                    }
                    else {
                        let index = this.issueTypes.findIndex(x => x.ID == event.item.ID);
                        if (index >= 0 && index < this.issueTypes.length )
                            this.issueTypes[index] = event.item;
                    }
                    this.selected = event.item;
                }
                this.showEditor = false;                
            });
    }

    private closeEditor() {
        this.showEditor = false;
        if (!this.selected) this.selected = this.issueTypes.length > 0 ? this.issueTypes[0] : null;
    }
};
