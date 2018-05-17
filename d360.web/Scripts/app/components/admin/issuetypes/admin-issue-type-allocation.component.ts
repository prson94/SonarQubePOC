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
                                <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt [globalFilter]="gb" [value]="allocations" selectionMode="single" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [(selection)]="selection" [rowsPerPageOptions]="defaultPagingOptions">                                                                        
                                    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                                    <p-column field="ObjectType" header="Object Type" sortable="true" [filter]="!showSimpleFilter"></p-column>                                                            
                                    <p-column field="TypeName" header="Object Name" sortable="true"  [filter]="!showSimpleFilter"></p-column>                                         
                                    <p-column [style]="{width:'40px'}">
                                        <ng-template let-item="rowData" pTemplate type="body">
                                            <div class="RowTools">                                
                                                <a style="cursor:pointer;" (click)="selection = item; formMode = FormMode.Deleting"><i class="fa fa-trash-o"></i></a>                                    
                                            </div>
                                        </ng-template>
                                    </p-column>                            
                                </p-dataTable>                          
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
            .then(r => {
                this.allocations = r.Allocations;
                this.isLoading = false;
            });
    }

    save(e: any) {
        this.isLoading = true;
        this.workflowService.postIssueTypeAllocation(e.item)
            .then(r => {
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
            .then(r => {
                this.isLoading = false;
                this.formMode = FormMode.Default;
                this.showMessageForResult(this.messagesService, r);
                this.load();
            });
    }

};
