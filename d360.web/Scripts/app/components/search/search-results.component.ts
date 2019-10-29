import { Component, Input, EventEmitter, Output, ChangeDetectionStrategy, ViewChild, ElementRef, AfterViewInit, ChangeDetectorRef} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SearchService } from '../../services/search.service';
import { SearchResultsObject } from '../../models/search-result.model';
import { CheckTreeNode } from '../shared/small-widgets/check-tree/checktreenode';
import { SearchStateService } from './search-state.service';

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
    @Input() itemsPerPage: number = 5;
    @Input() from: number = 0;
    @Input() loading: boolean = false;

    @Output() selectedCategoryChange = new EventEmitter();
    @Output() advFilterChanged = new EventEmitter();
    @ViewChild('searchContainer') container: ElementRef;

    newFilterOptions: any[] = [];

    ngOnInit() {
    }

    ngOnChanges(changes: any) {
        if (changes['results'] && !changes['results'].firstChange) {
            this.setResultsHeight();
        }
    }

    constructor(private searchStateService: SearchStateService, private ref: ChangeDetectorRef) {
        super();
    }

    ngAfterViewInit(): void {
        this.setResultsHeight();
        this.newFilterOptions.push({ field: "Name", value: 'any' });
        this.newFilterOptions.push({ field: "Description", value: 'any' });
        this.newFilterOptions.push({ field: "Tags", value: 'any' });
    }

    filterChanged(options) {
        this.advFilterChanged.emit(options);
    }

    setResultsHeight() {
        window.setTimeout(() => {
            if (this.container && this.container.nativeElement) {
                this.container.nativeElement.style.height = (window.innerHeight - 108) + 'px';
            }
            this.ref.markForCheck();
        }, 50);
    }

    private nodeSelectDeselect(event) {
        this.selectedCategoryChange.emit(this.searchStateService.selectedFilters);
    }

    private paginate(data) {
        /*
            event.page: New page number
            event.first: Index of first record
            event.rows: Number of rows to display in new page            
            event.pageCount: Total number of pages
        */
        this.searchStateService.page(data.first, data.size);
    }

    private pageNumber() {
        return Math.floor(this.from / this.itemsPerPage);
    }
};