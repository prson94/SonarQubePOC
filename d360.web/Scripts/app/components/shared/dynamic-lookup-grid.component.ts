
import { Component, Input, Output, OnInit } from '@angular/core';
import { Column } from 'primeng/primeng';
import { Lookup, LookupItem } from '../../models/lookup.model';
import { LookupGrid, GridColumn, GridField, GridFilterColumn } from '../../models/grid-definition.model';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-dynamic-lookup-grid',
    template: `    

               <p-dataTable *ngIf="hideHeader" [value]="data.Values" selectionMode="single" [rows]="10" [paginator]="!hideFooter" [pageLinks]="3">  
                    <p-column *ngFor="let column of visibleColumns" [sortable]="column.sortable" [field]="column.datafield">
                        <template let-item="rowData" pTemplate type="body">
                                    <div [ngSwitch]="column.type">
                                        <span *ngSwitchCase="'date'">{{item[column.datafield] | date:'short'}}</span>
                                        <span *ngSwitchCase="'bool'">
                                            <i *ngIf="item[column.datafield] === 'true'" class="fa fa-check enabled" title="True"></i>
                                            <i *ngIf="item[column.datafield] === 'false'" class="fa fa-times disabled" title="False"></i>
                                        </span>
                                        <span *ngSwitchCase="'tooltip'">
                                            <d3s-tooltip [objectType]="item.Object" [objectId]="item.ObjectID" tooltipType="preview">
                                                <a (click)="navigate(item.Url)">{{item[column.datafield]}}</a>
                                            </d3s-tooltip>
                                        </span>
                                        <span *ngSwitchCase="'preview'">
                                            <d3s-tooltip *ngIf="item[column.datafield + '_Type'] != 'none'" [objectType]="item[column.datafield + '_Object']" [objectId]="item[column.datafield + '_ObjectID']" [tooltipType]="item[column.datafield + '_Type']">
                                                <a (click)="navigate(item[column.datafield + '_Url'])">{{item[column.datafield + '_Name']}}</a>
                                            </d3s-tooltip>
                                            <div *ngIf="item[column.datafield + '_Type'] == 'none'" [innerHtml]="item[column.datafield]"></div>
                                        </span>
                                        <span *ngSwitchDefault [innerHtml]="item[column.datafield]"></span>
                                    </div>
                         </template>
                    </p-column>                                                                                         
                </p-dataTable>               
               <p-dataTable *ngIf="!hideHeader" [value]="data.Values" selectionMode="single" [rows]="10" [paginator]="!hideFooter" [pageLinks]="3">  
                    <p-column *ngFor="let column of visibleColumns" [header]="column.text" [filter]="column.filterable && !hideFilter" [sortable]="column.sortable" [field]="column.datafield">
                        <template let-item="rowData" pTemplate type="body">
                                    <div [ngSwitch]="column.type">
                                        <span *ngSwitchCase="'date'">{{item[column.datafield] | date:'short'}}</span>
                                        <span *ngSwitchCase="'bool'">
                                            <i *ngIf="item[column.datafield] === 'true'" class="fa fa-check enabled" title="True"></i>
                                            <i *ngIf="item[column.datafield] === 'false'" class="fa fa-times disabled" title="False"></i>
                                        </span>
                                        <span *ngSwitchCase="'tooltip'">
                                            <d3s-tooltip [objectType]="item.Object" [objectId]="item.ObjectID" tooltipType="preview">
                                                <a (click)="navigate(item.Url)">{{item[column.datafield]}}</a>
                                            </d3s-tooltip>
                                        </span>
                                        <span *ngSwitchCase="'preview'">
                                            <d3s-tooltip *ngIf="item[column.datafield + '_Type'] != 'none'" [objectType]="item[column.datafield + '_Object']" [objectId]="item[column.datafield + '_ObjectID']" [tooltipType]="item[column.datafield + '_Type']">
                                                <a (click)="navigate(item[column.datafield + '_Url'])">{{item[column.datafield + '_Name']}}</a>
                                            </d3s-tooltip>
                                            <div *ngIf="item[column.datafield + '_Type'] == 'none'" [innerHtml]="item[column.datafield]"></div>
                                        </span>
                                        <span *ngSwitchDefault [innerHtml]="item[column.datafield]"></span>
                                    </div>
                         </template>
                    </p-column>                                                                                         
                </p-dataTable>                                    
                `
})

export class DynamicLookupGridComponent implements OnInit {
    @Input() data: LookupGrid;
    @Input() hideFooter = false;
    @Input() hideHeader = false;
    @Input() hideFilter = true;

    isComplex = false;

    visibleColumns;

    constructor(private router: Router) {
    }
    
    ngOnInit() {
        
        this.isComplex = (this.data.Fields.find(f => f.name == 'Url') == null);

        //do this on init to avoid binding to function call
        this.data.Columns.forEach(c => {
            c.type = this.columnDataType(c);
            //console.log(c.type);
        });

        this.data.Columns.filter(c => c.type == 'hidden').forEach(c => {
            let i = this.data.Columns.find(i => i.datafield == c.text);
            if (i) {
                i.type = 'preview';
            }
        });

        this.visibleColumns = this.data.Columns.filter(c => c.type != 'hidden');
        //console.log(this.visibleColumns);
    }

    private columnDataType(column: GridFilterColumn): string {
        var fields = this.data.Fields.filter(x => x.name == column.datafield);

        //TODO: need to modify values from server to contain object
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
}



