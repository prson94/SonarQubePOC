
import { Component, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef, Input, AfterViewInit, OnChanges, SimpleChange, Output, EventEmitter, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import * as _ from 'lodash';
import { Paginator } from 'primeng/paginator'

@Component({
    selector: 'd3s-paginator',
    template: `<div class="paging-bar">
                    <span class="items">Showing {{(page * itemsPerPage) + 1}} to {{GetToDisplayValue()}} of {{totalRecords}} items</span>
                    <span class="grow"></span>
                    <div *ngIf="totalRecords > itemsPerPage" class="pages">
                        <span (click)="changePageToFirst($event)">First</span>
                        <span (click)="changePageToPrev($event)">Previous</span>
                        <span [ngClass]="{selected: page == (cpage - 1)}" *ngFor="let cpage of pageOptions" (click)="onPageLinkClick(cpage - 1)">{{cpage}}</span>
                        <span (click)="changePageToNext($event)">Next</span>
                        <span (click)="changePageToLast($event)">Last</span>
                        <span><i class="fa fa-cog"></i></span>
                    </div>
                </div>
			  `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class PaginatorComponent implements OnChanges, OnInit {
    @Input() rows: number;
    @Input() page: number;
    @Input() totalRecords: number;
    @Input() percentage: number;
    @Output() onPageChange = new EventEmitter();
    private itemsPerPage: number = 10;
    private pageOptions = [1];
    private visableNumbers: number = 3;
    constructor(
        ref: ChangeDetectorRef,
        private router: Router
    ) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        
    }

    CheckCurrent(page: number) {
        console.log(page + "  " +this.page)
    }

    ngOnInit(): void {
        this.itemsPerPage = 10;
        this.page = 0;
        this.CheckVisableNumbers();
    }

    isFirstPage(): boolean {
        if (0 == this.page) {
            return true;
        }
        return false;
    }

    isLastPage(): boolean {
        if (this.getPageCount() <= this.page) {
            return true;
        }
        return false;
    }

    changePageToFirst(event: any): void {
        this.page = 0;

        this.paginate(this.itemsPerPage, this.page, (this.page * this.itemsPerPage));
    }

    changePageToPrev(event: any): void {
        if (this.isFirstPage())
            return;
        else this.page--;

        this.paginate(this.itemsPerPage, this.page, (this.page * this.itemsPerPage));
    }

    changePageToNext(event: any): void {
        if (this.isLastPage())
            return;
        else
            this.page++;
        console.log({ size: this.itemsPerPage, page: this.page });

        this.paginate(this.itemsPerPage, this.page, (this.page * this.itemsPerPage));
    }
    changePageToLast(event: any): void {
        this.page = this.getPageCount();

        this.paginate(2, this.page, (this.page * this.itemsPerPage));
    }
    onPageLinkClick(page: number): void {
        if (page !== undefined && (this.page !== page)) {
            this.page = page;
            this.paginate(this.itemsPerPage, this.page, (this.page * this.itemsPerPage));
        }
    }

    getPageCount(): number {
        if (this.totalRecords > 0) {
            {
                return Math.round(this.totalRecords / this.itemsPerPage);
            }
        }
        return 1;
    }

    paginate(size, page, firstItemIndex) {
        this.CheckVisableNumbers(); 
        this.onPageChange.emit({ size: size, page: page, first: firstItemIndex });
    }


    private CheckVisableNumbers() {
        if (this.getPageCount() > 1) {
            if (this.isFirstPage())
                this.pageOptions = [1, 2, 3];
            else if (this.isLastPage() && this.getPageCount() > 2)
                this.pageOptions = [this.getPageCount() - 2, this.getPageCount() - 1, this.getPageCount()];
            else {
                for (var i = 0; i < this.visableNumbers; i++) {
                    if (this.getPageCount() <= i)
                        this.pageOptions[i] = i + 1;
                }
            }
        }
    }

    GetToDisplayValue() {
        if (this.totalRecords < this.itemsPerPage)
            return this.totalRecords;
        else {
            return (this.page * this.itemsPerPage) + this.itemsPerPage;
        }
    }
};
