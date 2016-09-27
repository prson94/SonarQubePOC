
import { Component, Input, OnChanges, SimpleChange} from '@angular/core';
import { Report, ReportTile } from '../../models/report.model';
import { MessagesService, ReportsService  } from '../../services/index';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-report-item-tile',
    providers: [ReportsService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Tiles on this Dashboard
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">
                   <p-dataTable [globalFilter]="gb" [value]="tiles" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                    <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                                                            
                    <p-column field="ContentAreaNumber" header="Content Area #" [sortable]="true" [filter]="!showSimpleFilter"></p-column>
                    <p-column [style]="{width:'40px'}">
                        <template let-template="rowData" pTemplate type="body">
                            <div class="RowTools">
                                <a style="cursor:pointer;" (click)="showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                            </div>
                        </template>
                    </p-column>                            
                        <p-column  [style]="{width:'40px'}">
                            <template let-template="rowData" pTemplate type="body">
                                <div class="RowTools">                                
                                    <a style="cursor:pointer;" (click)="showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                </div>
                            </template>
                        </p-column>                            
                    </p-dataTable>  
                </span>    
                <delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the tile [' + [selected?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></delete-form>                 
                `
})

export class ReportItemsTile extends BaseComponent implements OnChanges {
    @Input() report: Report = null;

    error: any;
    
    showEditor: boolean = false;
    showDelete: boolean = false;
    isLoading: boolean = false;

    tiles: ReportTile[] = [];
    selected: ReportTile;

    theDeleteCallback: Function;

    constructor(private reportsService: ReportsService) {
        super();
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


