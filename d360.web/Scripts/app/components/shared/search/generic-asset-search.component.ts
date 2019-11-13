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

    // Should we expose the Show/Hide Full Path link in the dropdown
    @Input() showSelectFullpathLink: boolean = true;
    @Output() showSelectFullpathLinkChange: EventEmitter<any> = new EventEmitter();

    private isSearchWindowOpened: boolean = false;

    private searchOption = new AssetSearchFilter();
    private searchresults: CommonComponentAssetResult[] = [];
    private searchResultsCount: number;

    private isFullPathVisible: boolean = false;

    private readonly pageSize: number = 6;
    private pageNum: number = 1;
    private numberOfPages: number = 1;

    private currentSearchNavigationIndex: number = 0;

    constructor(
        private router: Router,
        private ref: ChangeDetectorRef,
        private assetService: AssetService,
        private eRef: ElementRef
    ) {

    }

    @HostListener('document:keydown', ['$event']) onKeydownHandler(event: KeyboardEvent) {
        if (event.key === "Escape") {
            this.closeSearch();
        }

        if (event.key === "ArrowDown") {
            this.currentSearchNavigationIndex++;
            if (this.currentSearchNavigationIndex > this.pageSize - 1)
                this.currentSearchNavigationIndex = this.pageSize - 1;
        }
        if (event.key === "ArrowUp") {
            this.currentSearchNavigationIndex--;
            if (this.currentSearchNavigationIndex < 0)
                this.currentSearchNavigationIndex = 0;
        }
        if (event.key === "ArrowLeft") {
            this.pageNum--;
            if (this.pageNum < 1)
                this.pageNum = 1;

            this.currentSearchNavigationIndex = 0;
            this.search(null);

        }
        if (event.key === "ArrowRight") {
            this.pageNum++;
            if (this.pageNum > this.numberOfPages)
                this.pageNum = this.numberOfPages;
            this.currentSearchNavigationIndex = 0;

            this.search(null);
        }
        if (event.key === "Enter") {
            this.onSelect(this.currentSearchNavigationIndex);
        }
    }

    @HostListener('document:click', ['$event'])
    onclick(ev: MouseEvent) {
        // if clicked outside of the component
        if (!this.eRef.nativeElement.contains(ev.target)) {
            this.closeSearch();
        }
    }

    private closeSearch() {
        this.isSearchWindowOpened = false;
        this.currentSearchNavigationIndex = 0;
        this.searchresults = [];
    }

    paginate($event, el) {
        this.pageNum = $event.page;
        this.search(null);
    }


    private search($event) {
        if ($event)
            this.searchOption.SearchPhrase = $event.target.value;

        if (this.searchOption.SearchPhrase == '')
            return;
        this.searchOption.PageSize = this.pageSize;
        this.searchOption.PageNum = this.pageNum;

        this.assetService.searchAssetPath(this.searchOption)
            .subscribe(result => {
                this.searchresults = JSON.parse(JSON.stringify(result.items));

                //load test data
                //this.searchresults = [];
                //var item1 = new CommonComponentAssetResult();
                //item1.AssetTypeUid = '';
                //item1.Uid = '';
                //item1.Segments = [];
                //item1.Segments.push({ Value: 'AzureRemoteHost' });
                //item1.Segments.push({ Value: 'EnrolDB' });
                //item1.Segments.push({ Value: 'SSMS' });
                //item1.Segments.push({ Value: 'MEMBER_INFO' });
                //item1.Segments.push({ Value: 'Name' });

                //this.searchresults.push(item1);

                //var item2 = new CommonComponentAssetResult();
                //item2.AssetTypeUid = '';
                //item2.Uid = '';
                //item2.Segments = [];
                //item2.Segments.push({ Value: 'OracleHost' });
                //item2.Segments.push({ Value: 'ClaimsDb' });
                //item2.Segments.push({ Value: 'dbo' });
                //item2.Segments.push({ Value: 'MEMBERS' });
                //item2.Segments.push({ Value: 'MEMBER_NAME' });

                //this.searchresults.push(item2);

                //var item3 = new CommonComponentAssetResult();
                //item3.AssetTypeUid = '';
                //item3.Uid = '';
                //item3.Segments = [];
                //item3.Segments.push({ Value: 'Data Warehouse' });
                //item3.Segments.push({ Value: 'DWDB' });
                //item3.Segments.push({ Value: 'edm' });
                //item3.Segments.push({ Value: 'MEMBERS' });
                //item3.Segments.push({ Value: 'MEMBER_NAME' });

                //this.searchresults.push(item3);

                this.searchResultsCount = result.total;
                this.numberOfPages = Math.ceil(result.total / result.pageSize);

                //Dont reset if event is not sent from input
                this.ref.markForCheck();
            });
    }

    private openSearchWindow() {
        this.isSearchWindowOpened = true;
    }

    private onSelect(idx: number) {

        var selectedItem = this.searchresults[idx];
        this.closeSearch();

        if (this.multiSelect)
            this.results.push(selectedItem);
        else {
            this.results = [];
            this.results.push(selectedItem);
        }
        this.resultsChange.emit({ action: 'added', item: selectedItem });
    }

    private unselect(idx: number) {
        var item = this.results[idx];
        this.results.splice(idx, 1);
        this.resultsChange.emit({ action: 'deleted', item: item });
    }

    toggleFullPaths(autocompleteElement) {
        this.isFullPathVisible = !this.isFullPathVisible;
        this.reBindData();
        if (autocompleteElement)
            autocompleteElement.focusInput();
        this.ref.markForCheck();
    }

    private reBindData() {
        this.results = JSON.parse(JSON.stringify(this.results));
    }

}


