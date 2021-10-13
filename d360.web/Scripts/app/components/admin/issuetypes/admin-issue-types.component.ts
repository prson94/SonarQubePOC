import { Component } from '@angular/core';
import { WorkflowIssueType } from '../../../models/workflow.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { WorkflowService } from '../../../services/workflow.service';
import { AdminBaseComponent } from '../admin-base.component'
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-issue-types',
    template: `
                <div class="row">
                    <div class="col l6 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor && !showDelete">Action Types
                                <d3s-tile-actions [hasAdd]="true" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" (addClick)="showAdd()"></d3s-tile-actions>                            
                            </header>  
                            <d3s-loading [isLoading]="isLoading && !showDelete"></d3s-loading>
                            <span *ngIf="!isLoading && !showEditor && !showDelete">
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                                <p-table #dt [value]="issueTypes" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name','Description']" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="20" [(selection)]="selected" (onRowSelect)="selectedItemChange()">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'Name'">
                                                Name
                                                <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                            </th>
                                            <th>Description</th>
                                            <th style="width: 30px"></th>
                                            <th style="width: 30px"></th>
                                            <th style="width: 30px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'Description'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th></th>
                                            <th></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-item>
                                        <tr (dblclick)="selected=item;showEditor=true;" [pSelectableRow]="item">
                                            <td>{{item.Name}}</td>
                                            <td>
                                                <span [innerHtml]="item.Description"></span>
                                            </td>
                                            <td>
                                                <div class="RowTools" *ngIf="!item.IsSystem">
                                                    <d3s-preview-tooltip objectType="IssueType"[objectId]="item.ID" icon="info"></d3s-preview-tooltip>
                                                </div>
                                            </td>
                                            <td>
                                                <div class="RowTools" *ngIf="!item.IsSystem">
                                                    <a style="cursor:pointer;" (click)="selected=item;OnEdit();"><i class="fa fa-pencil"></i></a>
                                                </div>
                                            </td>
                                            <td>
                                                <div class="RowTools" *ngIf="!item.IsSystem">
                                                    <a style="cursor:pointer;" (click)="selected=item;OnDelete();"><i class="fa fa-trash-o"></i></a>
                                                </div>
                                            </td>
                                        </tr>
                                    </ng-template>
                                    <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                        <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                    </ng-template>
                                </p-table>  
                            </span>
                            <d3s-dynamic-editor *ngIf="showEditor" 
                                        [objectID]="selected?.ID"
                                        [objectType]="'IssueType'"                                         
                                        [title]="'Action Type'" 
                                        [selection]="selected" 
                                        (saveClick)="saveIssueType($event)" 
                                        (closeClick)="closeEditor()"                                         
                                        [objectTypeUid]="selected?.Uid">
                            </d3s-dynamic-editor>     
                            <d3s-delete-form *ngIf="showDelete"
                                [callback]="theDeleteCallback"
                                [itemUid]="selected?.Uid"
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
                                    <d3s-field-definition-tile [objectType]="'IssueType'" [objectID]="selected?.ID" 
                                        [showIsListable]="false" 
                                        [showIsPartOfKey]="false"
                                        [actionTypeUid]="selected?.Uid"
                                        [objectName]="selectedRow?.Name"
                                    ></d3s-field-definition-tile>     
                                </div>
                            </div>
                        </div> 
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail"> 
                                    <d3s-admin-issue-type-allocation [issueTypeUid]="selected?.Uid"></d3s-admin-issue-type-allocation>
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
    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        protected messagesService: MessagesObservableService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        titleService: Title,
        private workflowService: WorkflowService) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_Actions;
        this.adminHeading = StringConstants.Section_Actions;
        this.tabTitle = 'Action Types';
        this.setCommonItems();
        this.theDeleteCallback = this.deleteIssueType.bind(this);
    }

    ngOnInit() {
        this.load();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }   

    private deleteIssueType(uid: string) {
        this.isLoading = true;
        this.workflowService.deleteWorkflowIssueType(uid)
            .subscribe(result => {
                if (result) {
                    this.showMessageForApiResponse(this.messagesService, result);
                    if (result.Success) {
                        this.issueTypes = this.issueTypes.filter(x => x.Uid != uid);
                    }
                    this.selected = this.issueTypes.length > 0 ? this.issueTypes[0] : null;
                }
                this.isLoading = false;
                this.showDelete = false;
            });
    }

    selectedItemChange(callback: Function = null) {        
        if (this.selected) {
            if (!this.selected.ID) {
                this.workflowService.getIssueByUID(this.selected.Uid)
                    .subscribe(result => {
                        this.selected.ID = result.ID;
                        this.isLoading = false;
                        this.buildSecondaryNavigationForObject(result.ID, 'IssueType');
                        if (callback) {
                            callback();
                        }
                    });
            } else {
                this.buildSecondaryNavigationForObject(this.selected.ID, 'IssueType');
                if (callback) {
                    callback();
                }
            }
        }
        
    }
    private load() {
        this.isLoading = true;
        this.workflowService.getAdminWorkflowIssueTypes()
            .subscribe(result => {
                this.issueTypes = result.sort((a, b) => a.Name.localeCompare(b.Name));
                this.selected = this.issueTypes.length > 0 ? this.issueTypes[0] : null;
                this.selectedItemChange();
                this.isLoading = false;
            });
    }

    private showAdd() {
        this.selected = null;
        this.showEditor = true;
    }

    private saveIssueType(event) {
        this.isLoading = true;

        this.workflowService.saveIssueType(event.item)
            .subscribe(result => {
                this.isLoading = false;
                this.showMessageForApiResponse(this.messagesService, result);
                if (result.Success) {
                    this.load();                    
                }
                this.showEditor = false;
            });
    }

    private closeEditor() {
        this.showEditor = false;
        if (!this.selected) this.selected = this.issueTypes.length > 0 ? this.issueTypes[0] : null;
    }

    private OnEdit() {
        this.selectedItemChange(() => this.showEditor = true);
    }

    private OnDelete() {
        this.selectedItemChange(() => this.showDelete = true);
    }
}
