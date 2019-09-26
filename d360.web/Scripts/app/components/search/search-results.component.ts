import { Component, OnInit, Input, EventEmitter, Output, ChangeDetectionStrategy, ViewChild, ElementRef, AfterViewInit, ChangeDetectorRef} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SearchService } from '../../services/search.service';
import { SearchResultsObject, SearchResultInfo, SearchCategories } from '../../models/search-result.model';

@Component({
    selector: 'd3s-search-results',
    templateUrl: './search-results-component.html',
    providers: [SearchService],
    changeDetection: ChangeDetectionStrategy.Default,
    host: {
        '(window:resize)': 'setResultsHeight()'
    },  
})

export class SearchResultsComponent extends BaseComponent implements AfterViewInit {
    @Input() results: SearchResultsObject;
    @Input() categories: SearchCategories[] = [];    
    @Input() itemsPerPage: number = 5;
    @Input() from: number = 0;
    @Input() loading: boolean = false;
    @Input() useSubscription: boolean = false;
        
    @Output() paginateClick = new EventEmitter();    
    @Input() selectedCategory: SearchCategories;
    @Output() selectedCategoryChange = new EventEmitter();

    @ViewChild('searchContainer') container: ElementRef;

    ngOnChanges(changes: any) {
        if (changes['results'] && !changes['results'].firstChange) {
            this.setResultsHeight();
        }
    }

    constructor(private searchService: SearchService, private ref: ChangeDetectorRef) {
        super();
    }

    ngAfterViewInit(): void {
        this.setResultsHeight();
    }

    setResultsHeight() {
        window.setTimeout(() => {
            if (this.container && this.container.nativeElement) {
                this.container.nativeElement.style.height = (window.innerHeight - 108) + 'px';
            }
            this.ref.markForCheck();
        }, 50);
    }

    private selectCategory(category) {
        this.selectedCategory = category;

        this.selectedCategoryChange.emit(this.selectedCategory);
    }

    private paginate(data) {
        /*
            event.page: New page number
            event.first: Index of first record
            event.rows: Number of rows to display in new page            
            event.pageCount: Total number of pages
        */
        this.paginateClick.emit({page: data.page, size: data.size, first: data.first});
    }

    private pageNumber() {
        return Math.floor(this.from / this.itemsPerPage);
    }
};