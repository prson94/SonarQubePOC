import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, HostListener, Output, EventEmitter } from '@angular/core';
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


    private isSearchWindowOpened: boolean = false;
    private searchresults: CommonComponentAssetResult[] = [];

    // Holds the selected assets to provide back to parent component. Can be pre-populated as well.
    @Input() results: CommonComponentAssetResult[] = [];
    @Output() resultsChange: EventEmitter<any> = new EventEmitter();

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

    private search($event) {
        var searchOption = new AssetSearchFilter();
        searchOption.SearchPhrase = $event.query;
        this.assetService.searchAssetPath(searchOption)
            .subscribe(result => {
                this.searchresults = result;
                this.ref.markForCheck();
            });
    }

    private openSearchWindow() {
        this.isSearchWindowOpened = true;
    }

    private onSelect(event: CommonComponentAssetResult) {
        this.closeSearch();
        this.results.push(event);
        this.resultsChange.emit({ action:'added', item: event });
    }

    private unselect(idx: number) {
        var item = this.results[idx];
        this.results.splice(idx, 1);
        this.resultsChange.emit({ action: 'deleted', item: item });

    }

}


