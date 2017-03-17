import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { WorkflowService } from '../../../services/workflow.service';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { WorkflowTypeItem, WorkflowChangeType } from '../../../models/workflow.model';

@Component({
    selector: 'd3s-workflow-monitor',
    template: ` 
                <div class="row">
                    <div class="col s4">
                        <div class="row">                                    
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <header>Workflows</header>
                                    <p-dataTable sortField="Name" sortOrder="1" [value]="workflows" selectionMode="single" [selection]="selected" (selectionChanged)="selected=$event;loadWorkflowItems(selected)">
                                        <p-column field="Name" header="Name" [sortable]="true"></p-column>
                                        <p-column field="ChangeType" header="Change Type" [sortable]="true">
                                            <template let-col let-workflow="rowData" pTemplate type="body">
                                                <span>{{changeTypeText(workflow.ChangeType)}}</span>
                                            </template>
                                        </p-column>
                                    </p-dataTable>                                       
                                </div>
                            </div>
                        </div>
                        <div class="row">                                        
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <header>Workflow Item Details</header>
                                    <p-dataTable [value]="details" selectionMode="single">
                                        <p-column field="Name" header="Item" [sortable]="true">
                                            <template let-item="rowData" pTemplate type="body">
                                                <a (click)="openItem(item.Url)">{{item.Name}}</a>
                                            </template>
                                        </p-column>
                                        <p-column field="Step" header="Step" [sortable]="true"></p-column>
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
                    </div>
                    <div class="col s8" *ngIf="selected && selected.ID > 0">    
                        <d3s-workflow-diagram [id]="selected.ID" readonly="true"></d3s-workflow-diagram>
                    </div>
                </div>                                            
                `,
    providers: [WorkflowService]
})

export class WorkflowMonitorComponent extends BaseComponent implements OnInit {
    @Input() objectID: number;
    @Input() objectType: string;

    private workflows: WorkflowTypeItem[] = [];
    private selected: WorkflowTypeItem = null;

    private details: any[] = [];
        
    constructor(protected workflowService: WorkflowService, private router: Router) {
        super();
    }

    ngOnInit() {
        this.load();
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

    private loadWorkflowItems(selected: WorkflowTypeItem) {
        this.workflowService.getWorkflowItems(selected.ID)
            .then(res => {
                this.details = res;
            });
    }

    private changeTypeText(changeType: WorkflowChangeType) {
        return WorkflowChangeType[changeType];
    }

    private openItem(url: string) {
        this.router.navigateByUrl(url);
    }
};