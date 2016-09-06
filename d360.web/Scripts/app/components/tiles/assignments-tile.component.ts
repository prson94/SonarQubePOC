///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit, Input } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService, ResourcesService } from '../../services/index';
import { Count } from '../../models/counts.model';
import { Resource } from '../../models/resource.model';

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
                    <div *ngIf="isLoading" style="width:100%; text-align:center;">
                        <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                    </div>
                    <p-dataTable *ngIf="!isLoading" [value]="counts" selectionMode="single" [(selection)]="selected" >                    
                        <p-column field="Name" header="Name" [sortable]="true"></p-column>           
                        <p-column field="Total" header="Count" [sortable]="true" [style]="{'text-align':'center'}"></p-column>                                                                
                    </p-dataTable>                      
                </div>
                `
})

export class AssignmentsTile extends BaseComponent implements OnInit {
    @Input() resourceId = -1;
    private counts: any[] = [];
    private selected: any;
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
                this.counts = res;
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
}


