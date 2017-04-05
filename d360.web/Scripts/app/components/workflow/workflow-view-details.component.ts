import { Input, Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { WorkflowService } from '../../services/workflow.service';


@Component({
    selector: 'd3s-workflow-view-detail',
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="tile tile-detail" *ngIf="!isLoading">
                    <header>Workflow Details</header>
                    <div class="row">
                        <div class="col s12 FieldName">Name</div>
                        <div class="col s12">{{details?.ObjectDetails.Name}}</div>
                        <div class="col s12 FieldName">Type Name</div>
                        <div class="col s12">{{details?.ObjectDetails.TypeName}}</div>
                        <div class="col s12 FieldName">Type</div>
                        <div class="col s12">{{details?.ObjectDetails.Type}}</div>
                        <div class="col s12 FieldName">Started</div>
                        <div class="col s12">{{details?.Item.StartedOn | date:'shortDate'}}</div>
                        <div class="col s12 FieldName">Completed</div>
                        <div class="col s12">{{details?.Item.CompletedOn | date:'shortDate'}}</div>
                        <div class="col s12 FieldName">Number of Events</div>
                        <div class="col s12">{{details?.Item.NumberOfEvents}}</div>
                    </div>
                    <div class="row">
                        <div class="col s12 FieldName">Workflow Tasks</div>
                    </div>
                    <div class="row">
                        <div class="col s12">
                            <p-dataTable #dt [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" pageLinks="3" [value]="details?.ItemSteps" selectionMode="single">                    
                                <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                                <p-column field="StartedOn" header="Date Started" sortable="true"></p-column>
                                <p-column field="CompletedOn" header="Date Completed" sortable="true"></p-column>
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
                this.details = res;                
                this.isLoading = false;
            });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
};