import { Component, Input, OnChanges, SimpleChange} from '@angular/core';
import { Report, ReportTile } from '../../models/report.model';
import { MessagesService, ReportsService  } from '../../services/index';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-admin-report-item',
    providers: [ReportsService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Tiles on this Dashboard
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                   <p-dataTable #dt [globalFilter]="gb" [value]="tiles" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="selected=$event.data;showEditor=true" [(selection)]="selected" >                                                                        
                    <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                    <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                                                                                
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
                <d3s-admin-report-tile-editor *ngIf="showEditor" [reportId]="report?.ID" [tile]="selected" (saveClick)="saveTile($event);" (closeClick)="showEditor=false"></d3s-admin-report-tile-editor>              
                `
})

export class AdminReportItemsComponent extends BaseComponent implements OnChanges {
    @Input() report: Report = null;

    error: any;
    
    showEditor: boolean = false;
    showDelete: boolean = false;
    isLoading: boolean = false;

    tiles: ReportTile[] = [];
    selected: ReportTile;

    theDeleteCallback: Function;

    constructor(private reportsService: ReportsService, private messagesService: MessagesService) {
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
        this.reportsService.deleteReportTile(id).then(result => {
            this.showMessageForResult(this.messagesService, result);
            if (result.type != 'error') this.tiles = this.tiles.filter(x => x.ID != id);
            this.showDelete = false;            
        });
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
    
    saveTile(event) {
        this.isLoading = true;
        this.reportsService.saveTile(event.tile)
            .then(result => {
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, result);
                this.getTiles();
                this.showEditor = false;
            });        
    }
    
}


