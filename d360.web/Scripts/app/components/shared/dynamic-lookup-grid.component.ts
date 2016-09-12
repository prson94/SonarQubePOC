///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, Output, OnInit } from '@angular/core';
import { Column } from 'primeng/primeng';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { LookupGrid, GridColumn, GridField } from '../../models/grid-definition.model';

@Component({
    selector: 'd3s-dynamic-lookup-grid',
    template: `                
               <p-dataTable [value]="data.Values" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3">  
                    <p-column *ngFor="let column of data.Columns" [header]="column.text" [filter]="column.filterable" [sortable]="column.sortable">
                        <template let-row="rowData" pTemplate type="body">
                                <div [innerHtml]="row[column.datafield]"></div>
                        </template>
                    </p-column>                                                                                         
                </p-dataTable>                                    
                `
})

export class DynamicLookupGridComponent implements OnInit {
    @Input() data: LookupGrid;

    constructor() {
    }

    ngOnInit() {
        //console.log(this.data);
    } 
}



