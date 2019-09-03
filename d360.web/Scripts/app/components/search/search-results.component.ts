import { Component, OnInit, Input, EventEmitter, Output, ChangeDetectionStrategy, ViewChild} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SearchService } from '../../services/search.service';
import { SearchResultsObject, SearchResultInfo, SearchCategories } from '../../models/search-result.model';
import { Paginator } from 'primeng/primeng';

@Component({
    selector: 'd3s-search-results',
    templateUrl: './search-results-component.html',
    providers: [SearchService],
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class SearchResultsComponent extends BaseComponent {
    @Input() results: SearchResultsObject;
    @Input() categories: SearchCategories[] = [];    
    @Input() itemsPerPage: number = 5;
    @Input() from: number = 0;
    @Input() loading: boolean = false;
        
    @Output() paginateClick = new EventEmitter();    
    @Input() selectedCategory: SearchCategories;
    @Output() selectedCategoryChange = new EventEmitter();

    @ViewChild('pag') paginator: Paginator;

    ngOnChanges(changes: any) {
        if (changes.from != undefined && this.paginator != undefined) {
            this.paginator.updatePaginatorState();
        }
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
};