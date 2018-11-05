import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { FusionAgentExecutionStats, FusionConfigurationDetails } from '../../models/fusion.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import * as _ from 'lodash';

@Component({
        selector: 'd3s-fusion-agent-history',
        template: `                 
                <div class="tile tile-detail">
                    <header>Agent History<span *ngIf="fusion"> - {{fusion.Name}}</span><d3s-tile-actions [hasRefresh]="true" (refreshClick)="load()" [hasAdd]="false" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" [hasExport]="true" (exportClick)="export()"></d3s-tile-actions></header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                        <p-table #dt [value]="executions" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['FusionType','Fusion','DateStarted','DateCompleted','Success']" [pageLinks]="3" [paginator]="true" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [(selection)]="selected">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th [pSortableColumn]="'FusionType'" style="width: 20%">
                                        Type
                                        <d3s-sortIcon [field]="'FusionType'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'Fusion'" style="width: 20%">
                                        Configuration
                                        <d3s-sortIcon [field]="'Fusion'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'DateStarted'" style="width: 20%">
                                        Started
                                        <d3s-sortIcon [field]="'DateStarted'"></d3s-sortIcon>
                                    </th>
                                    <th style="width: 20%">Completed</th>
                                    <th [pSortableColumn]="'Success'" style="width: 20%">
                                        Success
                                        <d3s-sortIcon [field]="'Success'"></d3s-sortIcon>
                                    </th>
                                </tr>
                                <tr [hidden]="showSimpleFilter">
                                    <th><d3s-column-filter [field]="'FusionType'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'Fusion'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'DateStarted'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'DateCompleted'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'Success'" [datatype]="'text'"></d3s-column-filter></th>
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
                                        <i *ngIf="item.Success" class="fa fa-check enabled" title="Success"></i>
                                        <i *ngIf="!item.Success && item.DateCompleted" class="fa fa-times disabled" title="Failure"></i>
                                    </td>
                                </tr>
                            </ng-template>
                            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            </ng-template>
                        </p-table>  
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

    constructor(private router: Router, private fusionService: FusionService) {
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
    
    private showFusion(fusion: FusionAgentExecutionStats) {
        if (!fusion) {
            console.log("ERROR NO SELECTED FUSION ITEM TO NAVIGATE TO.");

            return;
        }
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('FusionType', fusion.FusionID));
    }

    private export() {
        this.fusionService.getFusionAgentHistoryExport(this.maxRows, this.fusion ? this.fusion.ID : undefined)
    }
};