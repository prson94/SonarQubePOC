import { Component, OnInit, Output, EventEmitter, Input} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { ArtifactService } from '../../services/index';
import { Count} from '../../models/counts.model';
import { Artifact } from '../../models/artifacts.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-activity-details-tile',
    providers: [ArtifactService],
    template: `
                <div class="tile tile-detail">
                   <header>Activity for {{objectName}}
                    <d3s-tile-actions hasAdd="false" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                   </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                        <p-dataTable #dt [globalFilter]="gb" [value]="items" selectionMode="single" [(selection)]="selected" (onRowDblclick)="selected=$event.data;navigateToArtifact();" scrollable="true" scrollWidth="100%" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions">                    
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column field="Name" header="Name" sortable="custom" (sortFunction)="columnSort($event)"  [filter]="!showSimpleFilter">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <a (click)="artifactLink(item.ArtifactTypeID, item.ID)">{{item.Name}}</a>
                                </template>
                            </p-column>                                                                                                   
                            <p-column field="Status" header="Status" sortable="true" [filter]="!showSimpleFilter" [style]="{'width':'150px'}"></p-column>
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
                    </span>
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

    constructor(private router: Router, private artifactService: ArtifactService) {
        super();
    }

    ngOnInit() {
        if (this.objectId > 0)
            this.load();
    }

    private navigateToArtifact() {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${this.selected.ArtifactTypeID}/${this.selected.ID}`);
    }

    private artifactLink(artifactTypeId, artifactId) {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${artifactTypeId}/${artifactId}`);           
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

    private columnSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
        this.items = _.orderBy(this.items, [item => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);
    }
}


