
import { Component, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef, Input, AfterViewInit, OnChanges, SimpleChange, Output, EventEmitter, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import * as _ from 'lodash';

export enum LABEL_STYLE {
    ANGLE = "angle",
    WORD = "word"
}

@Component({
    selector: 'd3s-paginator',
    templateUrl: "./paginator-bar-component.html",
    styleUrls: ["paginator-bar-component.less"],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class PaginatorComponent implements OnChanges, OnInit {
    @Input() rows: number;
    @Input() page: number;
    @Input() totalRecords: number;
    @Input() percentage: number;
    @Input() labelStyle: LABEL_STYLE = LABEL_STYLE.ANGLE;
    @Input() hideLastButton: boolean = false;
    @Input() hideSettings: boolean = true;
    @Output() onPageChange = new EventEmitter();
    itemsPerPageOptions = [10, 25, 50, 100];
    itemsPerPage: number = 25;
    pageOptions = [1];
    visableNumbers: number = 5;
    constructor(
        ref: ChangeDetectorRef,
        private router: Router
    ) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if ((changes['page'] != undefined && !changes['page'].firstChange) || (changes['totalRecords'] != undefined && !changes['totalRecords'].firstChange))
            this.CheckVisableNumbers();
    }

    ngOnInit(): void {
        this.itemsPerPage = 25;
        this.page = 0;
        this.CheckVisableNumbers();
    }

    public isAngle(): boolean {
        return this.labelStyle === LABEL_STYLE.ANGLE;
    }

    get labelFirst() {
        return this.isAngle() ? "&laquo;" : "First";
    }
    get labelLast() {
        return this.isAngle() ? "&raquo;" : "Last";
    }
    get labelPrevious() {
        return this.isAngle() ? "&lsaquo;" : "Previous";
    }
    get labelNext() {
        return this.isAngle() ? "&rsaquo;" : "Next";
    }

    changePageNumber(newItemsPerPage: number) {
        this.page = Math.floor((this.page * this.itemsPerPage) / newItemsPerPage);
        this.itemsPerPage = newItemsPerPage;
        this.paginate(this.itemsPerPage, this.page, (this.page * this.itemsPerPage));
    }

    isFirstPage(): boolean {
        if (0 == this.page) {
            return true;
        }
        return false;
    }

    isLastPage(): boolean {
        if (this.getPageCount() <= (this.page + 1)) {
            return true;
        }
        return false;
    }

    changePageToFirst(event: any): void {
        if (this.isFirstPage())
            return;
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
        if (this.isLastPage())
            return;
        else
            this.page = this.getPageCount() - 1;
        this.paginate(this.itemsPerPage, this.page, (this.page * this.itemsPerPage));
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
                return Math.ceil(this.totalRecords / this.itemsPerPage);
            }
        }
        return 1;
    }

    paginate(size, page, firstItemIndex) {
        this.CheckVisableNumbers();
        this.onPageChange.emit({ size: size, page: page, first: firstItemIndex });
    }

    CheckVisableNumbers() {
        this.pageOptions = [];
        let currentPage = this.page + 1, totalPages = this.getPageCount();
        let step = 2; // Current page +- step
        let paging = [];      

        //end pagination at CurrentPage+2 or total pages, whichever is smallest, but up to step*2 + 1 options
        let end = Math.min(Math.max(currentPage + step, 1 + 2 * step), totalPages);
        //start pagination at CurrentPage-2 or end-4, whichever is smallest, but no lower than 1
        let start = Math.max(Math.min(currentPage - step, end - 2 * step), 1);

        for (let i = start; i <= end; i++) {
            paging.push(i); 
        }

        this.pageOptions = paging;
    }

    GetToDisplayValue() {
        if (this.totalRecords <= this.itemsPerPage)
            return this.totalRecords;
        else if ((this.page * this.itemsPerPage) + this.itemsPerPage >= this.totalRecords)
            return this.totalRecords;
        else 
            return (this.page * this.itemsPerPage) + this.itemsPerPage;
    }
}
