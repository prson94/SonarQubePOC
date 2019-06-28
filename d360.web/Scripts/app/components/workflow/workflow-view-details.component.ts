import { Input, Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { WorkflowService } from '../../services/workflow.service';
import { StepType, WorkflowActivityType } from '../../models/workflow.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { HeaderActionsService } from '../../services/header-actions.service';
import { HeaderActions } from '../../models/header.model';


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
                        <div class="col s12" *ngIf="details?.ObjectDetails != null"><d3s-preview-tooltip [objectType]="details?.Item.Object" [objectId]="details?.ObjectDetails?.ID" [innerHtmlContent]="details?.ObjectDetails?.Name"></d3s-preview-tooltip></div>
                        <div class="col s12" *ngIf="details?.ObjectDetails == null">(item deleted)</div>
                        <div class="col s12 FieldName">Type Name</div>
                        <div class="col s12" *ngIf="details?.ObjectDetails != null">{{details?.ObjectDetails?.TypeName}}</div>
                        <div class="col s12" *ngIf="details?.ObjectDetails == null">(item deleted)</div>
                        <div class="col s12 FieldName">Type</div>
                        <div class="col s12" *ngIf="details?.ObjectDetails != null">{{details?.ObjectDetails?.Type}}</div>
                        <div class="col s12" *ngIf="details?.ObjectDetails == null">(item deleted)</div>
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
                            <p-table #dt [value]="details?.ItemSteps" selectionMode="single" [metaKeySelection]="true" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
                                <ng-template pTemplate="header">
                                    <tr>
                                        <th>Step Name</th>
                                        <th>Step Type</th>
                                        <th>Complete</th>
                                        <th>Activity Type</th>
                                        <th [pSortableColumn]="'StartedOn'">
                                            Date Started
                                            <d3s-sortIcon [field]="'StartedOn'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'CompletedOn'">
                                            Date Completed
                                            <d3s-sortIcon [field]="'CompletedOn'"></d3s-sortIcon>
                                        </th>
                                    </tr>
                                </ng-template>
                                <ng-template pTemplate="body" let-item>
                                    <tr [pSelectableRow]="item">
                                        <td>
                                            <span *ngIf="item.CompletedOn">{{item.StepName}}</span>
                                            <span *ngIf="!item.CompletedOn && item.ActivityType =='Form'"><a (click)="showForm(item)">{{item.StepName}}</a></span>
                                        </td>
                                        <td>{{item.StepType}}</td>
                                        <td>
                                            <span *ngIf="item.CompletedOn;else other_content"><i class="fa fa-check enabled" title="True"></i></span>
                                            <ng-template #other_content><span></span></ng-template>
                                        </td>
                                        <td>{{item.ActivityType}}</td>
                                        <td>
                                            <span>{{item.StartedOn | date:'shortDate'}}</span>
                                        </td>
                                        <td>
                                            <span>{{item.CompletedOn | date:'shortDate'}}</span>
                                        </td>
                                    </tr>
                                </ng-template>
                                <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                    <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                </ng-template>
                            </p-table>
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
    private workflowTypeId: number;
    constructor(
        private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private headerActionsService: HeaderActionsService,
        protected workflowService: WorkflowService
    ) {
        super();
    }

    ngOnInit() {
        this.showHideFollow(false);
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
            .subscribe(res => {
                for (let item of res.ItemSteps) {
                    if (!res.Steps) {
                        item.StepName = "";
                        continue;
                    }
                    var step = res.Steps.filter(x => x.ID == item.StepID);

                    if (!step || step.length == 0) {
                        item.StepName = "(unresolved)";
                        continue;
                    }
                    item.StepName = step[0].Name;
                    item.StepType = StepType[step[0].StepType];
                    item.ActivityType = WorkflowActivityType[step[0].ActivityType];
                }
                this.details = res;
                if (res && res.Workflow && res.Workflow.ID)
                    this.workflowTypeId = res.Workflow.ID;
                this.isLoading = false;
            });
    }

    private showHideFollow(show: boolean) {
        let headerActions: HeaderActions = new HeaderActions();
        headerActions.showFollow = show;
        this.headerActionsService.setCurrentHeaderActions(headerActions);
    }

    ngOnDestroy() {
        this.showHideFollow(true);
        this.sub.unsubscribe();
    }

    private getStepName(itemStep: any): string {
        if (!this.details || !this.details.Steps) return "";
        var step = this.details.Steps.filter(x => x.ID == itemStep.StepID);

        if (!step || step.length == 0) return "";
        return step[0].Name;
    }

    private showForm(item: any) {
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_FORM}/${this.workflowTypeId}/${item.ID}/${item.ItemID}`);
    }
};