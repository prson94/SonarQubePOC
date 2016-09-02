///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/index';
import { Count} from '../../models/counts.model';

@Component({
    selector: 'd3s-assignments-tile',
    providers: [WorkflowService],
    template: `
                <div class="tile tile-detail">
                   <header>Your Assignments
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
    private counts: any[] = [];
    private selected: any;
    private daysToLookBack: number = 7;
    private isLoaded: boolean = false;

    constructor(private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        if (!this.isLoaded) this.load();
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getMyCounts(this.daysToLookBack)
            .then(res => {
                this.counts = res;
                this.isLoading = false;
            });
    }
}


