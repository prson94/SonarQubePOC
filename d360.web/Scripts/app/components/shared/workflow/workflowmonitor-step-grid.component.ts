import { Component, OnChanges, Input, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { WorkflowItemStep, WorkflowActivityType, StepType } from '../../../models/workflow.model';
import { WorkflowHelpers } from '../../../static/workflow-helpers';
import { Router } from '@angular/router';
import { StateService } from '../../../services/state.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-workflow-monitor-step-grid',
    template: ` 
    <p-table #dt [value]="itemSteps" selectionMode="single" [metaKeySelection]="true" [pageLinks]="3" [paginator]="true" [rows]="10" [(selection)]="selection"> 
        <ng-template pTemplate="header">
            <tr>
                <th [pSortableColumn]="allowSort">
                    Step Name
                    <d3s-sortIcon *ngIf="allowSort" [field]="'Name'"></d3s-sortIcon>
                </th>
                <th [pSortableColumn]="allowSort" style="width:  90px">
                    Complete
                    <d3s-sortIcon *ngIf="allowSort" [field]="'Complete'"></d3s-sortIcon>
                </th>
                <th [pSortableColumn]="allowSort">
                    Activity Type
                    <d3s-sortIcon *ngIf="allowSort" [field]="'ActivityType'"></d3s-sortIcon>
                </th>
                <th [pSortableColumn]="allowSort" [hidden]="!showAssigneeColumn">
                    Assignee
                    <d3s-sortIcon *ngIf="allowSort" [field]="'Assignee'"></d3s-sortIcon>
                </th>
                <th [pSortableColumn]="allowSort">
                    Date Started
                    <d3s-sortIcon *ngIf="allowSort" [field]="'StartedOn'"></d3s-sortIcon>
                </th>
                <th [pSortableColumn]="allowSort">
                    Date Completed
                    <d3s-sortIcon *ngIf="allowSort" [field]="'CompletedOn'"></d3s-sortIcon>
                </th>
            </tr>
            <tr [hidden]="showSimpleFilter">
                <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                <th><d3s-column-filter [field]="'Complete'" [datatype]="'text'"></d3s-column-filter></th>
                <th><d3s-column-filter [field]="'ActivityType'" [datatype]="'text'"></d3s-column-filter></th>
                <th [hidden]="!showAssigneeColumn"><d3s-column-filter [field]="'Assignee'" [datatype]="'text'"></d3s-column-filter></th>
                <th><d3s-column-filter [field]="'StartedOn'" [datatype]="'text'"></d3s-column-filter></th>
                <th><d3s-column-filter [field]="'CompletedOn'" [datatype]="'text'"></d3s-column-filter></th>
            </tr>
        </ng-template>
        <ng-template pTemplate="body" let-item>
            <tr (click)="rowClick(item)" [pSelectableRow]="item"  style="word-wrap: break-word">
                <td>
                    <a *ngIf="item.IsAssignedLoginUser=='True'" (click)="doSelect(item)">{{item.Name}}</a>
                    <span *ngIf="item.IsAssignedLoginUser!='True'">{{item.Name}}</span>
                </td>
                <td>
                    <span>
                        <i *ngIf="item.Complete == true" class="fa fa-check enabled" title="True"></i>
                        <i *ngIf="item.Complete == false" class="fa fa-times disabled" title="False"></i>
                    </span>
                </td>
                <td>
                    {{helper.activityTypeName(item.ActivityType)}}
                </td>
                <td [hidden]="!showAssigneeColumn">{{item.Assignee}}</td>
                <td>
                    {{item.StartedOn | date:'shortDate'}}
                </td>
                <td>
                    {{item.CompletedOn | date:'shortDate'}}
                </td>
            </tr>
        </ng-template>
        <ng-template pTemplate="summary">
            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
        </ng-template>
    </p-table>
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

    constructor(
        protected settingsService: CompanySettingsService,
        private stateService: StateService,
        private ref: ChangeDetectorRef,
        private router: Router
        ) {
        super(settingsService);
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['itemSteps'] != null && (changes['itemSteps'].isFirstChange || changes['itemSteps'].currentValue != changes['itemSteps'].previousValue)) {
            this.load();
        }
    }

    load() {
        if (this.itemSteps != null) {
            this.showAssigneeColumn = (this.itemSteps.find(i => i.ActivityType == WorkflowActivityType.Form) != null)
            let index = this.itemSteps.findIndex(x => x.StepID == this.stateService.workflowItemFilters.stepId && x.ItemID == this.stateService.workflowItemFilters.itemId);
            index = (index == -1) ? 0 : index;
            this.selection = this.itemSteps[index];
            this.selectionChange.emit(this.selection);
        }
        this.ref.markForCheck();
    }

    doSelect(item: WorkflowItemStep) {
       this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_FORM}/${item.TypeID}/${item.ID}/${item.ItemID}`);
    }

    rowClick(item: any) {
        this.stateService.workflowItemFilters.stepId = item.StepID;
        this.selectionChange.emit(item);
    }
}