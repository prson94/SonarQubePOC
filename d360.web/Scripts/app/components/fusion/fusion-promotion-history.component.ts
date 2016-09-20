///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { FusionPromotionExecutionStats } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-promotion-history',
    template: `                 
                <div class="tile tile-detail">
                    <header>Promotion History</header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                                              
                        <p-dataTable [globalFilter]="gb" scrollable="true" scrollWidth="100%" [value]="executions" selectionMode="single" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" [(selection)]="selected" (onRowDblclick)="selected=$event.data" >                        
                            <p-column field="DateStarted" header="Started" [sortable]="true" [style]="{width:'150px'}">
                                <template let-col let-data="rowData" pTemplate type="body">
                                    <span>{{data.DateStarted | date: 'short'}}</span>
                                </template>
                            </p-column>
                            <p-column field="DateCompleted" header="Completed" [sortable]="true" [style]="{width:'150px'}">
                                <template let-col let-data="rowData" pTemplate type="body">
                                    <span>{{data.DateCompleted | date: 'short'}}</span>
                                </template>
                            </p-column>                        
                            <p-column field="TotalNewPromotions" header="# New Promotions" [sortable]="true" [style]="{width:'150px'}"></p-column>
                            <p-column field="PromotedArtifacts" header="# New Artifacts" [sortable]="true" [style]="{width:'150px'}"></p-column>
                            <p-column field="PromotedDomains" header="# New Domains" [sortable]="true" [style]="{width:'150px'}"></p-column>
                            <p-column field="PromotedDomainItems" header="# New Domain Items" [sortable]="true" [style]="{width:'150px'}"></p-column>
                            <p-column field="PromotedTaxonomies" header="# New Taxonomies" [sortable]="true" [style]="{width:'150px'}"></p-column>
                            <p-column field="RelationshipsAdded" header="# New Relationships" [sortable]="true" [style]="{width:'150px'}"></p-column>
                            <p-column field="NumberOfRules" header="# Rules" [sortable]="true" [style]="{width:'150px'}"></p-column>
                            <p-column field="AttributesConsidered" header="# Attributes Considered" [sortable]="true" [style]="{width:'150px'}"></p-column>
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
};