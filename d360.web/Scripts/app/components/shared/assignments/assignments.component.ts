import { Component, OnInit, Output, EventEmitter, Input} from '@angular/core';
import { BaseComponent } from '../base.component';
import { WorkflowService } from '../../../services/workflow.service';
import { ResourcesService } from '../../../services/resources.service';
import { Count } from '../../../models/counts.model';
import { WorkflowType } from '../../../models/workflow.model';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-assignments',
    providers: [WorkflowService, ResourcesService],
    template: `
                <div class="tile tile-detail">
                   <header>Assignments
                    <d3s-tile-actions [hasAdd]="false"></d3s-tile-actions>                            
                   </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <p-table #dt *ngIf="!isLoading && counts.length > 0" [value]="counts" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name','Version','Step','Total']" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'">
                                    Name
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Version'" style="text-align:center">
                                    Version
                                    <d3s-sortIcon [field]="'Version'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Step'" style="text-align:left">
                                    Step
                                    <d3s-sortIcon [field]="'Step'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Total'" style="text-align:center">
                                    Count
                                    <d3s-sortIcon [field]="'Total'"></d3s-sortIcon>
                                </th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr (dblclick)="selected=item;doSelect(selected)" [pSelectableRow]="item">
                                <td>
                                    <a (click)="doSelect(item)">{{item.Name}}</a>
                                </td>
                                <td style="text-align:center">{{item.Version}}</td>
                                <td>{{item.Step}}</td>
                                <td style="text-align:center">{{item.Total}}</td>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>                                    
                    <div *ngIf="counts.length == 0 && !isLoading" style="padding:10px">You currently have no assignments</div>
                </div>
                `
})

export class AssignmentsComponent extends BaseComponent implements OnInit {
    @Input() resourceId = -1;
    @Output() showItemDetail = new EventEmitter();
    counts: Count[] = [];
    private selected: Count;
    private daysToLookBack: number = 7;
    private isLoaded: boolean = false;
    private items: any[] = [];
    private resource: any = null;


    constructor(
        private resourcesService: ResourcesService,
        protected settingsService: CompanySettingsService,
        private workflowService: WorkflowService) {
        super(settingsService);
    }

    ngOnInit() {
        if (!this.isLoaded) this.load();
    }

    private load() {
        this.isLoading = true;
        let loadResource = (this.resourceId != null && this.resourceId >= 0);

        this.workflowService.getMyCounts(this.daysToLookBack, (loadResource ? this.resourceId : null))
            .subscribe(res => {
                this.counts = res.filter(item => (item.Total > 0));
                if (loadResource)
                    this.resourcesService.getResource(this.resourceId)
                        .subscribe(r => {
                            this.items = r.items;
                            if (this.items.length > 0) {
                                this.resource = this.items[0];
                            }
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
            workflowType: this.getWorkflowType(item),
            resourceID: this.resourceId,
            workflowId: item.Id,
            version: item.Version,
            stepId:item.StepId
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
            case "ACTIONS":
                return WorkflowType.WorkIssue;                          
        }
        return WorkflowType.None;
    }
}


