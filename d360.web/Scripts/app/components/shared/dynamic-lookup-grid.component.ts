///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, Output, OnChanges, SimpleChange, EventEmitter} from '@angular/core';
import { Column } from 'primeng/primeng';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { LookupGrid, GridColumn, GridField } from '../../models/grid-definition.model';

@Component({
    selector: 'd3s-dynamic-lookup-grid',
    template: `                
               <p-dataTable [value]="data.Values" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3">                                                                       
                    <p-column *ngFor="let column of data.Columns" [field]="column.datafield" [header]="column.text" [filter]="column.filterable" [sortable]="column.sortable"></p-column>                          
                </p-dataTable>                                    
                `
})

export class DynamicLookupGridComponent {
    @Input() data: LookupGrid;

    constructor() {
    }

    load() {
    }   
}



