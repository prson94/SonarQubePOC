///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { FusionWorkerExecution } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-execution-history',
    template: `                 
                <div class="tile tile-detail">
                    <header>Execution History</header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                                              
                        <p-dataTable [globalFilter]="gb" scrollable="true" scrollWidth="100%" [value]="executions" selectionMode="single" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" [(selection)]="selected" (onRowDblclick)="selected=$event.data" >
                            <p-column field="FusionType" header="Type" [sortable]="true" [style]="{width:'175px'}"></p-column>
                            <p-column field="Fusion" header="Configuration" [sortable]="true" [style]="{width:'175px'}"></p-column>
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
                            <p-column field="ErrorCount" header="Errors" [sortable]="true" [style]="{width:'100px'}"></p-column>
                            <p-column field="ResultCount" header="Results" [sortable]="true" [style]="{width:'100px'}"></p-column>
                            <p-column field="Adds" header="Adds" [sortable]="true" [style]="{width:'100px'}"></p-column>
                            <p-column field="Deletes" header="Deletes" [sortable]="true" [style]="{width:'100px'}"></p-column>
                            <p-column field="Updates" header="Updates" [sortable]="true" [style]="{width:'100px'}"></p-column>
                            <p-column [style]="{width:'40px'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools">                                
                                        <i class="fa fa-info"></i>
                                    </div>
                                </template>
                            </p-column>
                        </p-dataTable>      
                    </span>
                </div>
          `,
        providers: [FusionService],                
})

export class FusionExecutionHistoryComponent extends BaseComponent implements OnInit {
    @Input() maxRows: number = 100;

    private executions: FusionWorkerExecution[] = [];
    private selected: FusionWorkerExecution;
    
    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.fusionService.getFusionWorkerExecutionHistory(this.maxRows)
            .then(res => {
                this.executions = res;
                this.selected = this.executions.length > 0 ? this.executions[0] : null;
                this.isLoading = false;
            });
    }
};