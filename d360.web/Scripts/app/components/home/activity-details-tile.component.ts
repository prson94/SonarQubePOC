import { Component, OnInit, Output, EventEmitter, Input} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { ArtifactService } from '../../services/artifacts.service';
import { Count} from '../../models/counts.model';
import { AssetDetail } from '../../models/asset.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-activity-details-tile',
    providers: [ArtifactService],
    template: `
                <div class="tile tile-detail">
                   <header>Activity for {{objectName}}
                    <d3s-tile-actions [hasAdd]="false" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                   </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                        <p-dataTable #dt [globalFilter]="gb" [value]="items" selectionMode="single" [(selection)]="selected" (onRowDblclick)="selected=$event.data;navigateToArtifact();" scrollable="true" scrollWidth="100%" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions">                    
                            <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                            <p-column field="DisplayValue" header="Name" sortable="true" [filter]="!showSimpleFilter">
                                <ng-template let-col let-item="rowData" pTemplate type="body">
                                    <a (click)="artifactLink(item.TypeID, item.ObjectID)">{{item.DisplayValue}}</a>
                                </ng-template>
                            </p-column>                                                                                                   
                            <p-column field="CreatedOn" header="Created" sortable="true" [filter]="!showSimpleFilter" [style]="{'width':'150px'}">
                                <ng-template let-col let-data="rowData" pTemplate type="body">
                                    <span>{{data.CreatedOn | date: 'shortDate'}}</span>
                                </ng-template>
                            </p-column>
                            <p-column field="UpdatedOn" header="Updated" sortable="true" [filter]="!showSimpleFilter" [style]="{'width':'150px'}">
                                <ng-template let-col let-data="rowData" pTemplate type="body">
                                    <span>{{data.UpdatedOn | date: 'shortDate'}}</span>
                                </ng-template>
                            </p-column>                            
                            <p-column [style]="{width:'40px'}">
                                <ng-template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools">                                        
                                        <d3s-preview-tooltip objectType="Artifact" [objectId]="item.ObjectID" (click)="selectArtifact(item)" icon="info"></d3s-preview-tooltip>
                                    </div>
                                </ng-template>
                            </p-column>
                        </p-dataTable>      
                    </span>
                    <button pButton type="button" (click)="close.emit();" label="Close" style="width:150px;margin-top:10px"></button>                    
                </div>
                `
})

export class ActivityDetailsTile extends BaseComponent implements OnInit {    
    private items: AssetDetail[] = [];
    private selected: AssetDetail;

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
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${this.selected.TypeID}/${this.selected.ObjectID}`);
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
}


