import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { BaseComponent } from './base.component';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-grid-paging-info',
    template: `   
        <ng-container *ngIf="totalRecords" i18n>
            Rows {{startValue}} - {{endValue}} of {{totalRecords?.toLocaleString()}} Items
        </ng-container>
        `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GridPagingInfoComponent extends BaseComponent {
    @Input() first: number;
    @Input() rows: number;
    @Input() totalRecords: number;

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

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