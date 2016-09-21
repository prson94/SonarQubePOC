import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { FusionAgentExecutionStats, FusionConfigurationDetails } from '../../models/fusion.model';

@Component({
        selector: 'd3s-fusion-agent-history',
        template: `                 
                <div class="tile tile-detail">
                    <header>Agent History<span *ngIf="fusion"> - {{fusion.Name}}</span></header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span  *ngIf="!isLoading">
                        <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                                              
                        <p-dataTable [globalFilter]="gb" [value]="executions" selectionMode="single" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" [(selection)]="selected" (onRowDblclick)="selected=$event.data" >
                            <p-column field="FusionType" header="Type" [sortable]="true" [style]="{width:'20%'}"></p-column>
                            <p-column field="Fusion" header="Configuration" [sortable]="true" [style]="{width:'20%'}"></p-column>
                            <p-column field="DateStarted" header="Started" [sortable]="true" [style]="{width:'20%'}">
                                <template let-col let-data="rowData" pTemplate type="body">
                                    <span>{{data.DateStarted | date: 'short'}}</span>
                                </template>
                            </p-column>
                            <p-column field="DateCompleted" header="Completed" [sortable]="true" [style]="{width:'20%'}">
                                <template let-col let-data="rowData" pTemplate type="body">
                                    <span>{{data.DateCompleted | date: 'short'}}</span>
                                </template>
                            </p-column>
                            <p-column field="Success" header="Success" [sortable]="true" [style]="{width:'20%'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <i *ngIf="item.Success" class="fa fa-check enabled" title="Success"></i>
                                    <i *ngIf="!item.Success && item.DateCompleted" class="fa fa-times disabled" title="Failure"></i>
                                </template>
                            </p-column>
                        </p-dataTable>      
                    </span>
                </div>
                `,
        providers: [FusionService],
})

export class FusionAgentHistoryComponent extends BaseComponent implements OnInit {
    @Input() maxRows: number = 100;

    private executions: FusionAgentExecutionStats[] = [];
    private selected: FusionAgentExecutionStats;

    @Input() fusion: FusionConfigurationDetails;

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.fusionService.getFusionAgentHistory(this.maxRows, this.fusion ? this.fusion.ID : undefined)
            .then(res => {
                this.executions = res;
                this.selected = this.executions.length > 0 ? this.executions[0] : null;
                this.isLoading = false;
            });
    }
};