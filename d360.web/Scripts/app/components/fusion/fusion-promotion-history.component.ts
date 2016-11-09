import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { FusionPromotionExecutionStats } from '../../models/fusion.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-fusion-promotion-history',
    template: `                 
                <div class="tile tile-detail">
                    <header>Promotion History<d3s-tile-actions [hasAdd]="false" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                        <p-dataTable #dt [globalFilter]="gb" scrollable="true" scrollWidth="100%" [value]="executions" selectionMode="single" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" [(selection)]="selected" (onRowDblclick)="selected=$event.data" >                        
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column field="DateStarted" header="Started" sortable="custom" (sortFunction)="nullDateSort($event)" [style]="{width:'150px'}" [filter]="!showSimpleFilter">
                                <template let-col let-data="rowData" pTemplate type="body">
                                    <span>{{data.DateStarted | date: 'short'}}</span>
                                </template>
                            </p-column>
                            <p-column field="DateCompleted" header="Completed" sortable="custom" (sortFunction)="nullDateSort($event)" [style]="{width:'150px'}" [filter]="!showSimpleFilter">
                                <template let-col let-data="rowData" pTemplate type="body">
                                    <span>{{data.DateCompleted | date: 'short'}}</span>
                                </template>
                            </p-column>                        
                            <p-column field="TotalNewPromotions" header="# New Promotions" [sortable]="true" [style]="{width:'150px'}" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="PromotedArtifacts" header="# New Artifacts" [sortable]="true" [style]="{width:'150px'}" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="PromotedDomains" header="# New Domains" [sortable]="true" [style]="{width:'150px'}" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="PromotedDomainItems" header="# New Domain Items" [sortable]="true" [style]="{width:'150px'}" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="PromotedTaxonomies" header="# New Taxonomies" [sortable]="true" [style]="{width:'150px'}" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="RelationshipsAdded" header="# New Relationships" [sortable]="true" [style]="{width:'150px'}" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="NumberOfRules" header="# Rules" [sortable]="true" [style]="{width:'150px'}" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="AttributesConsidered" header="# Attributes Considered" [sortable]="true" [style]="{width:'150px'}" [filter]="!showSimpleFilter"></p-column>
                        </p-dataTable>      
                    </span>
                </div>
                `,
    providers: [FusionService],
})

export class FusionPromotionHistoryComponent extends BaseComponent implements OnInit {
    @Input() maxRows: number = 100;

    private executions: FusionPromotionExecutionStats[] = [];
    private selected: FusionPromotionExecutionStats;

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.fusionService.getFusionPromotionHistory(this.maxRows)
            .then(res => {
                this.executions = res;
                this.selected = res.length > 0 ? res[0] : null;
                this.isLoading = false;
            });
    }

    private nullDateSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                
        this.executions = _.sortBy(this.executions, event.field);
        if (event.order == -1) this.executions.reverse();
    }    
};