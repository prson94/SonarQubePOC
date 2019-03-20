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
                <p-table #dt [value]="items" [scrollable]="true" scrollWidth="100%" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="globalFilterFields" [paginator]="true" [rows]="25" [rowsPerPageOptions]="defaultPagingOptions">
                    <ng-template pTemplate="colgroup">
                        <colgroup>
                            <col  style="width:125px">
                            <col *ngFor="let column of columns"  style="width:250px">
                            <col  style="width:100px">
                            <col  style="width:250px">
                        </colgroup>
                    </ng-template>
                    <ng-template pTemplate="header">
                        <tr>
                            <th [pSortableColumn]="'Status'" style="width: 125px">
                                Status
                                <d3s-sortIcon [field]="'Status'"></d3s-sortIcon>
                            </th>
                            <th *ngFor="let column of columns" [pSortableColumn]="column.datafield" style="width: 250px">
                                {{column.text}}
                                <d3s-sortIcon [field]="column.datafield"></d3s-sortIcon>

                            </th>
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
                            <th *ngFor="let column of columns"><d3s-column-filter [field]="column.datafield" ></d3s-column-filter></th>
                            <th><d3s-column-filter [field]="'RowIndex'" [datatype]="'text'"></d3s-column-filter></th>
                            <th><d3s-column-filter [field]="'StatusMessage'" [datatype]="'text'"></d3s-column-filter></th>
                        </tr>
                    </ng-template>
                    <ng-template pTemplate="body" let-item>
                        <tr [pSelectableRow]="item">
                            <td>{{item.Status}}</td>
                            <td *ngFor="let column of columns">{{item[column.datafield]}}</td>
                            <td>{{item.RowIndex}}</td>
                            <td>
                                <span *ngIf="item.StatusMessage" [innerHtml]="item.StatusMessage"></span>
                            </td>
                        </tr>
                    </ng-template>
                    <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                        <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                    </ng-template>
                </p-table>
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

    get globalFilterFields(): string[] {
        let f = this.columns.map(c => c.datafield);
        f.concat(['Status', 'RowIndex', 'StatusMessage']);
        return f;
    }
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
