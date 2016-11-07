import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { FusionAgentExecutionStats, FusionConfigurationDetails } from '../../models/fusion.model';
import * as _ from 'lodash';

@Component({
        selector: 'd3s-fusion-agent-history',
        template: `                 
                <div class="tile tile-detail">
                    <header>Agent History<span *ngIf="fusion"> - {{fusion.Name}}</span><d3s-tile-actions [hasAdd]="false" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span  *ngIf="!isLoading">
                        <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                        <p-dataTable [globalFilter]="gb" [value]="executions" selectionMode="single" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" [(selection)]="selected" (onRowDblclick)="selected=$event.data" >
                            <p-column field="FusionType" header="Type" sortable="custom" (sortFunction)="caseInsensitiveSort($event)" [style]="{width:'20%'}" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="Fusion" header="Configuration" sortable="custom" (sortFunction)="caseInsensitiveSort($event)" [style]="{width:'20%'}" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="DateStarted" header="Started" [sortable]="true" [style]="{width:'20%'}" [filter]="!showSimpleFilter">
                                <template let-col let-data="rowData" pTemplate type="body">
                                    <span>{{data.DateStarted | date: 'short'}}</span>
                                </template>
                            </p-column>
                            <p-column field="DateCompleted" header="Completed" sortable="custom" (sortFunction)="nullDateSort($event)" [style]="{width:'20%'}" [filter]="!showSimpleFilter">
                                <template let-col let-data="rowData" pTemplate type="body">
                                    <span>{{data.DateCompleted | date: 'short'}}</span>
                                </template>
                            </p-column>
                            <p-column field="Success" header="Success" [sortable]="true" [style]="{width:'20%'}" [filter]="!showSimpleFilter">
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

    
    private nullDateSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                
        this.executions = _.sortBy(this.executions, event.field);
        if (event.order == -1) this.executions.reverse();
    }

    private caseInsensitiveSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending        
        this.executions = _.orderBy(this.executions, [item => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);
    }
};