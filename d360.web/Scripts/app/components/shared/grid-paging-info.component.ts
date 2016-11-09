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
        return ((this.first + this.rows > this.totalRecords) ? this.totalRecords : (this.first + this.rows)).toLocaleString();
    }
}