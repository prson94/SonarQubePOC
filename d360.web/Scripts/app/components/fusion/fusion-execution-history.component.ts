import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { FusionWorkerExecution, FusionConfigurationDetails } from '../../models/fusion.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-fusion-execution-history',
    template: `                 
                <div class="tile tile-detail" *ngIf="!showExecutionErrors && !showExecutionResults">
                    <header>Execution History<span *ngIf="fusion"> - {{fusion.Name}}</span><d3s-tile-actions [hasAdd]="false" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" [hasRefresh]="true" (refreshClick)="load();"></d3s-tile-actions></header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                        <p-dataTable #dt [globalFilter]="gb" scrollable="true" scrollWidth="100%" [value]="executions" selectionMode="single" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" [(selection)]="selected" (onRowDblclick)="selected=$event.data" >
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column field="FusionType" header="Type" sortable="true" [style]="{width:'175px'}" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="Fusion" header="Configuration" sortable="true" [style]="{width:'175px'}" [filter]="!showSimpleFilter">
                                <template let-item="rowData" pTemplate type="body">
                                    <a (click)="showFusion(item)">{{item.Fusion}}</a>
                                </template>
                            </p-column>
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
                            <p-column field="ErrorCount" header="Errors" [sortable]="true" [style]="{width:'100px'}" [filter]="!showSimpleFilter">
                                <template let-col let-data="rowData" pTemplate type="body">
                                    <a *ngIf="data.ErrorCount" (click)="selected=data;showExecutionErrors=true;">{{data.ErrorCount}} <i class="fa fa-times disabled"></i></a>
                                    <span *ngIf="!data.ErrorCount">{{data.ErrorCount}}</span>
                                </template>
                            </p-column>
                            <p-column field="ResultCount" header="Results" [sortable]="true" [style]="{width:'100px'}" [filter]="!showSimpleFilter">
                                <template let-col let-data="rowData" pTemplate type="body">
                                    <a *ngIf="data.ResultCount" (click)="selected=data;showExecutionResults=true;">{{data.ResultCount}} <i class="fa fa-check enabled"></i></a>
                                    <span *ngIf="!data.ResultCount">{{data.ResultCount}}</span>
                                </template>
                            </p-column>                            
                            <p-column field="Adds" header="Adds" [sortable]="true" [style]="{width:'100px'}" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="Deletes" header="Deletes" [sortable]="true" [style]="{width:'100px'}" [filter]="!showSimpleFilter"></p-column>
                            <p-column field="Updates" header="Updates" [sortable]="true" [style]="{width:'100px'}" [filter]="!showSimpleFilter"></p-column>                            
                            <p-column field="RawLogFileName" header="Data File" [sortable]="false" [filter]="!showSimpleFilter" [style]="{width:'250px'}">
                                <template let-data="rowData" pTemplate type="body">
                                    <a (click)="downloadFusionData(data);">{{data.RawLogFileName}}</a>
                                </template>
                            </p-column>
                        </p-dataTable>      
                    </span>                    
                </div>                
                <div class="tile tile-detail" *ngIf="showExecutionErrors && selected">
                    <header>Execution History - Error Details</header>
                    <d3s-fusion-execution-errors [executionId]="selected.ID"></d3s-fusion-execution-errors>
                    <button pButton type="button" (click)="showExecutionErrors=false;" label="Close" style="width: 150px;"></button>
                </div>
                <div class="tile tile-detail" *ngIf="showExecutionResults && selected">
                    <header>Execution History - Result Details</header>
                    <d3s-fusion-execution-results [executionId]="selected.ID"></d3s-fusion-execution-results>
                    <button pButton type="button" (click)="showExecutionResults=false;" label="Close" style="width: 150px;"></button>
                </div>
          `,
        providers: [FusionService],                
})

export class FusionExecutionHistoryComponent extends BaseComponent implements OnInit {
    @Input() maxRows: number = 100;

    private executions: FusionWorkerExecution[] = [];
    private selected: FusionWorkerExecution;

    private showExecutionResults: boolean = false;
    private showExecutionErrors: boolean = false;

    @Input() fusion: FusionConfigurationDetails;

    constructor(private router: Router, private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.fusionService.getFusionWorkerExecutionHistory(this.maxRows, this.fusion ? this.fusion.ID : undefined)
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
    
    private showFusion(fusion: FusionWorkerExecution) {
        if (!fusion) {
            console.log("ERROR NO SELECTED FUSION ITEM TO NAVIGATE TO.");

            return;
        }
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('FusionType', fusion.FusionID));
    }

    private downloadFusionData(data: FusionWorkerExecution) {
        this.fusionService.downloadRawFusionData(data.ID, data.RawLogFileName);
    }
};