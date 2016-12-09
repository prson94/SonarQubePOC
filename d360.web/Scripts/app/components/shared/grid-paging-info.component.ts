import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { BaseComponent } from './base.component';

@Component({
    selector: 'd3s-grid-paging-info',
    template: `   
            Rows {{startValue}} - {{endValue}} of {{totalRecords?.toLocaleString()}} Items
        `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GridPagingInfoComponent extends BaseComponent {
    @Input() first: number;
    @Input() rows: number;
    @Input() totalRecords: number;

    get startValue() {
        if (this.first != undefined) {
            return (this.first + 1).toLocaleString();
        }
        return '';
    }

    get endValue() {       
        if (this.totalRecords === null) return "";

        if ((this.first + Number(this.rows)) > this.totalRecords) {
            return this.totalRecords.toLocaleString();
        }
        return (this.first + Number(this.rows)).toLocaleString();                
    }
}

@NgModule({
    imports: [CommonModule,        
    ],
    declarations: [
        GridPagingInfoComponent
    ],
    exports: [
        GridPagingInfoComponent
    ]
})
export class SharedGridPagingInfoModule { }