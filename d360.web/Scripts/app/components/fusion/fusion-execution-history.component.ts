import * as _ from 'lodash';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";
import {Component, Input, OnInit} from '@angular/core';
import {Router} from '@angular/router';

import {FusionConfigurationDetails, FusionWorkerExecution} from '../../models/fusion.model';

import {FusionService} from '../../services/fusion.service';

import {SiteUrlHelpers} from '../../static/site-url-helpers';

import {BaseComponent} from '../shared/base.component';

@Component({
    selector: 'd3s-fusion-execution-history',
    template: `
        <div class="tile tile-detail" *ngIf="!showExecutionErrors && !showExecutionResults">
            <header>Execution History<span *ngIf="fusion"> - {{fusion.Name}}</span>
                <d3s-tile-actions [hasAdd]="false" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"
                                  [hasRefresh]="true" (refreshClick)="load();" [hasExport]="true"
                                  (exportClick)="export()"></d3s-tile-actions>
            </header>
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <span *ngIf="!isLoading">
                        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100"
                               (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..."
                               class="grid-simple-filter">
                        <p-table #dt [value]="executions" [scrollable]="true" scrollWidth="100%" selectionMode="single"
                                 [metaKeySelection]="true"
                                 [globalFilterFields]="['FusionType','Fusion','DateStarted','DateCompleted','ErrorCount','ResultCount','Adds','Deletes','Updates','RawLogFileName']"
                                 [pageLinks]="3" [paginator]="true" [rows]="5" [rowsPerPageOptions]="[5,10,20]"
                                 [(selection)]="selected">
                            <ng-template pTemplate="colgroup">
                                <colgroup>
                                    <col style="width:175px">
                                    <col style="width:175px">
                                    <col style="width:150px">
                                    <col style="width:150px">
                                    <col style="width:100px">
                                    <col style="width:100px">
                                    <col style="width:100px">
                                    <col style="width:100px">
                                    <col style="width:100px">
                                    <col style="width:250px">
                                </colgroup>
                            </ng-template>
                            <ng-template pTemplate="header">
                                <tr>
                                    <th [pSortableColumn]="'FusionType'" style="width: 175px">
                                        Type
                                        <d3s-sortIcon [field]="'FusionType'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'Fusion'" style="width: 175px">
                                        Configuration
                                        <d3s-sortIcon [field]="'Fusion'"></d3s-sortIcon>
                                    </th>
                                    <th style="width: 150px" [pSortableColumn]="'DateStarted'">
                                        Started
                                        <d3s-sortIcon [field]="'DateStarted'"></d3s-sortIcon>
                                    </th>
                                    <th style="width: 150px" [pSortableColumn]="'DateCompleted'">
                                        Completed
                                        <d3s-sortIcon [field]="'DateCompleted'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'ErrorCount'" style="width: 100px">
                                        Errors
                                        <d3s-sortIcon [field]="'ErrorCount'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'ResultCount'" style="width: 100px">
                                        Results
                                        <d3s-sortIcon [field]="'ResultCount'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'Adds'" style="width: 100px">
                                        Adds
                                        <d3s-sortIcon [field]="'Adds'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'Deletes'" style="width: 100px">
                                        Deletes
                                        <d3s-sortIcon [field]="'Deletes'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'Updates'" style="width: 100px">
                                        Updates
                                        <d3s-sortIcon [field]="'Updates'"></d3s-sortIcon>
                                    </th>
                                    <th style="width: 250px">Data File</th>
                                </tr>
                                <tr [hidden]="showSimpleFilter">
                                    <th><d3s-column-filter [field]="'FusionType'"
                                                           [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'Fusion'"
                                                           [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'DateStarted'"
                                                           [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'DateCompleted'"
                                                           [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'ErrorCount'"
                                                           [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'ResultCount'"
                                                           [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'Adds'"
                                                           [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'Deletes'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'Updates'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'RawLogFileName'"
                                                           [datatype]="'text'"></d3s-column-filter></th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr (dblclick)="selected=item" [pSelectableRow]="item">
                                    <td>{{item.FusionType}}</td>
                                    <td>
                                        <a (click)="showFusion(item)">{{item.Fusion}}</a>
                                    </td>
                                    <td>
                                        <span>{{item.DateStarted | date: 'short'}}</span>
                                    </td>
                                    <td>
                                        <span>{{item.DateCompleted | date: 'short'}}</span>
                                    </td>
                                    <td>
                                        <a *ngIf="item.ErrorCount"
                                           (click)="selected=item;showExecutionErrors=true;">{{item.ErrorCount}} <i
                                                class="fa fa-times disabled"></i></a>
                                        <span *ngIf="!item.ErrorCount">{{item.ErrorCount}}</span>
                                    </td>
                                    <td>
                                        <a *ngIf="item.ResultCount"
                                           (click)="selected=item;showExecutionResults=true;">{{item.ResultCount}} <i
                                                class="fa fa-check enabled"></i></a>
                                        <span *ngIf="!item.ResultCount">{{item.ResultCount}}</span>
                                    </td>
                                    <td>{{item.Adds}}</td>
                                    <td>{{item.Deletes}}</td>
                                    <td>{{item.Updates}}</td>
                                    <td>
                                        <a (click)="downloadFusionData(item);">{{item.RawLogFileName}}</a>
                                    </td>
                                </tr>
                            </ng-template>
                            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows"
                                                      [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            </ng-template>
                        </p-table>  
                    </span>
        </div>
        <div class="tile tile-detail" *ngIf="showExecutionErrors && selected">
            <d3s-fusion-execution-errors [executionId]="selected.ID"></d3s-fusion-execution-errors>
            <button pButton type="button" (click)="showExecutionErrors=false;" label="Close"
                    style="width: 150px;"></button>
        </div>
        <div class="tile tile-detail" *ngIf="showExecutionResults && selected">
            <d3s-fusion-execution-results [executionId]="selected.ID"></d3s-fusion-execution-results>
            <button pButton type="button" (click)="showExecutionResults=false;" label="Close"
                    style="width: 150px;"></button>
        </div>
    `,
    providers: [FusionService],
})

export class FusionExecutionHistoryComponent extends BaseComponent implements OnInit {
    @Input() maxRows: number = 100;
    @Input() fusion: FusionConfigurationDetails;

    private executions: FusionWorkerExecution[] = [];
    private selected: FusionWorkerExecution;

    private showExecutionResults: boolean = false;
    private showExecutionErrors: boolean = false;

    destroySubject$: Subject<void> = new Subject();

    constructor(
        private router: Router,
        private fusionService: FusionService
    ) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;

        this.fusionService
            .getFusionWorkerExecutionHistory(
                this.maxRows,
                this.fusion ? this.fusion.ID : undefined
            )
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                res => {
                    this.executions = res;
                    this.selected = this.executions.length > 0 ? this.executions[0] : null;
                    this.isLoading = false;
                }
            );
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

    private export() {
        this.fusionService.getFusionWorkerExecutionHistoryExport(this.maxRows, this.fusion ? this.fusion.ID : undefined);
    }
}
