///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit, Output, EventEmitter, Input} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ArtifactService } from '../../services/index';
import { Count} from '../../models/counts.model';
import { Artifact } from '../../models/artifacts.model';

@Component({
    selector: 'd3s-activity-details-tile',
    providers: [ArtifactService],
    template: `
                <div class="tile tile-detail">
                   <header>Activity for {{objectName}}
                    <d3s-tile-actions [hasAdd]="false"></d3s-tile-actions>                            
                   </header>
                    <div *ngIf="isLoading" style="width:100%; text-align:center;">
                        <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                    </div>
                    <p-dataTable *ngIf="!isLoading" [value]="items" selectionMode="single" [(selection)]="selected" scrollable="true" scrollWidth="100%" [rows]="10" [paginator]="true" [pageLinks]="4" [rowsPerPageOptions]="[5,10,20]" [responsive]="true" [stacked]="stacked">                    
                        <p-column field="Name" header="Name" [sortable]="true" [filter]="true">
                            <template let-col let-item="rowData" pTemplate type="body">
                                <a [routerLink]="'/a/artifact/' + item.ArtifactTypeID + '/' + item.ID">{{item.Name}}</a>
                            </template>
                        </p-column>                                                                                                   
                        <p-column field="Status" header="Status" [sortable]="true" [filter]="true" [style]="{'width':'150px'}"></p-column>
                        <p-column [style]="{width:'40px'}">
                            <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools">
                                    <d3s-tooltip [objectType]="'Artifact'" [objectId]="item.ID" [tooltipType]="'certificate'" [icon]="'certificate'" [iconColor]="certificateColor(item)"></d3s-tooltip>                                            
                                </div>
                            </template>
                        </p-column>
                        <p-column [style]="{width:'40px'}">
                            <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools">
                                    <d3s-tooltip [objectType]="'Artifact'" [objectId]="item.ID" (click)="selectArtifact(item)" [tooltipType]="'Preview'" [icon]="'info'"></d3s-tooltip>                                            
                                </div>
                            </template>
                        </p-column>
                    </p-dataTable>      
                    <button pButton type="button" (click)="close.emit();" label="Close" style="width:150px;margin-top:10px"></button>                    
                </div>
                `
})

export class ActivityDetailsTile extends BaseComponent implements OnInit {    
    private items: Artifact[] = [];
    private selected: Artifact;

    @Input() objectName: string;
    @Input() objectId: number = 0;
    
    @Input() daysToLookBack: number = 7;

    @Output() close = new EventEmitter();

    constructor(private artifactService: ArtifactService) {
        super();
    }

    ngOnInit() {
        if (this.objectId > 0)
            this.load();
    }

    private load() {
        this.isLoading = true;
        this.artifactService.getActivityDetails(this.objectId, this.daysToLookBack)
            .then(res => {
                this.items = res;
                this.isLoading = false;
            });
    }

    private certificateColor(item) {
        switch (item.Status) {
            case 'Certified':
                return '#3f9d40';
            case 'Under Review':
                return '#e2792a';
        }
        return '#ebebeb';
    }
    
}


