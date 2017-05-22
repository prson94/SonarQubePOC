import { Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { WorkflowService } from '../../../services/workflow.service';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { WorkflowListItem, WorkflowChangeType, WorkflowActivityType, StepType } from '../../../models/workflow.model';

@Component({
    selector: 'd3s-workflow-monitor',
    template: ` 
                <div class="row">
                    <div class="col s4">
                        <div class="row">                                    
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <header>Workflows</header>
                                    <p-dataTable sortField="Name" sortOrder="1" [value]="workflows" selectionMode="single" [selection]="selected" (selectionChange)="selected=null;loadWorkflowItems($event)">
                                        <p-column field="Name" header="Name" [sortable]="true"></p-column>
                                        <p-column field="ChangeType" header="Change Type" [sortable]="true">
                                            <template let-col let-workflow="rowData" pTemplate type="body">
                                                <span>{{changeTypeText(workflow.ChangeType)}}</span>
                                            </template>
                                        </p-column>
                                        <p-column field="ConditionText" header="Condition" [sortable]="true"></p-column>
                                        <p-column field="Version" header="Version" [sortable]="true"></p-column>
                                    </p-dataTable>                                       
                                </div>
                            </div>
                        </div>
                        <div class="row">                                        
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <header>Workflow Items</header>
                                    <p-dataTable [value]="details" [rows]="10" paginator="true" selectionMode="single" (selectionChange)="selectedItem=$event;loadItemsDetails(selectedItem)">
                                        <p-column field="Name" header="Item" [sortable]="true">
                                            <template let-item="rowData" pTemplate type="body">
                                                <a (click)="openItem(item.Url)">{{item.Name}}</a>
                                            </template>
                                        </p-column>  
                                        <p-column field="NumberOfEvents" header="Total Events" [sortable]="true"></p-column>                                      
                                        <p-column field="UpdatedOn" header="Updated" [sortable]="true">
                                            <template let-col let-data="rowData" pTemplate type="body">
                                                <span>{{data.UpdatedOn | date: 'shortDate'}}</span>
                                            </template>
                                        </p-column>
                                        <p-column field="CompletedOn" header="Completed" [sortable]="true">
                                            <template let-col let-data="rowData" pTemplate type="body">
                                                <span>{{data.CompletedOn | date: 'shortDate'}}</span>
                                            </template>
                                        </p-column>
                                    </p-dataTable>
                                </div>
                            </div>
                        </div>                                
                        <div class="row">                                        
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <header>Items Details</header>
                                    <p-dataTable [value]="itemdetails" selectionMode="single" scrollable="true" scrollWidth="100%">                                        
                                        <p-column field="Name" header="Step Name" [sortable]="true" [style]="{'width':'150px'}">
                                            <template let-col let-data="rowData" pTemplate type="body">
                                                <span [title]="data.Settings">{{data.Name}}</span>
                                            </template>
                                        </p-column>
                                        <p-column field="ActivityTypeString" header="Activity" [sortable]="true" [style]="{'width':'100px'}"></p-column>
                                        <p-column field="StepTypeString" header="Step" [sortable]="true" [style]="{'width':'100px'}"></p-column>
                                        <p-column field="UpdatedOn" header="Started" [sortable]="true" [style]="{'width':'100px'}">
                                            <template let-col let-data="rowData" pTemplate type="body">
                                                <span>{{data.StartedOn | date: 'shortDate'}}</span>
                                            </template>
                                        </p-column>
                                        <p-column field="StartedBy" header="Started By" [sortable]="true" [style]="{'width':'100px'}"></p-column>
                                        <p-column field="CompletedOn" header="Completed" [sortable]="true" [style]="{'width':'100px'}">
                                            <template let-col let-data="rowData" pTemplate type="body">
                                                <span>{{data.CompletedOn | date: 'shortDate'}}</span>
                                            </template>
                                        </p-column>
                                        <p-column field="CompletedBy" header="Completed By" [sortable]="true" [style]="{'width':'100px'}"></p-column>                                   
                                        <p-column field="ToStep" header="Next" [sortable]="true" [style]="{'width':'150px'}"></p-column>                                   
                                    </p-dataTable>
                                </div>
                            </div>
                        </div>                                
                    </div>
                    <div class="col s8" *ngIf="selected && selected.ID > 0">    
                        <d3s-workflow-diagram [id]="selected.ID" [version]="selected.Version" readonly="true"></d3s-workflow-diagram>
                    </div>
                </div>                                            
                `,
    providers: [WorkflowService]
})

export class WorkflowMonitorComponent extends BaseComponent implements OnInit {
    
    private workflows: WorkflowListItem[] = [];
    private selected: WorkflowListItem = null;

    private details: any[] = [];
    private selectedItem: any = null;

    private itemdetails: any[] = [];
    private sub: any;

    constructor(
        protected workflowService: WorkflowService,
        private router: Router,
        private route: ActivatedRoute,        
    ) {
        super();
    }
    
    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];

            this.load();
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
    
    private load() {
        this.isLoading = true;
        this.workflowService.getObjectTypes(this.objectID, this.objectType)
            .then(res => {
                this.workflows = res;
                if (!this.selected && this.workflows && this.workflows.length > 0) {
                    this.selected = this.workflows[0];
                    this.loadWorkflowItems(this.selected);
                }
                this.isLoading = false;
            });
    }

    private loadWorkflowItems(selected: WorkflowListItem) {
        this.itemdetails = [];
        this.workflowService.getWorkflowItems(selected.VersionID)
            .then(res => {
                this.selected = selected;
                this.details = res;
                if (!this.selectedItem && this.details && this.details.length > 0) {
                    this.selectedItem = this.details[0];
                    this.loadItemsDetails(this.selectedItem);
                }
            });
    }

    private loadItemsDetails(selectedItem: any) {

        this.workflowService.getWorkflowItemDetails(this.selected.ID, selectedItem.ItemID)
            .then(res => {
                for (let i of res) {
                    i.ActivityTypeString = WorkflowActivityType[i.ActivityType];
                    i.StepTypeString = StepType[i.StepType];
                }
                this.itemdetails = res;
            });
    }

    private changeTypeText(changeType: WorkflowChangeType) {
        return WorkflowChangeType[changeType];
    }

    private openItem(url: string) {
        this.router.navigateByUrl(url);
    }
};