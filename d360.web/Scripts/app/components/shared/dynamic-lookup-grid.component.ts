
import { Component, Input, Output, OnInit } from '@angular/core';
import { Column } from 'primeng/primeng';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { LookupGrid, GridColumn, GridField } from '../../models/grid-definition.model';

@Component({
    selector: 'd3s-dynamic-lookup-grid',
    template: `    

               <p-dataTable *ngIf="hideHeader" [value]="data.Values" selectionMode="single" [rows]="10" [paginator]="!hideFooter" [pageLinks]="3">  
                    <p-column *ngFor="let column of data.Columns" [sortable]="column.sortable" [field]="column.datafield">
                        <template let-item="rowData" pTemplate type="body">
                                    <span [ngSwitch]="columnDataType(column)">
                                        <span *ngSwitchCase="'date'">{{item[column.datafield] | date:'short'}}</span>
                                        <span *ngSwitchCase="'bool'">
                                            <i *ngIf="item[column.datafield] === 'true'" class="fa fa-check enabled" title="True"></i>
                                            <i *ngIf="item[column.datafield] === 'false'" class="fa fa-times disabled" title="False"></i>
                                        </span>
                                        <span *ngSwitchDefault [innerHtml]="item[column.datafield]"></span>
                                    </span>
                         </template>
                    </p-column>                                                                                         
                </p-dataTable>             
               <p-dataTable *ngIf="!hideHeader" [value]="data.Values" selectionMode="single" [rows]="10" [paginator]="!hideFooter" [pageLinks]="3">  
                    <p-column *ngFor="let column of data.Columns" [header]="column.text" [filter]="column.filterable && !hideFilter" [sortable]="column.sortable" [field]="column.datafield">
                        <template let-item="rowData" pTemplate type="body">
                                    <span [ngSwitch]="columnDataType(column)">
                                        <span *ngSwitchCase="'date'">{{item[column.datafield] | date:'short'}}</span>
                                        <span *ngSwitchCase="'bool'">
                                            <i *ngIf="item[column.datafield] === 'true'" class="fa fa-check enabled" title="True"></i>
                                            <i *ngIf="item[column.datafield] === 'false'" class="fa fa-times disabled" title="False"></i>
                                        </span>
                                        <span *ngSwitchDefault [innerHtml]="item[column.datafield]"></span>
                                    </span>
                         </template>
                    </p-column>                                                                                         
                </p-dataTable>                                    
                `
})

export class DynamicLookupGridComponent {
    @Input() data: LookupGrid;
    @Input() hideFooter = false;
    @Input() hideHeader = false;
    @Input() hideFilter = true;

    constructor() {
    }
    

    private columnDataType(column: GridColumn): string {
        var fields = this.data.Fields.filter(x => x.name == column.datafield);

        if (fields.length > 0)
            return fields[0].type;
        return 'string';
    }
}



