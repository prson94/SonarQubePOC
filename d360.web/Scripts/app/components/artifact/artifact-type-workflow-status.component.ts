import { Input, Component, OnChanges, SimpleChange } from '@angular/core';
import { ArtifactTypeService, WorkflowService } from '../../services/index';
import { BaseComponent} from '../shared/base.component';
import { ArtifactType } from '../../models/artifact-type.model';
import { ArtifactTypeWorkflowBreakdown, WorkflowStepStatistic } from '../../models/workflow.model';

@Component({
    selector: 'd3s-artifact-type-workflow-status',
    template: `     
                <d3s-loading [isLoading]="isLoading"></d3s-loading>            
                <div class="row" *ngIf="!isLoading">                    
                    <div class="col s12 m12 l4">                        
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">                            
                                    <header>Workflow Type</header>
                                    <p-dataTable [value]="workflowTypes" selectionMode="single" [selection]="selected" (selectionChange)="selected=$event;workflowTypeChanged();">                    
                                        <p-column field="Name" header="" [sortable]="false"></p-column>                                                                                               
                                    </p-dataTable> 
                                </div>  
                            </div>
                            <div class="col s12">
                                <div class="tile tile-detail">                            
                                    <header>{{selected?.Name}} Workflow</header>
                                    <div class="form-instructions">{{selectedWorkflow?.Description}}</div>
                                    <p-dataTable [value]="selectedWorkflow?.Steps" selectionMode="single">                    
                                        <p-column field="ID" header="ID" [sortable]="true"></p-column>
                                        <p-column field="Name" header="Name" [sortable]="true"></p-column>                                                                                               
                                        <p-column field="Count" header="Count" [sortable]="true"></p-column>
                                    </p-dataTable> 
                                </div>  
                            </div>                            
                        </div>
                    </div>                
                    <div class="col s12 m12 l8">
                        <div class="tile tile-detail">                            
                                    <header>{{selected?.Name}} Details</header>
                
                        </div>  
                    </div>
                </div>
                `,
    providers: [ArtifactTypeService,WorkflowService],
})

export class ArtifactTypeWorkflowStatusComponent extends BaseComponent implements OnChanges {
    @Input() artifactType: ArtifactType;


    private workflowTypes: any[] = [{ Name: 'Certify Artifact', ID:2 }, { Name: 'Propose New Artifact', ID:1 }, { Name: 'Propose New Artifact(multi-approval)', ID:5 }];
    private selected: any;

    private workflowStats: ArtifactTypeWorkflowBreakdown[] = [];
    private selectedWorkflow: ArtifactTypeWorkflowBreakdown;

    constructor(private workflowService: WorkflowService) {
        super();
    }
        
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.artifactType != null && changes['artifactType']) {            
            this.load();
        }
    }
    

    private load() {
        this.isLoading = true;
        this.workflowService.getWorkflowStepBreakdownByArtifactType(this.artifactType.ID)
            .then(result => {
                this.workflowStats = result;                
                this.selected = this.workflowTypes[0];
                this.workflowTypeChanged();
                this.isLoading = false;
            });
    }

    private workflowTypeChanged() {
        let res = this.workflowStats.filter(x => x.ID == this.selected.ID);

        if (res.length > 0)
            this.selectedWorkflow = res[0];
    }
    
};