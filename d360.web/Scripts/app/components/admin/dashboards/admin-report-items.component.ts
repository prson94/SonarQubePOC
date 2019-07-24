import { Component, Input, OnChanges, SimpleChange} from '@angular/core';
import { Report, ReportTile } from '../../../models/report.model';
import { ReportsService  } from '../../../services/reports.service';
import { BaseComponent } from '../../shared/base.component';
import { catchError } from 'rxjs/operators';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-report-item',
    providers: [ReportsService],
    template: `
               <header *ngIf="!showEditor && !showDelete">Tiles on this Dashboard
                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
               </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading && !showDelete && !showEditor">
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                    <p-table #dt [value]="tiles" selectionMode="single" [globalFilterFields]="['Name']" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selected">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'">
                                    Name
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                                <th style="width: 40px"></th>
                                <th style="width: 40px"></th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
                                <th></th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr (dblclick)="selected=item;showEditor=true" [pSelectableRow]="item">
                                <td>{{item.Name}}</td>
                                <td>
                                    <div class="RowTools">
                                        <a style="cursor:pointer;" (click)="selected=item;showEditor=true"><i class="fa fa-pencil"></i></a>
                                    </div>
                                </td>
                                <td>
                                    <div class="RowTools">
                                        <a style="cursor:pointer;" (click)="selected=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>                   
                </span>    
                <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the tile [' + [selected?.Name] + ']?'"                                         
                    (onCancel)="showDelete=false;"
                ></d3s-delete-form>   
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

    constructor(private reportsService: ReportsService, private messagesService: MessagesObservableService) {
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
            .subscribe(result => {
                this.tiles = result;
                this.selected = (this.tiles.length > 0 ? this.tiles[0] : null);
                this.isLoading = false;
            }, catchError(error => this.error = error));
    }

    deleteTile(id: number) {
        this.reportsService.deleteReportTile(id).subscribe(result => {
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
        this.showEditor = false;
        this.isLoading = true;
        this.reportsService.saveTile(event.tile)
            .subscribe(result => {
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, result);
                this.getTiles();                
            });        
    }
    
}


