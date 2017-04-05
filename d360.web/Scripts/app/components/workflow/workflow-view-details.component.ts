import { Input, Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { WorkflowService } from '../../services/workflow.service';
import { StepType, WorkflowActivityType } from '../../models/workflow.model';


@Component({
    selector: 'd3s-workflow-view-detail',
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="tile tile-detail" *ngIf="!isLoading">
                    <header>Workflow Details</header>
                    <div class="row">
                        <div class="col s12 FieldName">Workflow Name</div>
                        <div class="col s12">{{details?.Workflow.Name}}</div>
                        <div class="col s12 FieldName">Name</div>
                        <div class="col s12">{{details?.ObjectDetails.Name}}</div>
                        <div class="col s12 FieldName">Type Name</div>
                        <div class="col s12">{{details?.ObjectDetails.TypeName}}</div>
                        <div class="col s12 FieldName">Type</div>
                        <div class="col s12">{{details?.ObjectDetails.Type}}</div>
                        <div class="col s12 FieldName">Started</div>
                        <div class="col s12">{{details?.Item.StartedOn | date:'shortDate'}}</div>
                    </div>
                    <div class="row" *ngIf="details?.Item.CompletedOn">
                        <div class="col s12 FieldName">Completed</div>
                        <div class="col s12">{{details?.Item.CompletedOn | date:'shortDate'}}</div>
                    </div>
                    <div class="row">
                        <div class="col s12 FieldName">Number of Events</div>
                        <div class="col s12">{{details?.Item.NumberOfEvents}}</div>
                    </div>
                    <div class="row">
                        <div class="col s12 FieldName">&nbsp;</div>
                    </div>
                    <div class="row">
                        <div class="col s12 FieldName">Workflow Tasks</div>
                    </div>
                    <div class="row">
                        <div class="col s12">
                            <p-dataTable #dt [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" pageLinks="3" [value]="details?.ItemSteps" selectionMode="single">                    
                                <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                                <p-column field="StepName" header="Step Name" sortable="false"></p-column>
                                <p-column field="StepType" header="Step Type" sortable="false"></p-column>
                                <p-column field="ActivityType" header="Activity Type" sortable="false"></p-column>
                                <p-column field="StartedOn" header="Date Started" sortable="true">
                                    <template let-itemStep="rowData" pTemplate type="body">
                                        <span>{{itemStep.StartedOn | date:'shortDate'}}</span>
                                    </template>
                                </p-column>
                                <p-column field="CompletedOn" header="Date Completed" sortable="true">
                                    <template let-itemStep="rowData" pTemplate type="body">
                                        <span>{{itemStep.CompletedOn | date:'shortDate'}}</span>
                                    </template>
                                </p-column>
                            </p-dataTable>       
                        </div>
                    </div>
                </div>
              `,
    providers: [WorkflowService]
})

export class WorkflowViewDetailsComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private workflowId: number;
    private details: any;
    private item: any;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected workflowService: WorkflowService
    ) {
        super();
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Workflow Item Status'));

        this.setBrowserTitle(this.titleService, 'Workflow Item Status');

        this.sub = this.route.params.subscribe(params => {
            this.workflowId = +params['workflowId'];
            this.load();
        });        
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getWorkflowDetailsV2(this.workflowId)
            .then(res => {
                for (let item of res.ItemSteps) {
                    if (!res.Steps) {
                        item.StepName = "";
                        continue;
                    }
                    var step = res.Steps.filter(x => x.ID == item.StepID);

                    if (!step || step.length == 0) {
                        item.StepName = "";
                        continue;
                    }
                    item.StepName = step[0].Name;
                    item.StepType = StepType[step[0].StepType];
                    item.ActivityType = WorkflowActivityType[step[0].ActivityType];
                }
                this.details = res;                                
                this.isLoading = false;
            });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
    

    private getStepName(itemStep: any): string {
        if (!this.details || !this.details.Steps) return "";
        var step = this.details.Steps.filter(x => x.ID == itemStep.StepID);

        if (!step || step.length == 0) return "";
        return step[0].Name;
    }
};