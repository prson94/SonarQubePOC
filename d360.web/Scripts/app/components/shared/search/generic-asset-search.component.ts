import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, HostListener, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { AssetService } from '../../../services/asset.service';
import { AssetSearchFilter, CommonComponentAssetResult } from '../../../models/asset-search.model';

declare var CompanySettings;

@Component({
    selector: 'd3s-asset-search',
    templateUrl: 'generic-asset-search.component.html',
    providers: [AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class AssetSearchComponent {

    // Holds the selected assets to provide back to parent component. Can be pre-populated as well.
    @Input() results: CommonComponentAssetResult[] = [];
    @Output() resultsChange: EventEmitter<any> = new EventEmitter();

    // Allow option to select many items within the control.
    @Input() multiSelect: boolean = false;
    @Output() multiSelectChange: EventEmitter<any> = new EventEmitter();

    private isSearchWindowOpened: boolean = false;

    private searchOption = new AssetSearchFilter();
    private searchresults: CommonComponentAssetResult[] = [];
    private searchResultsCount: number;

    private readonly pageSize: number = 6;
    private pageNum: number = 1;

    constructor(
        private router: Router,
        private ref: ChangeDetectorRef,
        private assetService: AssetService
    ) {

    }

    @HostListener('document:keydown', ['$event']) onKeydownHandler(event: KeyboardEvent) {
        if (event.key === "Escape") {
            this.closeSearch();
        }
    }

    private closeSearch() {
        this.isSearchWindowOpened = false;
        this.searchresults = [];
    }

    paginate($event, el) {
        this.pageNum = $event.page;
        this.search(null, el);
    }


    private search($event, autocompleteElement: any) {
        if ($event)
            this.searchOption.SearchPhrase = $event.query;

        if (this.searchOption.SearchPhrase == '')
            return;
        this.searchOption.PageSize = this.pageSize;
        this.searchOption.PageNum = this.pageNum;

        this.assetService.searchAssetPath(this.searchOption)
            .subscribe(result => {
                this.searchresults = result.items;
                this.searchResultsCount = result.total;
                if (autocompleteElement)
                    autocompleteElement.focusInput();
                this.ref.markForCheck();
            });
    }

    private openSearchWindow() {
        this.isSearchWindowOpened = true;
    }

    private onSelect(event: CommonComponentAssetResult) {
        this.closeSearch();
        this.results.push(event);
        this.resultsChange.emit({ action: 'added', item: event });
    }

    private unselect(idx: number) {
        var item = this.results[idx];
        this.results.splice(idx, 1);
        this.resultsChange.emit({ action: 'deleted', item: item });

    }

}


