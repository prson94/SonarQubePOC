import { Component, Input, OnChanges, SimpleChange, SimpleChanges } from '@angular/core';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component'
import { WorkflowService } from '../../../services/workflow.service';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from '../../../models/form.model';

@Component({
    selector: 'd3s-admin-issue-type-allocation',
    template: `
                <div class="row"> 
                <header *ngIf="formMode == FormMode.Default">Allocations
                    <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <div *ngIf="!isLoading">
                        <div [ngSwitch]="formMode">
                            <div *ngSwitchCase="FormMode.Default" class="col s12">
                                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                                <p-table #dt [value]="allocations" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['ObjectType','TypeName']" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selection">
                                    <ng-template pTemplate="header">
                                        <tr>
                                            <th [pSortableColumn]="'ObjectType'">
                                                Object Type
                                                <d3s-sortIcon [field]="'ObjectType'"></d3s-sortIcon>
                                            </th>
                                            <th [pSortableColumn]="'TypeName'">
                                                Object Name
                                                <d3s-sortIcon [field]="'TypeName'"></d3s-sortIcon>
                                            </th>
                                            <th style="width: 40px"></th>
                                        </tr>
                                        <tr [hidden]="showSimpleFilter">
                                            <th><d3s-column-filter [field]="'ObjectType'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th><d3s-column-filter [field]="'TypeName'" [datatype]="'text'"></d3s-column-filter></th>
                                            <th></th>
                                        </tr>
                                    </ng-template>
                                    <ng-template pTemplate="body" let-item>
                                        <tr [pSelectableRow]="item">
                                            <td>{{item.ObjectType}}</td>
                                            <td>{{item.TypeName}}</td>
                                            <td>
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selection = item; formMode = FormMode.Deleting"><i class="fa fa-trash-o"></i></a>
                                                </div>
                                            </td>
                                        </tr>
                                    </ng-template>
                                    <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                        <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                    </ng-template>
                                </p-table>                 
                            </div>
                            <div *ngSwitchCase="FormMode.Adding" class="col s12">
                                <d3s-dynamic-editor 
                                    objectType="IssueTypeRelation" 
                                    [objectID]="issueTypeId" 
                                    title="Issue Type Allocation"  
                                    (saveClick)="save($event)" 
                                    (closeClick)="formMode = FormMode.Default">
                                </d3s-dynamic-editor>
                            </div>
                            <div *ngSwitchCase="FormMode.Deleting" class="col s12">
                                <d3s-delete-form
                                            [callback]="deleteCallback"
                                            method="callback"
                                            [prompt]="'Are you sure you want to delete this allocation?'"                                         
                                            (onCancel)="formMode = FormMode.Default"
                                ></d3s-delete-form> 
                            </div>
                        </div>
                    </div>
                </div>  
                `,
    providers: [WorkflowService],
})

export class AdminIssueTypeAllocationComponent extends BaseComponent implements OnChanges {
    @Input() issueTypeId: number;
    formMode = FormMode.Default;
    FormMode = FormMode;
    allocations = [];
    selection = null;
    deleteCallback: Function;

    constructor(private workflowService: WorkflowService, protected messagesService: MessagesService) {
        super();
        this.deleteCallback = this.delete.bind(this);
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['issueTypeId'].currentValue != changes['issueTypeId'].previousValue || changes['issueTypeId'].isFirstChange) {
            this.formMode = FormMode.Default;
            this.load();
        }
    }

    load() {
        if (this.issueTypeId == null) {
            this.allocations = [];
            return;
        }
        this.isLoading = true;
        this.workflowService.getIssueTypeAllocations(this.issueTypeId)
            .subscribe(r => {
                this.allocations = r.Allocations;
                this.isLoading = false;
            });
    }

    save(e: any) {
        this.isLoading = true;
        this.workflowService.postIssueTypeAllocation(e.item)
            .subscribe(r => {
                this.formMode = FormMode.Default;
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, r);
                this.load();
            });
    }

    add() {
        this.formMode = FormMode.Adding;
    }

    delete() {
        this.isLoading = true;
        this.workflowService.deleteIssueTypeAllocation(this.issueTypeId, this.selection.AssetTypeID)
            .subscribe(r => {
                this.isLoading = false;
                this.formMode = FormMode.Default;
                this.showMessageForResult(this.messagesService, r);
                this.load();
            });
    }

};
