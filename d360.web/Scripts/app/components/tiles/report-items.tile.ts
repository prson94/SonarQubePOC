///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, OnChanges, SimpleChange} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { Report, ReportTile } from '../../models/report.model';
import { MessagesService, ReportsService  } from '../../services/index';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import {DeleteForm} from '../forms/delete.form';


@Component({
    selector: 'd3s-report-item-tile',
    directives: [DataTable, Column, TileActionsComponent, DeleteForm],
    providers: [ReportsService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Tiles on this Dashboard
                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Tile'" (addClick)="add()"></d3s-tile-actions>                            
               </header>
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
               <p-dataTable *ngIf="!isLoading && !showDelete && !showEditor" [value]="tiles" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                        
                
                <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                                                            
                <p-column field="ContentAreaNumber" header="Content Area #" [sortable]="true" [filter]="true"></p-column>
                <p-column [style]="{width:'40px'}">
                    <template let-template="rowData">
                        <div class="RowTools">
                            <a style="cursor:pointer;" (click)="showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                        </div>
                    </template>
                </p-column>                            
                    <p-column  [style]="{width:'40px'}">
                        <template let-template="rowData">
                            <div class="RowTools">                                
                                <a style="cursor:pointer;" (click)="showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                            </div>
                        </template>
                    </p-column>                            
                </p-dataTable>      
                <delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the tile [' + [selected?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></delete-form>                 
                `
})

export class ReportItemsTile implements OnChanges {
    @Input() report: Report = null;

    error: any;
    
    showEditor: boolean = false;
    showDelete: boolean = false;
    isLoading: boolean = false;

    tiles: ReportTile[] = [];
    selected: ReportTile;

    theDeleteCallback: Function;

    constructor(private reportsService: ReportsService) {
        this.theDeleteCallback = this.deleteTile.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.report != null) this.getTiles();
    }

    getTiles() {
        this.isLoading = true;
        this.reportsService
            .getReportTiles(this.report)
            .then(result => {
                this.tiles = result;
                this.selected = (this.tiles.length > 0 ? this.tiles[0] : null);
                this.isLoading = false;
            })
            .catch(error => this.error = error);
    }

    deleteTile(id: number) {
        this.reportsService.deleteReportTile(id);
        this.showDelete = false;
        this.tiles.splice(this.findReportTileIndex(id), 1);
    }

    add() {
        this.showEditor = true;
        this.selected = null;
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null && this.tiles.length > 0)
            this.selected = this.tiles[0];
    }

    findReportTileIndex(id: number) {
        var index: number = -1;
        for (var tile of this.tiles) {
            index++;
            if (tile.ID == id) return index;
        }
    }
    
}


