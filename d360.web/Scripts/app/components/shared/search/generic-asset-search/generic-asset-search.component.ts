import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, HostListener, Output, EventEmitter, ViewChild, ElementRef, OnInit, OnChanges, SimpleChange, SimpleChanges } from '@angular/core';
import { AssetService } from '../../../../services/asset.service';
import { AssetSearchFilter, CommonComponentAssetTypeFilterRelationshipSide, CommonComponentAssetSelection, CommonComponentSelectStyle, CommonComponentAssetResultExt, CommonComponentAssetResult, CommonComponentAssetTypeFilter, CommonComponentDisplayStyle } from '../../../../models/asset-search.model';
import { PredicateType, Predicate } from '../../../../models/predicate.model';
import { RelationshipsService } from '../../../../services/relationships.service';
import { ToolTipService } from '../../../../services/tooltip.service';

declare var CompanySettings;

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

    @Input() resultDisplayStyle: CommonComponentDisplayStyle;

    @Input() relationshipSide: CommonComponentAssetTypeFilterRelationshipSide;

    private isSearchWindowOpened: boolean = false;

    private searchOption = new AssetSearchFilter();
    private searchresults: CommonComponentAssetResultExt[] = [];
    private searchResultsCount: number;

    private isFullPathVisible: boolean = false;

    private readonly pageSize: number = 6;
    private pageNum: number = 1;
    private numberOfPages: number = 1;

    private currentSearchNavigationIndex: number = 0;
    private isLoading: boolean = false;

    @ViewChild('searchInput', { static: true }) searchInput: ElementRef;

    constructor(
        private ref: ChangeDetectorRef,
        private assetService: AssetService,
        private eRef: ElementRef,
        private tooltipService: ToolTipService
    ) {

    }

    ngOnInit() {
        this.prePopulate();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.prepopulatedResults && changes.prepopulatedResults.previousValue != changes.prepopulatedResults.currentValue) {
            this.prePopulate();
        }
    }

    private prePopulate() {
        if (this.prepopulatedResults) {
            this.searchresults = [];
            this.prepopulatedResults.forEach(pr => {
                this.searchresults.push({ AssetTypeUid: pr.AssetTypeUid, Uid: pr.Uid, Segments: pr.Segments, IsSelected: false });
                this.ref.markForCheck();
            })
            this.prepopulatedResults = null;
        }
    }

    @HostListener('document:keydown', ['$event']) onKeydownHandler(event: KeyboardEvent) {
        if (!this.eRef.nativeElement.contains(event.target)) {
            return;
        }

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

        if (event.key === "Enter") {
            if (this.isSearchWindowOpened)
                this.onSelect(this.currentSearchNavigationIndex,null);
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
        if (this.clearResultsAfterSelection)
            this.searchresults = [];
    }

    paginate($event, el) {
        this.pageNum = $event.page + 1;
        this.search(null);
    }


    private search($event) {

        if ($event) {
            if (this.searchOption.SearchPhrase == $event.target.value)
                return;

            this.searchOption.SearchPhrase = $event.target.value;
        }

        if (this.searchOption.SearchPhrase == '')
            return;

        this.searchOption.PageSize = this.pageSize;
        this.searchOption.PageNum = this.pageNum;
        this.searchOption.Filters = this.filters;

        this.isLoading = true;

        this.assetService.searchAssetPath(this.searchOption)
            .subscribe(result => {
                this.searchresults = JSON.parse(JSON.stringify(result.items));

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
            this.selected = [];
            this.selected.push(selectedItem);
        }

        this.selectedChange.emit({ action: 'added', item: selectedItem });
    }

    private unselect(idx: number) {
        var item = this.selected[idx];
        this.selected.splice(idx, 1);
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


