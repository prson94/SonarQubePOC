///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ArtifactService } from '../../services/index';
import { Count} from '../../models/counts.model';

@Component({
    selector: 'd3s-activity-tile',
    providers: [ArtifactService],
    template: `
                <div class="tile tile-detail">
                   <header>Activity
                    <d3s-tile-actions [hasAdd]="false"></d3s-tile-actions>                            
                   </header>
                    <div *ngIf="isLoading" style="width:100%; text-align:center;">
                        <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                    </div>
                    <p-dataTable *ngIf="!isLoading && counts.length > 0" [value]="counts" selectionMode="single" [(selection)]="selected" >                    
                        <p-column field="Name" header="Name" [sortable]="true"></p-column>                                                                           
                        <p-column field="New" header="Total" [sortable]="true" [style]="{'text-align':'center'}"></p-column>  
                    </p-dataTable>                      
                    <div *ngIf="counts.length == 0 && !isLoading">
                        No recent activity
                    </div>
                </div>
                `
})

export class ActivityTile extends BaseComponent implements OnInit {
    private counts: any[] = [];
    private selected: any;
    private daysToLookBack: number = 7;
    private isLoaded: boolean = false;

    constructor(private artifactService: ArtifactService) {
        super();
    }

    ngOnInit() {
        if (!this.isLoaded) this.load();
    }

    private load() {
        this.isLoading = true;
        this.artifactService.getActivityCount(this.daysToLookBack)
            .then(res => {
                this.counts = res;
                this.isLoading = false;
            });
    }
}


