import { Component, Input, Output, OnInit } from '@angular/core';
import { Column } from 'primeng/primeng';
import { Lookup, LookupItem } from '../../../models/lookup.model';
import { LookupGrid, GridColumn, GridField, GridFilterColumn } from '../../../models/grid-definition.model';
import { BaseComponent } from '../base.component';


@Component({
    selector: 'd3s-dynamic-lookup-grid',
    template: `    
    <div *ngIf="!hideFilter && !hideHeader">
        <header>
            &nbsp;<d3s-tile-actions hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
        </header>
    </div>
    <input #gb type="text" [hidden]="!showSimpleFilter || hideFilter || hideHeader" pInputText size="100" placeholder="Search..." class="grid-simple-filter" />
    <div [class.hide-datatable-header]="hideHeader">
        <p-dataTable #dt [value]="data.Values" selectionMode="single" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [paginator]="!hideFooter" pageLinks="3" [globalFilter]="gb">
            <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
            <p-column *ngFor="let column of visibleColumns" [attr.header]="hideHeader ? null : column.text" [filter]="column.filterable && !hideFilter && !showSimpleFilter" [sortable]="column.sortable ? 'custom' : false" [field]="column.datafield" filterMatchMode="contains" (sortFunction)="customSort($event, column)">
                <ng-template *ngIf="!hideHeader" pTemplate="header">
                    <span *ngIf="column.description != null && column.description != ''" [pTooltip]="column.description" tooltipPosition="top">{{column.text}}</span>
                    <span *ngIf="column.description == null || column.description == ''">{{column.text}}</span>
                </ng-template>
                <ng-template let-item="rowData" pTemplate="body">
                    <d3s-dynamic-field-value [column]="column" [item]="item" [fields]="data.Fields" [isComplex]="isComplex"></d3s-dynamic-field-value>
                </ng-template>
            </p-column>
        </p-dataTable>
    </div>                                  
                `
})

export class DynamicLookupGridComponent extends BaseComponent implements OnInit {
    @Input() data: LookupGrid;
    @Input() hideFooter = false;
    @Input() hideHeader = false;
    @Input() hideFilter = true;

    isComplex = false;
    showSimpleFilter = true;

    visibleColumns;

    constructor() {
        super();
    }
    
    ngOnInit() {      
        this.isComplex = (this.data.Fields.find(f => f.name == 'Url') == null);

        this.data.Columns.filter(c => c.type == 'hidden').forEach(c => {
            let i = this.data.Columns.find(i => i.datafield == c.text);
            if (i) {
                i.type = 'preview';
            }
        });

        this.visibleColumns = this.data.Columns.filter(c => c.type != 'hidden'); 
    }

    customSort(e: any, col: any) {        
        let field = e.field;
        let direction = e.order;
        let type = col.type;

        this.data.Values = [...this.data.Values.sort((a, b) => {
            let fa = a[field];
            let fb = b[field];

            switch (type) {
                case 'number':
                    let na: number = +fa;
                    let nb: number = +fb;

                    if (na == null || isNaN(na))
                        na = -Infinity;
                    if (nb == null || isNaN(nb))
                        nb = -Infinity;

                    return ((na > nb) ? 1 : (na < nb) ? -1 : 0) * direction;
                case 'date':
                case 'datetime':
                    let da: number = Date.parse(fa);
                    let db: number = Date.parse(fb);

                    if (da == null || isNaN(da))
                        da = new Date(null).getTime();
                    if (db == null || isNaN(db))
                        db = new Date(null).getTime();

                    return ((da > db) ? 1 : (da < db) ? -1 : 0) * direction;
                default:                    
                    return ((fa > fb) ? 1 : (fa < fb) ? -1 : 0) * direction;
            }
        })];
    }
}



