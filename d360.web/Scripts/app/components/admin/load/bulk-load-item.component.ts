import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';
import { LoadDetail } from '../../../models/load.model';
import { LoadService } from '../../../services/load.service';
import { GridColumn } from '../../../models/grid-definition.model';
import { BaseComponent } from '../../shared/base.component'

@Component({
    selector: 'd3s-bulk-load-item',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div *ngIf="!isLoading">
                <header>
                    {{title}}
                    <d3s-tile-actions [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" [hasRefresh]="true" (refreshClick)="refresh()" [hasExportErrors]="true" (exportErrorsClick)="exportErrors()" [hasExportOriginal]="true" (exportOriginalClick)="exportOriginal()"></d3s-tile-actions>                                    
                </header>
                <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                <p-table #dt [value]="items" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Status','RowIndex','StatusMessage']" [paginator]="true" [rows]="25" [rowsPerPageOptions]="defaultPagingOptions">
                    <ng-template pTemplate="header">
                        <tr>
                            <th [pSortableColumn]="'Status'" style="width: 125px">
                                Status
                                <d3s-sortIcon [field]="'Status'"></d3s-sortIcon>
                            </th>
                            <th [pSortableColumn]="''" style="width: 250px"></th>
                            <th [pSortableColumn]="'RowIndex'" style="width: 100px">
                                Row
                                <d3s-sortIcon [field]="'RowIndex'"></d3s-sortIcon>
                            </th>
                            <th [pSortableColumn]="'StatusMessage'" style="width: 250px">
                                Message
                                <d3s-sortIcon [field]="'StatusMessage'"></d3s-sortIcon>
                            </th>
                        </tr>
                        <tr [hidden]="showSimpleFilter">
                            <th><d3s-column-filter [field]="'Status'" [datatype]="'text'"></d3s-column-filter></th>
                            <th></th>
                            <th><d3s-column-filter [field]="'RowIndex'" [datatype]="'text'"></d3s-column-filter></th>
                            <th><d3s-column-filter [field]="'StatusMessage'" [datatype]="'text'"></d3s-column-filter></th>
                        </tr>
                    </ng-template>
                    <ng-template pTemplate="body" let-item>
                        <tr [pSelectableRow]="item">
                            <td>{{item.Status}}</td>
                            <td></td>
                            <td>{{item.RowIndex}}</td>
                            <td>
                                <span [innerHtml]="item.StatusMessage"></span>
                            </td>
                        </tr>
                    </ng-template>
                    <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                        <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                    </ng-template>
                </p-table>


<!--
<input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                <p-dataTable #dt [globalFilter]="gb" [value]="items" selectionMode="single" [rows]="25" paginator="true" scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="defaultPagingOptions">
                    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                    <p-column field="Status" header="Status" sortable="true" [style]="{'width':'125px'}" [filter]="!showSimpleFilter"></p-column>
                    <p-column *ngFor="let column of columns" sortable="true" [field]="column.datafield" [header]="column.text" [style]="{'width':'250px'}" [filter]="!showSimpleFilter"></p-column>
                    <p-column field="RowIndex" header="Row" sortable="true" [style]="{'width':'100px'}" [filter]="!showSimpleFilter"></p-column>        
                    <p-column field="StatusMessage" header="Message" sortable="true" [style]="{'width':'250px'}" [filter]="!showSimpleFilter">
                        <ng-template let-item="rowData" pTemplate type="body">
                            <span [innerHtml]="item.StatusMessage"></span>
                        </ng-template>
                    </p-column>
                </p-dataTable> -->
            </div>
    `,
    providers: [LoadService]
})

export class BulkLoadItemComponent extends BaseComponent implements OnChanges {
    @Input() id: number;
    @Input() title: string = "Load Details";

    @Output() refreshClick = new EventEmitter();
    
    columns: GridColumn[];
    items: any[];


    constructor(private loadService: LoadService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
               return this.load();                
            }
        }

        this.load();
    }

    exportErrors(): void {
        if (this.id == null)
            return;

        this.loadService.getLoadErrorsXls(this.id);
    }

    exportOriginal(): void {
        if (this.id == null)
            return;

        this.loadService.getLoadOriginalXls(this.id);
    }

    load(): void {
        if (this.id == null)
            return;

        this.isLoading = true;

        this.loadService.getLoadColumns(this.id)
            .then(data => {
                this.columns = data;
            })
            .then(() => this.loadService.getLoadItems(this.id))
            .then(data => {
                this.items = data;
                this.isLoading = false;
            });
    }

    refresh(): void {
        this.load();
        this.refreshClick.emit();
    }
}
