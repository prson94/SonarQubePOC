import { Component, Input, OnInit, OnDestroy, NgModule, Output } from "@angular/core";
import { Table } from 'primeng/table';
import { CommonModule } from "@angular/common";
import { EventEmitter } from '@angular/core';


@Component({
    selector: 'd3s-sortIcon',
    template: `
        <a href="#" (click)="onClick($event)" [attr.aria-label]="ariaText" class="ui-sort-icon" style="color: #fff">
            <i class="fa fa-fw" [ngClass]="{'fa-sort-asc': sortOrder === 1, 'fa-sort-desc': sortOrder === -1, 'fa-sort': sortOrder === 0}"></i>
        </a>
    `
})
export class D3SSortIcon implements OnInit, OnDestroy {
    @Input() field: string;
    @Input() ariaLabel: string;
    @Input() ariaLabelDesc: string;
    @Input() ariaLabelAsc: string;

    subscription: any;
    sortOrder: number;

    @Output() changeCallback = new EventEmitter();

    constructor(public dt: Table) {
        this.subscription = this.dt.tableService.sortSource$.subscribe(sortMeta => {
            this.updateSortState();
        });
    }

    ngOnInit() {
        this.updateSortState();
    }

    onClick(event) {
        event.preventDefault();
    }

    updateSortState() {
        if (this.dt.sortMode === 'single') {
            this.sortOrder = this.dt.isSorted(this.field) ? this.dt.sortOrder : 0;
        }
        else if (this.dt.sortMode === 'multiple') {
            let sortMeta = this.dt.getSortMeta(this.field);
            this.sortOrder = sortMeta ? sortMeta.order : 0;
        }
        this.changeCallback.emit({ field: this.dt.sortField, order: this.dt.sortOrder })

    }

    get ariaText(): string {
        let text: string;

        switch (this.sortOrder) {
            case 1:
                text = this.ariaLabelAsc;
                break;

            case -1:
                text = this.ariaLabelDesc;
                break;

            default:
                text = this.ariaLabel;
                break;
        }

        return text;
    }

    ngOnDestroy() {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }
}
@NgModule({
    declarations: [
        D3SSortIcon,
    ],
    exports: [
        D3SSortIcon,
    ]
    , imports: [
        CommonModule,
    ]
})
export class D3SSortIconModule { }
