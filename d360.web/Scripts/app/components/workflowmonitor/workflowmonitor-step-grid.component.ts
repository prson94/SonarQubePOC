import { Component, OnChanges, Input, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { WorkflowItemStep, WorkflowActivityType, StepType } from '../../models/workflow.model';
import { WorkflowHelpers } from '../../static/workflow-helpers';
import { Router } from '@angular/router';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-workflow-monitor-step-grid',
    template: ` 
   <!-- <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">  -->                                            
    <p-dataTable #dt [value]="itemSteps" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" [(selection)]="selection" (onRowClick)="selectionChange.emit($event.data)">                                                                        
        <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
        <p-column field="Name" header="Step Name" [sortable]="allowSort" [filter]="!showSimpleFilter">
            <ng-template pTemplate type="body" let-item="rowData">
                <a *ngIf="item.IsAssignedLoginUser=='True'" (click)="doSelect(item)">{{item.Name}}</a>
                <span *ngIf="item.IsAssignedLoginUser!='True'">{{item.Name}}</span>
            </ng-template>
        </p-column>     
        <p-column field="Complete" header="Complete" [sortable]="allowSort" [filter]="!showSimpleFilter" [style]="{'width': '90px'}">
            <ng-template let-col let-item="rowData" pTemplate type="body">
                <span>
                    <i *ngIf="item.Complete == true" class="fa fa-check enabled" title="True"></i>
                    <i *ngIf="item.Complete == false" class="fa fa-times disabled" title="False"></i>
                </span>
            </ng-template>                                                        
        </p-column> 
        <p-column field="ActivityType" header="Activity Type" [sortable]="allowSort" [filter]="!showSimpleFilter">
            <ng-template let-col let-item="rowData" pTemplate type="body">
                {{helper.activityTypeName(item.ActivityType)}}
            </ng-template>                                                        
        </p-column>  
        <p-column *ngIf="showAssigneeColumn" field="Assignee" header="Assignee" [sortable]="allowSort" [filter]="!showSimpleFilter"></p-column>    
        <p-column field="StartedOn" header="Date Started" [sortable]="allowSort" [filter]="!showSimpleFilter">
            <ng-template let-col let-item="rowData" pTemplate type="body">
                {{item.StartedOn | date:'shortDate'}}
            </ng-template>                                                        
        </p-column>  
        <p-column field="CompletedOn" header="Date Completed" [sortable]="allowSort" [filter]="!showSimpleFilter">
            <ng-template let-col let-item="rowData" pTemplate type="body">
                {{item.CompletedOn | date:'shortDate'}}
            </ng-template>  
        </p-column>  
    </p-dataTable> 
`,
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowMonitorStepGridComponent extends BaseComponent implements OnChanges {
    @Input() itemSteps: WorkflowItemStep[] = [];
    @Output() selectionChange = new EventEmitter();

    helper = WorkflowHelpers;
    selection: WorkflowItemStep = null;

    showAssigneeColumn = false;
    allowSort = false;

    constructor(private ref: ChangeDetectorRef,private router:Router) {
        super();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['itemSteps'] != null && (changes['itemSteps'].isFirstChange || changes['itemSteps'].currentValue != changes['itemSteps'].previousValue)) {
            this.load();
        }
    }

    load() {
        if (this.itemSteps != null) {
            this.showAssigneeColumn = (this.itemSteps.find(i => i.ActivityType == WorkflowActivityType.Form) != null)
            this.selection = this.itemSteps[0];
            this.selectionChange.emit(this.selection);
        }
        this.ref.markForCheck();
    }

    doSelect(item) {
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_LIST_V2}/${item.WorkflowTypeId}/${item.Version}/${item.StepID};resourceID=${CurrentResourceID}`);
    }
}