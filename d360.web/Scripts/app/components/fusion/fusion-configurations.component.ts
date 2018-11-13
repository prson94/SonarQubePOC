import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { Fusion } from '../../models/fusion.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';


@Component({
    selector: 'd3s-fusion-configuration',
    template: `                 
                <div class="tile tile-detail">
                    <header>Configuration <d3s-tile-actions [hasAdd]="false" [hasExport]="true" (exportClick)="doExport()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">                   
                        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                        <p-table #dt [value]="fusions" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name','FusionType','Description','Enabled']" sortField="Name" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="10" [rowsPerPageOptions]="[5,10,20]" [(selection)]="selected">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th [pSortableColumn]="'Name'" style="width: 25%">
                                        Name
                                        <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'FusionType'" style="width: 20%">
                                        Type
                                        <d3s-sortIcon [field]="'FusionType'"></d3s-sortIcon>
                                    </th>
                                    <th style="width: 25%">Description</th>
                                    <th [pSortableColumn]="'Enabled'" style="width: 11%">
                                        Enabled
                                        <d3s-sortIcon [field]="'Enabled'"></d3s-sortIcon>
                                    </th>                                    
                                    <th style="width: 30px"></th>
                                </tr>
                                <tr [hidden]="showSimpleFilter">
                                    <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'FusionType'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'Description'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'Enabled'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th></th>                                    
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr (dblclick)="selected=item;showFusion(selected);" [pSelectableRow]="item">
                                    <td>
                                            <a (click)="showFusion(item)">{{item.Name}}</a>
                                    </td>
                                    <td>{{item.FusionType}}</td>
                                    <td>
                                            <span [innerHtml]="item.Description"></span>
                                    </td>
                                    <td>
                                            <i *ngIf="item.Enabled" class="fa fa-check enabled" title="Enabled"></i>
                                            <i *ngIf="!item.Enabled" class="fa fa-times disabled" title="Disabled"></i>
                                    </td>
                                    <td>
                                        <div class="RowTools">
                                            <d3s-preview-tooltip objectType="Fusion" [objectId]="item.ID" icon="info"></d3s-preview-tooltip>
                                        </div>
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

export class FusionConfigurationComponent extends BaseComponent implements OnInit {

    private fusions: Fusion[] = [];
    private selected: Fusion;

    constructor(private fusionService: FusionService, private router: Router) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.fusionService.getFusionConfigurations()
            .then(res => {
                this.isLoading = false;
                this.fusions = res;                
                this.selected = this.fusions.length > 0 ? this.fusions[0] : null;
            });
    }

    private showFusion(fusion) {
        if (!fusion) {
            console.log("ERROR NO SELECTED FUSION ITEM TO NAVIGATE TO.");

            return;
        }
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('FusionType', fusion.ID));        
    }

    private doExport() {
        this.fusionService.exportFusionConfigurations();
    }    
};