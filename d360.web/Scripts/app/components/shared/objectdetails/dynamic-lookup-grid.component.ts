import { Component, Input, Output, OnInit } from '@angular/core';
import { Column } from 'primeng/primeng';
import { Lookup, LookupItem } from '../../../models/lookup.model';
import { LookupGrid, GridColumn, GridField, GridFilterColumn } from '../../../models/grid-definition.model';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { BaseComponent } from '../base.component';


@Component({
    selector: 'd3s-dynamic-lookup-grid',
    template: `    
               <p-dataTable #dt *ngIf="hideHeader" [value]="data.Values" selectionMode="single" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [paginator]="!hideFooter" pageLinks="3">  
                    <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                    <p-column *ngFor="let column of visibleColumns" [sortable]="column.sortable ? 'custom' : false" [field]="column.datafield" (sortFunction)="customSort($event, column)">
                        <template pTemplate type="header">
                            <span *ngIf="column.description != null && column.description != ''" [pTooltip]="column.description" tooltipPosition="top">{{column.text}}</span>
                            <span *ngIf="column.description == null || column.description == ''">{{column.text}}</span>
                        </template>
                        <template let-item="rowData" pTemplate type="body">
                                    <div [ngSwitch]="column.type">
                                        <span *ngSwitchCase="'date'">{{item[column.datafield] | date:'shortDate'}}</span>
                                        <span *ngSwitchCase="'bool'">
                                            <i *ngIf="item[column.datafield] === 'true'" class="fa fa-check enabled" title="True"></i>
                                            <i *ngIf="item[column.datafield] === 'false'" class="fa fa-times disabled" title="False"></i>
                                        </span>
                                        <span *ngSwitchCase="'number'">{{item[column.datafield]}}</span>
                                        <span *ngSwitchCase="'lookup'">
                                            <d3s-tooltip [objectType]="item[column.objectfield]" [objectId]="item[column.objectidfield]" [tooltipType]="item[column.contextfield]">
                                                <a (click)="navigate(item[column.urlfield])" [innerHtml]="item[column.datafield]"></a>
                                            </d3s-tooltip>
                                        </span>
                                        <span *ngSwitchDefault [innerHtml]="item[column.datafield]"></span>
                                    </div>
                         </template>
                    </p-column>                                                                                         
                </p-dataTable>   
                <div *ngIf="!hideFilter && !hideHeader">                
                    <header>
                        &nbsp;<d3s-tile-actions hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
                    </header>   
                </div>      
                <input #gb type="text" [hidden]="!showSimpleFilter || hideFilter || hideHeader" pInputText size="100" placeholder="Search..." class="grid-simple-filter" />
               <p-dataTable #dt2 *ngIf="!hideHeader" [value]="data.Values" selectionMode="single" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [paginator]="!hideFooter" pageLinks="3" [globalFilter]="gb">  
                    <footer *ngIf="dt2.totalRecords"><d3s-grid-paging-info [totalRecords]="dt2.totalRecords" [first]="dt2.first" [rows]="dt2.rows"></d3s-grid-paging-info></footer>
                    <p-column *ngFor="let column of visibleColumns" [header]="column.text" [filter]="column.filterable && !hideFilter && !showSimpleFilter" [sortable]="column.sortable ? 'custom' : false" [field]="column.datafield" filterMatchMode="contains" (sortFunction)="customSort($event, column)">
                        <template pTemplate type="header">
                            <span *ngIf="column.description != null && column.description != ''" [pTooltip]="column.description" tooltipPosition="top">{{column.text}}</span>
                            <span *ngIf="column.description == null || column.description == ''">{{column.text}}</span>
                        </template>
                        <template let-item="rowData" pTemplate type="body">
                                    <div [ngSwitch]="column.type">
                                        <span *ngSwitchCase="'date'">{{item[column.datafield] | date:'shortDate'}}</span>
                                        <span *ngSwitchCase="'bool'">
                                            <i *ngIf="item[column.datafield] === 'true'" class="fa fa-check enabled" title="True"></i>
                                            <i *ngIf="item[column.datafield] === 'false'" class="fa fa-times disabled" title="False"></i>
                                        </span>
                                        <span *ngSwitchCase="'number'">{{item[column.datafield]}}</span>
                                        <span *ngSwitchCase="'lookup'">
                                            <d3s-tooltip [objectType]="item[column.objectfield]" [objectId]="item[column.objectidfield]" [tooltipType]="item[column.contextfield]">
                                                <a (click)="navigate(item[column.urlfield])" [innerHtml]="item[column.datafield]"></a>
                                            </d3s-tooltip>
                                        </span>
                                        <span *ngSwitchDefault [innerHtml]="item[column.datafield]"></span>
                                    </div>
                         </template>
                    </p-column>                                                                                         
                </p-dataTable>                                    
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

    constructor(private router: Router) {
        super();
    }
    
    ngOnInit() {
        
        this.isComplex = (this.data.Fields.find(f => f.name == 'Url') == null);

        //do this on init to avoid binding to function call
        this.data.Columns.forEach(c => {
            c.type = this.columnDataType(c);  
            if (c.type == 'number') {
                this.data.Values.forEach(v => {
                    v[c.datafield] = this.formatAsNumber(v[c.datafield]);
                });
            }
            if (c.type == 'string' || c.type == 'preview' || c.type == 'lookup') {
                this.data.Values.forEach(v => {
                    if (v[c.datafield] === null) {
                        v[c.datafield] = ''; //prevent IE from displaying 'null'
                    }
                });
            }
        });

        this.data.Columns.filter(c => c.type == 'hidden').forEach(c => {
            let i = this.data.Columns.find(i => i.datafield == c.text);
            if (i) {
                i.type = 'preview';
            }
        });

        this.visibleColumns = this.data.Columns.filter(c => c.type != 'hidden'); 

        console.log(this.data.Columns);
    }

    private formatAsNumber(val): string {
        return val != '' && val != null ? Number(val).toLocaleString() : "";
    }

    private columnDataType(column: GridFilterColumn): string {
        var fields = this.data.Fields.filter(x => x.name == column.datafield);

        if (column.type == 'preview')
            return 'preview';
        if ((column.datafield == 'Name' || column.datafield == 'TextPath') && !this.isComplex)
            return 'tooltip';
        if (fields.length > 0)
            return fields[0].type;
        return 'string';
    }

    navigate(url: string) {
        //TODO: should attempt to generate dynamically by object/objectid eventually
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url)); 
    }

    customSort(e: any, col: any) {
        let field = e.field;
        let direction = e.order;
        let type = col.type;

        this.data.Values = this.data.Values.sort((a, b) => {
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
        });
    }
}



