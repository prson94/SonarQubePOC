///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { Fusion } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-configuration',
    template: ` 
                <div class="tile tile-detail" *ngIf="showFusionFilter">
                    <div style="text-align:right;"><a (click)="showFusionFilter=false;" style="cursor:pointer;color:black;display:block; padding:0 5px 0 5px; background-color: #c3c3c3;"><i class="fa fa-2x fa-remove"></i></a></div>
                    <d3s-fusion-filters-tile [fusionTypeID]="selected?.FusionTypeID" [fusionID]="selected?.ID"></d3s-fusion-filters-tile>                                    
                </div>
                <div class="tile tile-detail" *ngIf="!showFusionFilter">
                    <header>Configuration <d3s-tile-actions [hasAdd]="false" [hasExport]="true" (exportClick)="doExport()"></d3s-tile-actions></header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                                              
                        <p-dataTable [globalFilter]="gb" [value]="fusions" selectionMode="single" [rows]="10" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showFusion();" >
                                            <p-column field="FusionType" header="Type" [sortable]="true" [style]="{width:'20%'}"></p-column>
                                            <p-column field="Name" header="Name" [sortable]="true" [style]="{width:'25%'}"></p-column>
                                            <p-column field="Description" header="Description" [sortable]="true" [style]="{width:'25%'}">
                                                <template let-item="rowData" pTemplate type="body">
                                                    <span [innerHtml]="item.Description"></span>
                                                </template>
                                            </p-column>
                                            <p-column field="Enabled" header="Enabled" [sortable]="true" [style]="{width:'11%'}">
                                                <template let-item="rowData" pTemplate type="body">
                                                    <i *ngIf="item.Enabled" class="fa fa-check enabled" title="Enabled"></i>
                                                    <i *ngIf="!item.Enabled" class="fa fa-times disabled" title="Disabled"></i>
                                                </template>
                                            </p-column>
                            <p-column [style]="{width:'30px'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools">                                
                                        <d3s-tooltip objectType="Fusion" [objectId]="item.ID" tooltipType="preview"><i class="fa fa-info"></i></d3s-tooltip>                                    
                                    </div>
                                </template>
                            </p-column>
                            <p-column [style]="{width:'30px'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <div class="RowTools" (click)="showFusionFilter=true;">                                
                                        <i class="fa fa-filter"></i>
                                    </div>
                                </template>
                            </p-column>
                        </p-dataTable>      
                    </span>
                </div>
                `,
    providers: [FusionService],        
})

export class FusionConfigurationComponent extends BaseComponent implements OnInit {

    private fusions: Fusion[] = [];
    private selected: Fusion;

    private showFusionFilter: boolean = false;
     
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

    private showFusion() {
        if (!this.selected) {
            console.log("ERROR NO SELECTED FUSION ITEM TO NAVIGATE TO.");

            return;
        }
        this.router.navigateByUrl(`/a/fusion/${this.selected.ID}`);
    }

    private doExport() {
        this.fusionService.exportFusionConfigurations();
    }
    
};