import { Component, NgZone, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { FormMode } from '../../../models/form.model';
import { MapType } from '../../../models/map.model';
import { MapsService } from '../../../services/maps.service';

@Component({
    selector: 'd3s-admin-maps-list',
    providers: [MapsService],
    template:
    `
 <div>
    <header>
        Map Types <d3s-tile-actions [hasAdd]="!isLoading" (addClick)="add()"></d3s-tile-actions>
    </header>
    <d3s-loading [isLoading]="isLoading"></d3s-loading>
    <div *ngIf="!isLoading">
        <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
        <p-dataTable #dt [globalFilter]="gb" [value]="mapTypes" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" (onRowDblclick)="edit($event.data.ID)" [selection]="selection" (selectionChange)="selection = $event; onSelectionChange.emit(selection)" >                                                        
            <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>                                                        
            <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                                                            
            <p-column field="Description" header="Description" [sortable]="true" [filter]="!showSimpleFilter">
                <ng-template let-item="rowData" pTemplate type="body">
                    <div [innerHtml]="item?.Description"></div>
                </ng-template>                                                        
            </p-column>   
            <p-column [style]="{width:'40px'}">
                <ng-template let-item="rowData" pTemplate type="body">
                    <div class="RowTools">
                        <a style="cursor:pointer;" (click)="selection = item; edit(item.ID);"><i class="fa fa-pencil"></i></a>                                        
                    </div>
                </ng-template>
            </p-column>                            
            <p-column  [style]="{width:'40px'}">
                <ng-template let-item="rowData" pTemplate type="body">
                    <div class="RowTools" *ngIf="item.ID > 1">                                
                        <a style="cursor:pointer;" (click)="selection = item; delete(item.ID);"><i class="fa fa-trash-o"></i></a>                                    
                    </div>
                </ng-template>
            </p-column>  
        </p-dataTable>      
    </div>
</div>
`
})

export class AdminMapsListComponent extends BaseComponent implements OnInit {
    @Output() onSelectionChange = new EventEmitter();
    @Output() onEditClick = new EventEmitter();
    @Output() onDeleteClick = new EventEmitter();
    @Output() onAddClick = new EventEmitter();

    selection: MapType;
    mapTypes: MapType[] = [];


    constructor(
        private mapsService: MapsService,
        protected messagesService: MessagesService) {
        super();

    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.mapsService.getMapTypes()
            .then(r => {
                this.mapTypes = r;
                if (this.mapTypes != null && this.mapTypes.length > 0) {
                    this.selection = this.mapTypes[0];
                    this.onSelectionChange.emit(this.selection);
                }
                this.isLoading = false;
            })
    }

    save() {

    }

    add() {
        this.onAddClick.emit();
    }

    edit(id: number) {
        this.selection = this.mapTypes.find(m => m.ID == id);
        this.onEditClick.emit(this.selection);
    }

    delete(id: number) {
        this.selection = this.mapTypes.find(m => m.ID == id);
        this.onDeleteClick.emit(this.selection);
    }

}


