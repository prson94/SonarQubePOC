import {Component, Input, Output, EventEmitter, OnChanges, SimpleChange} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/index';
import { WorkflowStatusDetails } from '../../models/workflow.model';

@Component({
    selector: 'd3s-workflow-detailed-view',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div *ngIf="!isLoading">
                <div class="row" *ngFor="let field of workflowStatusData?.Fields">                    
                    <div class="col s6">
                        {{field.Name}}
                    </div>
                    <div class="col s6" *ngIf="!isDateField(field.Name)" [innerHtml]="field.Value"></div>                    
                    <div class="col s6" *ngIf="isDateField(field.Name)">{{field.Value | date : 'short'}}</div>                    
                </div>
                <div class="row">&nbsp;</div>                
                <p-dataTable [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" pageLinks="3" [value]="workflowStatusData?.Assignments" selectionMode="single" scrollable="true" scrollWidth="100%" >                    
                    <p-column field="ActivityTypeName" header="Activity" sortable="true"></p-column>                                
                    <p-column field="ResourceID" header="User" sortable="true">
                        <template let-item="rowData" pTemplate type="body">
                            <span><d3s-tooltip objectType="Resource" [objectId]="item.ResourceID" tooltipType="preview">{{item.ResourceName}}</d3s-tooltip></span>
                        </template>
                    </p-column>
                    <p-column field="IsComplete" header="Completed?" sortable="true">
                        <template let-activity="rowData" pTemplate type="body">
                            <span><i class="fa fa-times disabled" *ngIf="!activity.IsComplete"></i><i class="fa fa-check enabled" *ngIf="activity.IsComplete"></i></span>
                        </template>
                    </p-column>
                </p-dataTable>
                
            </div>
        `,
    providers: [WorkflowService]
})

export class WorkflowDetailedViewComponent extends BaseComponent implements OnChanges {
    @Input() workflowId: string;

    private workflowStatusData: WorkflowStatusDetails;

    constructor(private workflowService: WorkflowService) {
        super();
    }
    
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['workflowId'] && this.workflowId){
                this.load();            
        }
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getWorkflowStatus(this.workflowId)
            .then(result => {             
                this.workflowStatusData = result;   
                this.isLoading = false;                
            });
    }    

    private isDateField(field: string): boolean {
        return field.toUpperCase().indexOf('DATE') >= 0;
    }
}