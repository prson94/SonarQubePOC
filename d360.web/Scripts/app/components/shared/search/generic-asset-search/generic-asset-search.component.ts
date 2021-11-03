import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, HostListener, Output, EventEmitter, ViewChild, ElementRef, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { AssetService } from '../../../../services/asset.service';
import { AssetSearchFilter, CommonComponentAssetTypeFilterRelationshipSide, CommonComponentAssetSelection, CommonComponentSelectStyle, CommonComponentAssetResultExt, CommonComponentAssetResult, CommonComponentAssetTypeFilter, CommonComponentDisplayStyle } from '../../../../models/asset-search.model';
import { PredicateType, Predicate } from '../../../../models/predicate.model';
import { RelationshipsService } from '../../../../services/relationships.service';
import { ToolTipService } from '../../../../services/tooltip.service';

@Component({
    selector: 'd3s-asset-search',
    templateUrl: 'generic-asset-search.component.html',
    providers: [AssetService, RelationshipsService, ToolTipService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class AssetSearchComponent implements OnInit, OnChanges {

    // Holds the selected assets to provide back to parent component. Can be pre-populated as well.
    @Input() selected: CommonComponentAssetSelection[] = [];
    @Output() selectedChange: EventEmitter<any> = new EventEmitter();

    // An option to pre-load data into the dropdown. Gives the most-likely options that a user would select, before a search is conducted. [Optional]
    @Input() prepopulatedResults: CommonComponentAssetResult[];

    // Allow option to select many items within the control.
    @Input() multiSelect: boolean = false;

    // Should we expose the Show/Hide Full Path link in the dropdown
    @Input() showSelectFullpathLink: boolean = true;

    // Allow option to provide a drop-down of predicates based on functional type. Used only when in multi-select mode.
    @Input() showPredicateSelector: boolean = false;

    // When selector is enabled, filters a list of predicates based on the type below.
    @Input() predicateSelectorType: PredicateType;;
    @Output() predicateSelectorTypeChange = new EventEmitter();

    // How should users select the options.
    @Input() multiSelectStyle: CommonComponentSelectStyle = CommonComponentSelectStyle.Button;

    // What should be the label on the button if multi-select is enabled and the    style is set appropriately.
    @Input() multiSelectButtonLabel: string = 'No Label';
    @Input() placeholder: string = 'No placeholder';

    //If true, search results wont be cleared after selection
    @Input() clearResultsAfterSelection: boolean = false;

    // What should we filter on, based on a combination of criteria. Optional.
    @Input() filters: CommonComponentAssetTypeFilter[];

    // Based on the selection here, which will also be updated based on the Show Full Path link
    @Input() resultDisplayStyle: CommonComponentDisplayStyle = CommonComponentDisplayStyle.AbbreviatedPath;

    @Input() relationshipSide: CommonComponentAssetTypeFilterRelationshipSide;

    @Input() isDisabled: boolean = false;

    @Input() maxItems: number;

    isSearchWindowOpened: boolean = false;

    searchOption = new AssetSearchFilter();
    searchresults: CommonComponentAssetResultExt[] = [];
    searchResultsCount: number;
    isSearchPhraseValid: boolean = true;

    isFullPathVisible: boolean = false;

    readonly pageSize: number = 10;
    pageNum: number = 1;
    numberOfPages: number = 1;

    currentSearchNavigationIndex: number = 0;
    isLoading: boolean = false;
    displayStyle: string = '';

    @ViewChild('searchInput', { static: true }) searchInput: ElementRef;

    constructor(
        private ref: ChangeDetectorRef,
        private assetService: AssetService,
        private eRef: ElementRef) {

    }

    @HostListener('document:keydown', ['$event']) onKeydownHandler(event: KeyboardEvent) {
        if (!this.eRef.nativeElement.contains(event.target)) {
            return;
        }

        if (event.key === "ArrowDown" || event.key === "Down") {
            this.currentSearchNavigationIndex++;
            if (this.currentSearchNavigationIndex > this.pageSize - 1)
                this.currentSearchNavigationIndex = this.pageSize - 1;
        }
        if (event.key === "ArrowUp" || event.key === "Up") {
            this.currentSearchNavigationIndex--;
            if (this.currentSearchNavigationIndex < 0)
                this.currentSearchNavigationIndex = 0;
        }

        if (event.key === "Enter") {
            if (this.isSearchWindowOpened)
                this.onSelect(this.currentSearchNavigationIndex, null);
        }
    }

    @HostListener('document:click', ['$event'])
    onclick(ev: MouseEvent) {
        var target = <HTMLElement>ev.target;
        // if clicked outside of the component
        if (!this.eRef.nativeElement.contains(target) && this.isSearchWindowOpened) {
            this.closeSearch();
        }
    }


    ngOnInit() {
        if (this.resultDisplayStyle == CommonComponentDisplayStyle.AbbreviatedPath
            || this.resultDisplayStyle == CommonComponentDisplayStyle.Name) {
            this.isFullPathVisible = false;
        }
        else {
            this.isFullPathVisible = true;
        }

        this.displayStyle = 'display-style-' + this.resultDisplayStyle.toString();
    }

    ngOnChanges(changes: SimpleChanges) {

        if (changes.prepopulatedResults && changes.prepopulatedResults.previousValue != changes.prepopulatedResults.currentValue) {
            this.prePopulate();
        }

        if (changes.selected && changes.selected.previousValue != changes.selected.currentValue) {
            clearTimeout(this.resolveAssetTimeout);
            this.resolveAssetTimeout = setTimeout(() => this.resolveAssetSegments(), 50);
        }


    }
    private resolveAssetTimeout = null;
    private resolveAssetSegments() {
        var itemsToResolve = [];
        this.selected.forEach(item => {
            if (!item.Segments) itemsToResolve.push({ uid: item.Uid, typeUid: item.AssetTypeUid });
        });
        let groups = itemsToResolve.reduce((r, a) => {
            r[a.typeUid] = [...r[a.typeUid] || [], a];
            return r;
        }, {});

        Object.keys(groups).forEach((key) => {
            var assets = groups[key].map(x => x.uid);
            var params = { _assetUid: assets.join(','), includeSegments: true };
            this.assetService.getAssets(key, params).subscribe(res => {
                var items = res.items;
                if (items) {
                    items.forEach(asset => {
                        var update = this.selected.find(x => x.Uid == asset.AssetUid && x.AssetTypeUid == asset.AssetTypeUid);
                        update.Segments = asset.Segments;
                        if (!asset.Segments) {
                            update.Segments = [];
                            update.Segments.push({ Value: asset.Name });
                        }
                    });
                }
                this.ref.markForCheck();
            });
        });
    }

    private prePopulate() {
        if (this.prepopulatedResults && this.prepopulatedResults.length > 0) {
            this.searchresults = [];
            this.prepopulatedResults.forEach(pr => {
                this.searchresults.push({ AssetTypeName: pr.AssetTypeName, AssetTypeIcon: pr.AssetTypeIcon, AssetTypeUid: pr.AssetTypeUid, Uid: pr.Uid, Segments: pr.Segments, IsSelected: false });
                this.ref.markForCheck();
            })

            var itemsToResolve = [];
            this.searchresults.forEach(item => {
                if (!item.Segments) itemsToResolve.push({ uid: item.Uid, typeUid: item.AssetTypeUid });
            });
            let groups = itemsToResolve.reduce((r, a) => {
                r[a.typeUid] = [...r[a.typeUid] || [], a];
                return r;
            }, {});

            Object.keys(groups).forEach((key) => {
                var assets = groups[key].map(x => x.uid);
                var params = { _assetUid: assets.join(','), includeSegments: true };
                this.assetService.getAssets(key, params).subscribe(res => {
                    var items = res.items;
                    if (items) {
                        items.forEach(asset => {
                            var update = this.searchresults.find(x => x.Uid == asset.AssetUid && x.AssetTypeUid == asset.AssetTypeUid);
                            if (update) {
                                update.Segments = [];
                                update.Segments = asset.Segments;
                                if (!asset.Segments) {
                                    update.Segments = [];
                                    update.Segments.push({ Value: asset.Name });
                                }
                            }
                        });
                    }
                    this.ref.markForCheck();
                });
            });

        }
    }

    private closeSearch() {
        this.isSearchWindowOpened = false;
        this.isSearchPhraseValid = true;
        this.currentSearchNavigationIndex = 0;
        if (this.clearResultsAfterSelection)
            this.searchresults = [];

        this.ref.markForCheck();
    }

    paginate($event, el) {
        this.pageNum = $event.page + 1;
        this.search(null);
    }


    private isValidPhrase(phrase: string): boolean {
        if (!phrase || phrase.length == 0) return false;
        return phrase.split('').some(character => '0123456789abcdefghijklmnopqrstuvwxyzABCEDEFGHIJKLMNOPQRSTUVWXYZ'.includes(character));
    }


    search($event) {
        if ($event) {
            if ($event.key === 'Escape' || $event.key === 'Esc') {
                if (this.isSearchWindowOpened)
                    this.closeSearch();
            }

            if (this.searchOption.SearchPhrase == $event.target.value)
                return;

            this.searchOption.SearchPhrase = $event.target.value;
        }

        this.isSearchPhraseValid = this.isValidPhrase(this.searchOption.SearchPhrase);

        if (!this.isSearchPhraseValid)
            return;

        this.searchOption.PageSize = this.pageSize;
        this.searchOption.PageNum = this.pageNum;
        this.searchOption.Filters = this.filters;

        this.isLoading = true;
        this.assetService.searchAssetPath(this.searchOption)
            .subscribe(result => {
                this.searchresults = JSON.parse(JSON.stringify(result.items));

                this.selected.forEach(s => {
                    let ix = this.searchresults.findIndex(x => x.Uid == s.Uid);

                    if (ix > -1) {
                        this.searchresults.splice(ix, 1);
                        this.searchresults = this.searchresults.slice();
                    }
                });

                this.searchResultsCount = result.total;
                this.numberOfPages = Math.ceil(result.total / result.pageSize);

                this.searchresults.forEach(sr => {
                    if (this.selected.some(x => x.Uid == sr.Uid)) {
                        sr.IsSelected = true;
                    }
                    else {
                        sr.IsSelected = false;
                    }
                });
                this.isLoading = false;
                this.ref.markForCheck();
            });
    }

    private openSearchWindow() {
        this.isSearchWindowOpened = true;
        setTimeout(() => {
            this.searchInput.nativeElement.focus();
        }, 100);
    }



    private onSelect(idx: number, $event: any) {

        //input type=checkbox triggers click 2 times, lets skip it
        if ($event && $event.target.className.indexOf('checker') != -1) {
            return;
        }
        var item = this.searchresults[idx];

        if (this.selected.some(x => x.Uid == item.Uid)) {

            if (this.multiSelectStyle == CommonComponentSelectStyle.CheckBox) {
                this.unselectByUID(item.Uid);
            }
            return;
        }

        var selectedItem = new CommonComponentAssetSelection();
        selectedItem.AssetTypeUid = item.AssetTypeUid;
        selectedItem.Uid = item.Uid;
        selectedItem.Predicate = null;
        selectedItem.Segments = item.Segments;

        if (!this.multiSelectStyle || this.multiSelectStyle == CommonComponentSelectStyle.Button)
            this.closeSearch();

        if (this.multiSelect)
            this.selected.push(selectedItem);
        else {
            while (this.selected.length) {
                this.selected.pop();
            }
            this.selected.push(selectedItem);
        }

        this.searchresults.splice(idx, 1);
        this.searchresults = this.searchresults.slice();
        this.selectedChange.emit({ action: 'added', item: selectedItem });
    }

    private unselect(idx: number) {
        var item = this.selected[idx];
        this.selected.splice(idx, 1);
        this.searchresults = this.searchresults.slice();
        this.selectedChange.emit({ action: 'removed', item: item });
    }

    private unselectByUID(uid: string) {
        this.unselect(this.selected.findIndex(x => x.Uid == uid));
    }

    toggleFullPaths() {
        this.isFullPathVisible = !this.isFullPathVisible;
    }

    private predicateSelected(event: Predicate, idx: number) {
        var item = this.selected[idx];
        item.Predicate = event;

        this.selectedChange.emit({ action: 'predicate-updated', item: item });
    }

}


