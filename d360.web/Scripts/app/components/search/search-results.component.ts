import { Component, Input, ChangeDetectionStrategy, ChangeDetectorRef} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SearchResultsObject } from '../../models/search-result.model';
import { SearchStateService } from './search-state.service';

@Component({
    selector: 'd3s-search-results',
    templateUrl: './search-results-component.html',
    changeDetection: ChangeDetectionStrategy.Default
})

export class SearchResultsComponent extends BaseComponent {
    @Input() results: SearchResultsObject;
    @Input() itemsPerPage: number = 5;
    @Input() from: number = 0;
    @Input() loading: boolean = false;

    constructor(public searchStateService: SearchStateService, private ref: ChangeDetectorRef) {
        super();
    }

    paginate(data) {
        /*
            event.page: New page number
            event.first: Index of first record
            event.rows: Number of rows to display in new page            
            event.pageCount: Total number of pages
        */
        this.searchStateService.page(data.first, data.size);
    }

};