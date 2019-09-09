
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
                        <span class="popup-container">
                            <i class="fa fa-cog"></i>
                            <div class="popup-menu popup">
                            <ul>
                                <li class="label"><span class="col1"></span><span>Items Per Page</span></li>
                                <li *ngFor="let item of itemsPerPageOptions"(click)="changePageNumber(item)">
                                    <span class="col1"> 
                                        <i *ngIf="item == itemsPerPage" class="fa fa-check"></i>
                                    </span>
                                    <span>{{item}}</span>
                                    <span class="col3"></span>
                                </li>
                            </ul>
                        </div>
                        </span>
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
    private itemsPerPageOptions = [10, 25, 50, 100];
    private itemsPerPage: number = 10;
    private pageOptions = [1];
    private visableNumbers: number = 5;
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
    changePageNumber(number: number) {
        this.itemsPerPage = number;
        this.paginate(this.itemsPerPage, this.page, (this.page * this.itemsPerPage));
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
        this.pageOptions = [];
        if (this.getPageCount() > 1) {
            if (this.isFirstPage())
                this.pageOptions = [1, 2, 3];
            else if (this.isLastPage() && this.getPageCount() > 2)
                this.pageOptions = [this.getPageCount() - 2, this.getPageCount() - 1, this.getPageCount()];
            else if (this.getPageCount() > this.visableNumbers) {
                this.pageOptions.push((this.page + 1) - 1);
                this.pageOptions.push((this.page + 1));
                this.pageOptions.push((this.page + 1) + 1);
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
