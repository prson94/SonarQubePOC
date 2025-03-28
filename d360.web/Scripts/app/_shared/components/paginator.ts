import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnInit,
    Output,
    SimpleChange
} from '@angular/core';
import { Router } from '@angular/router';

export enum LABEL_STYLE {
    ANGLE = "angle",
    WORD = "word"
}

@Component({
    selector: 'paginator',
	templateUrl: "./paginator.html",
	styleUrls: ["paginator.less"],
	standalone: true,
	imports: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Paginator implements OnChanges, OnInit {
    @Input() rows: number;
    @Input() page: number;
    @Input() totalRecords: number;
    @Input() percentage: number;
    @Input() labelStyle: LABEL_STYLE = LABEL_STYLE.ANGLE;
    @Input() hideLastButton: boolean = false;
    @Input() hideSettings: boolean = false;
    @Output() onPageChange = new EventEmitter();
    itemsPerPageOptions = [10, 25, 50, 100];
    pageOptions = [1];

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		if ((changes['page'] != null && !changes['page'].firstChange) || (changes['totalRecords'] != null && !changes['totalRecords'].firstChange)) {
			this.checkVisibleNumbers();
		}
    }

    ngOnInit(): void {
        this.checkVisibleNumbers();
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

	get previousCss() {
		let css = '';
		css += this.isAngle() ? "angle" : "";
		css += this.isFirstPage() ? " disabled" : "";
		return css;
	}

	get labelNext() {
        return this.isAngle() ? "&rsaquo;" : "Next";
	}

	get nextCss() {
		let css = '';
		css += this.isAngle() ? "angle" : "";
		css += this.isLastPage() ? " disabled" : "";
		return css;
	}

	pageSelectedCss(cpage: number): string {
		return (this.page === cpage) ? 'selected' : '';
	}

    changePageNumber(newItemsPerPage: number) {
		this.page = Math.floor((this.page * this.rows) / newItemsPerPage);
		this.rows = newItemsPerPage;
		this.paginate(this.rows, this.page);
    }

    isFirstPage(): boolean {
        if (this.page === 1) {
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
        if (this.isFirstPage())
            {return;}
        this.page = 1;
		this.paginate(this.rows, this.page);
    }

    changePageToPrev(event: any): void {
        if (this.isFirstPage())
            {return;}
        else {this.page--;}

		this.paginate(this.rows, this.page);
    }

    changePageToNext(event: any): void {
        if (this.isLastPage())
            {return;}
        else
            {this.page++;}
		this.paginate(this.rows, this.page);
	}

    changePageToLast(event: any): void {
        if (this.isLastPage())
            {return;}
        else
            {this.page = this.getPageCount();}
		this.paginate(this.rows, this.page);
	}

    onPageLinkClick(page: number): void {
        if (page !== undefined && (this.page !== page)) {
            this.page = page;
			this.paginate(this.rows, this.page);
        }
    }

    getPageCount(): number {
		if (this.totalRecords > 0) {
			return Math.ceil(this.totalRecords / this.rows);
        }
        return 1;
    }

    paginate(rows, page) {
        this.checkVisibleNumbers();
        this.onPageChange.emit({ rows, page });
    }

    checkVisibleNumbers() {
        this.pageOptions = [];
        const currentPage = this.page + 1, totalPages = this.getPageCount();
        const step = 2; // Current page +- step
        const paging = [];

        //end pagination at CurrentPage+2 or total pages, whichever is smallest, but up to step*2 + 1 options
        const end = Math.min(Math.max(currentPage + step, 1 + 2 * step), totalPages);
        //start pagination at CurrentPage-2 or end-4, whichever is smallest, but no lower than 1
        const start = Math.max(Math.min(currentPage - step, end - 2 * step), 1);

        for (let i = start; i <= end; i++) {
            paging.push(i); 
        }

        this.pageOptions = paging;
    }

    get fromDisplayValue() {
		return Math.min((this.page * this.rows) + 1, this.totalRecords);
    }

    get toDisplayValue() {
		if (this.totalRecords <= this.rows) {
            return this.totalRecords;
        } else {
			return Math.min((this.page * this.rows) + this.rows, this.totalRecords);
        }
    }
}
