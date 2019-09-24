
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
                        <span [ngClass]="{'disabled' : isFirstPage()}" (click)="changePageToFirst($event)">First</span>
                        <span [ngClass]="{'disabled' : isFirstPage()}" (click)="changePageToPrev($event)">Previous</span>
                        <span [ngClass]="{selected: page == (cpage - 1)}" *ngFor="let cpage of pageOptions" (click)="onPageLinkClick(cpage - 1)">{{cpage}}</span>
                        <span [ngClass]="{'disabled' : isLastPage()}" (click)="changePageToNext($event)">Next</span>
                        <span [ngClass]="{'disabled' : isLastPage()}" (click)="changePageToLast($event)">Last</span>
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

    ngOnInit(): void {
        this.itemsPerPage = 10;
        this.page = 0;
        this.CheckVisableNumbers();
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
        let lastNumItems = this.totalRecords % this.itemsPerPage;
        console.log(lastNumItems);
        this.paginate(lastNumItems, this.page, (this.page * this.itemsPerPage));
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
        let currentPage = this.page + 1, range = 5,  totalPages = this.getPageCount(), start = 1;  
        let paging = [];      
        if (currentPage < (range / 2) + 1) {
            start = 1;

        } else if (currentPage >= (totalPages - (range / 2))) {
            start = Math.floor(totalPages - range + 1);

        } else {
            start = (currentPage - Math.floor(range / 2));
        }

        for (let i = start; i <= ((start + range) - 1); i++) {
            paging.push(i); 
        }

        if (this.page < 2 && this.getPageCount() <= 3) {
            this.pageOptions = paging.splice(0, (this.getPageCount()));
        }
        else if (this.page < 2 && this.getPageCount() > 3) {
            this.pageOptions = paging.splice(0, 3);
        }
        else {
            this.pageOptions = paging;
        }   
    }

    GetToDisplayValue() {
        if (this.totalRecords <= this.itemsPerPage)
            return this.totalRecords;
        else if ((this.page * this.itemsPerPage) + this.itemsPerPage >= this.totalRecords)
            return this.totalRecords;
        else 
            return (this.page * this.itemsPerPage) + this.itemsPerPage;
    }
};
