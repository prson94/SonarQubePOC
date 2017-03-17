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
                                    <p-dataTable sortField="Name" sortOrder="1" [value]="workflows" selectionMode="single" [(selection)]="selected">                                            
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
                                    <header>Selection Details</header>
                                    <p-dataTable [value]="details" selectionMode="single">
                                        <p-column field="Name" header="Item" [sortable]="true"></p-column>
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
        
    constructor(protected workflowService: WorkflowService) {
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
                if (!this.selected && this.workflows && this.workflows.length > 0) this.selected = this.workflows[0]; 
                this.isLoading = false;
            });
    }

    private changeTypeText(changeType: WorkflowChangeType) {
        return WorkflowChangeType[changeType];
    }

};