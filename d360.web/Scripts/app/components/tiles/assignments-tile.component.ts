import { Component, OnInit, Output, EventEmitter, Input} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService, ResourcesService } from '../../services/index';
import { Count } from '../../models/counts.model';
import { Resource } from '../../models/resource.model';
import { WorkflowType } from '../../models/workflow.model';

@Component({
    selector: 'd3s-assignments-tile',
    providers: [WorkflowService, ResourcesService],
    template: `
                <div class="tile tile-detail">
                   <header *ngIf="resourceId >= 0">{{resource?.FirstName}}'s Assignments
                    <d3s-tile-actions [hasAdd]="false"></d3s-tile-actions>                            
                   </header>
                   <header *ngIf="resourceId == null || resourceId < 0">Your Assignments
                    <d3s-tile-actions [hasAdd]="false"></d3s-tile-actions>                            
                   </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <p-dataTable *ngIf="!isLoading && counts.length > 0" sortField="Name" [sortOrder]="1" [value]="counts" selectionMode="single" [(selection)]="selected" (onRowDblclick)="selected=$event.data;doSelect(selected)" >                    
                        <p-column field="Name" header="Name" [sortable]="true">
                            <template let-item="rowData" pTemplate type="body">
                                    <a (click)="doSelect(item)">{{item.Name}}</a>
                            </template>
                        </p-column>           
                        <p-column field="Total" header="Count" [sortable]="true" [style]="{'text-align':'center'}"></p-column>                                                                
                    </p-dataTable>                      
                    <div *ngIf="counts.length == 0 && !isLoading" style="padding:10px">You currently have no assignments</div>
                </div>
                `
})

export class AssignmentsTile extends BaseComponent implements OnInit {
    @Input() resourceId = -1;
    @Output() showItemDetail = new EventEmitter();
    private counts: Count[] = [];
    private selected: Count;
    private daysToLookBack: number = 7;
    private isLoaded: boolean = false;
    private resource: Resource = null;

    constructor(private workflowService: WorkflowService, private resourcesService: ResourcesService) {
        super();
    }

    ngOnInit() {
        if (!this.isLoaded) this.load();
    }

    private load() {
        this.isLoading = true;
        let loadResource = (this.resourceId != null && this.resourceId >= 0);

        this.workflowService.getMyCounts(this.daysToLookBack, (loadResource ? this.resourceId : null))
            .then(res => {
                this.counts = res.filter(item => (item.Total > 0));
                if (loadResource)
                    this.resourcesService.getResource(this.resourceId)
                        .then(r => {
                            this.resource = r;
                            this.isLoading = false;
                            this.isLoaded = true;
                        });
                else {
                    this.isLoading = false;
                    this.isLoaded = true;
                }
            });
    }

    private doSelect(item) {
        this.showItemDetail.emit({
            workflowType: this.getWorkflowType(item)
        });
    }

    private getWorkflowType(item): WorkflowType{
        if (!item) return null;

        switch (item.Name.toUpperCase()) {
            case "CERTIFY ARTIFACT":
                return WorkflowType.CertifyArtifact;
            case "CHALLENGE":
                return WorkflowType.ChallengeArtifact;
            case "PROPOSE NEW ARTIFACT":
                return WorkflowType.SuggestNewArtifact;
            case "ISSUES":
                return WorkflowType.WorkIssue;            
        }        
        return null;
    }
}


