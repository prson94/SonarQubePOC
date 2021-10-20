import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { Router } from '@angular/router';
import { LazyLoadEvent } from 'primeng/api';
import { Observable, ReplaySubject } from 'rxjs';
import { FieldType } from '../../../models/fieldtype-api.model';
import { forkJoin } from 'rxjs';
import { AssetService } from '../../../services/asset.service';
import { DataProfileService } from '../../../services/dataprofile.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { AdvancedFilterFieldType, Filters } from '../../assets-grid/advanced-filtering/advanced-filtering.models';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'match-detection',
    templateUrl: './match-detection.component.html',
    styleUrls: ["match-detection.less"],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [AssetService, DataProfileService]
})

export class MatchDetectionComponent extends BaseComponent implements OnChanges {
    @Input() isVisible: boolean = false;
    @Input() showDuplicates: boolean = true;
    @Input() showSimilar: boolean = true;

    @Input() assetUid: string = '';
    @Input() matchType: string = '';
    @Input() isParentModal: boolean = false;
    @Output() sidePanelLinkClicked  = new EventEmitter();    
    @Output() onClose = new EventEmitter();    

    assetPathText: string = '';

    duplicatesData: any[] = [];
    duplicatesDataTotalCount: number = 0;
    duplicatesSelection: any[];
    duplicatesDataLoading: boolean = true;
    duplicatesSimpleFilter: string = '';
    duplicateAdvancedFilter: string = "";
    duplicateRowsPerPage: number = 10;
    duplicateCurrentPageNumber: number = 0;

    similarData: any[] = [];
    similarDataTotalCount: number = 0;
    similarSelection: any[];
    similarDataLoading: boolean = true;
    similarSimpleFilter: string = '';
    similarAdvancedFilter: string = "";
    similarRowsPerPage: number = 10;
    similarCurrentPageNumber: number = 0;

    isExportInProgress = false;

    selection: any = null;
    sidePanelOpen: boolean = true;
    sidePanelLoading: boolean = false;
    sidePanelTab: string = 'dataprofile';
    sidePanelStorageKey: string;
    dataProfile: any;

    multipleItemsSelected: boolean = false;

    filterFields$: Observable<AdvancedFilterFieldType[]>;
    private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);

    name: string;

    duplicateSortField: string;
    duplicateSortOrder: number;
    similarSortField: string;
    similarSortOrder: number;

    isTagDrawerVisible: boolean = false;
    tagMatchType: string;
    selectedTagItems: any[] = [];
    tagsChanged: boolean = false;

    menuItems = [
        {
            title: 'Open'
        },
        {
            title: 'Open in New Tab'
        }
    ]

    menuItemsWithTags = [
        {
            title: 'Open'
        },
        {
            title: 'Open in New Tab'
        },
        {
            title: 'Edit Tags'
        }
    ]

    multiselectMenuItems = [        
        {
            title: 'Edit Tags'
        }
    ]

    filterFieldList: AdvancedFilterFieldType[] = [
        {
            Name: 'Tag',
            FriendlyName: 'Tag',
            Type: new FieldType("Tag"),
            Category: "",           
            RemovePopulatedOperator: false
        },
        {
            Name: 'Path',
            FriendlyName: 'Asset Path',
            Type: new FieldType("Path"),
            Category: "",            
            RemovePopulatedOperator: true
        }
    ]        

    constructor(
        private assetService: AssetService,
        private dataProfileService: DataProfileService,
        private cdRef: ChangeDetectorRef,
        private router: Router      
    ) {
        super();        

        this.filterFields$ = this.filterFieldsSubject.asObservable();
        this.filterFieldsSubject.next(this.filterFieldList);
        this.filterFieldsSubject.complete();
    }

    ngOnChanges(changes: SimpleChanges) {        
        if (changes.assetUid && changes.assetUid.currentValue !== changes.assetUid.previousValue) {
            this.selection = null;           
            this.dataProfile = null;
            this.loadData();
        }
        if ((changes.isVisible && changes.isVisible.currentValue === true) && (changes.matchType && changes.matchType.currentValue !== changes.matchType.previousValue)) {
            this.setSelectedMatch();
        }        
    }    

    private loadData() {
        this.assetService.getAsset(this.assetUid)
            .subscribe((res) => {
                var path = res.Path as string;                
                this.name = res.Name;
                this.assetPathText = "Showing matches for " + path.split("].[").join(`&nbsp;&nbsp;<i class='fa fa-angle-right'></i>&nbsp;&nbsp;`)
                    .replace("[", "").replace("]", "");
            });
    }

    lazyLoad(e: LazyLoadEvent, type: string) {
        if (type === "Data") {
            this.lazyLoadDuplicates(e);
        }
        else {
            this.lazyLoadSimiliar(e);            
        }        
    }

    lazyLoadDuplicates(e: LazyLoadEvent) {

        this.duplicateRowsPerPage = e.rows;
        this.duplicateCurrentPageNumber = (e.first / e.rows) + 1;
        this.duplicateSortField = e.sortField;
        this.duplicateSortOrder = e.sortOrder;
        localStorage.setItem("duplicate-rows", e.rows.toString());
        this.getData('data');
        
    }

    lazyLoadSimiliar(e: LazyLoadEvent) {

        this.similarRowsPerPage = e.rows;
        this.similarCurrentPageNumber = (e.first / e.rows) + 1;
        this.similarSortField = e.sortField;
        this.similarSortOrder = e.sortOrder;
        localStorage.setItem("similar-rows", e.rows.toString());

        this.getData('similar');
    }

    onMenuSelect(event, item, matchType, isMultiSelect=false) {
        if (!isMultiSelect && matchType === "duplicate") {
            this.duplicatesSelection = [];
            this.duplicatesSelection.push(item);
        } else if (!isMultiSelect && matchType === "similar") {
            this.similarSelection = [];
            this.similarSelection.push(item);
        }
        if (event.value === "Open") {
            this.router.navigate([`${SiteUrlHelpers.SITE_URL_ASSET_ROOT}/${item.uid}`]);
        }

        if (event.value === "Open in New Tab") {
            window.open(`${SiteUrlHelpers.SITE_URL_ASSET_ROOT}/${item.uid}`, '_blank');
        }

        if (event.value === "Edit Tags") {
            if (Array.isArray(item)) {
                this.selectedTagItems = item;
            } else {
                this.selectedTagItems.push(item);
            }
            this.tagMatchType = matchType.toLowerCase()==="similar" ? "Similar": "Duplicate";
            this.isTagDrawerVisible = true;            
        }
    }

    formatPath(assetPath: string) {
        return assetPath.split('>').join(`&nbsp;&nbsp;<i class='fa fa-angle-right'></i>&nbsp;&nbsp;`);
    }

    get duplicateRows() {
        var savedVal = localStorage.getItem("duplicate-rows");
        return savedVal ? +savedVal : 10;
    }

    get similarRows() {
        var savedVal = localStorage.getItem("similar-rows");
        return savedVal ? +savedVal : 10;
    }

    getLoadIdentifier(type: string) {
        return "MatchDetection" + type + this.assetUid;
    }

    getData(type: string) {        
        if (type.toLowerCase() === "data") {
            this.duplicatesDataLoading = true;
            this.dataProfileService.getMatchesByMatchType(this.assetUid, "Data", this.duplicateCurrentPageNumber, this.duplicateRowsPerPage, this.duplicatesSimpleFilter, this.duplicateAdvancedFilter, this.duplicateSortField, this.duplicateSortOrder)
                .subscribe((res) => {
                    this.duplicatesDataTotalCount = +res.total;
                    this.duplicatesData = res.items;                    
                    this.duplicatesDataLoading = false;      
                    this.setSelectedMatch();  
                    this.cdRef.markForCheck();
                });            
        } else if (type.toLowerCase() === "similar") {
            this.similarDataLoading = true;            
            this.dataProfileService.getMatchesByMatchType(this.assetUid, "Structure", this.similarCurrentPageNumber, this.similarRowsPerPage, this.similarSimpleFilter, this.similarAdvancedFilter, this.similarSortField, this.similarSortOrder)
                .subscribe((res) => {
                    this.similarData = res.items;
                    this.similarDataTotalCount = +res.total;                    
                    this.similarDataLoading = false;       
                    this.setSelectedMatch();                    
                    this.cdRef.markForCheck();
                });
        }        
    }

    advancedFiltersChanged($event: Filters, type: string) {
        if (type === 'data') {            
            this.duplicateAdvancedFilter = $event.filter;
            this.getData(type);
        } else if (type === 'similar') {
            this.similarAdvancedFilter = $event.filter;
            this.getData(type);
        }                  
    }
    onFiltersLoaded(type: string) {
        this.getData(type);        
    }

    canExportRecords(recordCount: number) {
        return recordCount <= this.maxExportRows;
    }

    export(matchType: string) {
        this.isExportInProgress = true;
        if (matchType === "similar") {
            this.dataProfileService.exportMatches(
                this.assetUid, "Structure", this.similarSimpleFilter, this.similarAdvancedFilter, this.name, this.similarSortField, this.similarSortOrder,
                () => { this.isExportInProgress = false; }
            );
        } else if (matchType === "data"){
            this.dataProfileService.exportMatches(
                this.assetUid, "Data", this.duplicatesSimpleFilter, this.duplicateAdvancedFilter, this.name, this.duplicateSortField, this.duplicateSortOrder,
                () => { this.isExportInProgress = false; }
            );
        }       
    }   

    selectMatch(event: any) {
        let selectedAssets = event;
        this.multipleItemsSelected = false;
        if (selectedAssets && selectedAssets.length == 1) {
            //only reload side panel if selection has changed. 
            if (this.selection !== selectedAssets[0]) {
                this.selection = selectedAssets[0];
                this.sidePanelLoading = true;
                this.dataProfileService.getDataProfiles(this.selection.uid).subscribe(
                    (r) => {
                        if (r && r.items && r.items.length > 0 && r.items[0].sampleCount != null) {
                            this.dataProfile = r.items[0];

                            forkJoin(
                                this.dataProfileService.getMatchCounts(this.dataProfile.assetUid, 'Structure'),
                                this.dataProfileService.getMatchCounts(this.dataProfile.assetUid, 'Data')
                            ).subscribe((res) => {
                                this.dataProfile['matches'] = {
                                    structure: res[0],
                                    data: res[1]
                                };
                            });
                        }
                            this.sidePanelLoading = false;
                    });
            }                                    
        } else {
            this.selection = null;
            if (selectedAssets && selectedAssets.length > 1) {
                this.multipleItemsSelected = true;                                
            }
            this.sidePanelLoading = false;
        }

        this.cdRef.markForCheck();
    }

    private changeMatchAsset(event: any) {
        if (event.assetUid != this.assetUid) {
            this.similarAdvancedFilter = '';
            this.duplicateAdvancedFilter = '';
        }
        this.sidePanelLinkClicked.emit({ assetUid: event.assetUid, matchType: event.matchType, showDuplicates: event.showDuplicates, showSimilar: event.showSimilar });
    }

    get panelApplies(): boolean {
        if (this.selection == null || this.sidePanelTab === 'detail') {
            return true;
        }
        if (this.selection != null && this.sidePanelTab === 'dataprofile') {
            return true;
        }
    }   

    private setSelectedMatch() {
        if (this.matchType === "data") {            
                this.duplicatesSelection = this.duplicatesData.slice(0, 1);
                this.selectMatch(this.duplicatesSelection);
                this.similarSelection = [];
        }
        if (this.matchType === "similar") {            
                this.similarSelection = this.similarData.slice(0, 1);
                this.selectMatch(this.similarSelection);
                this.duplicatesSelection = [];
        }        
    }
   
    get selectedAssetUids(): string[] {
        return this.selectedTagItems.filter((t) => t.tags !== undefined).map((t) => t.uid);
    }

    get selectedAssetsWithoutTagField(): any[] {
        return this.selectedTagItems.filter((t) => t.tags === undefined);
    }

    get selectedAssetsWithTagField(): any[] {
        return this.selectedTagItems.filter((t) => t.tags !== undefined);
    }

    get getCommonTags(): string[] {
        var a = this.selectedAssetsWithTagField[0].tags;
        return a.filter((t) => this.selectedAssetsWithTagField.every((c) => c.tags.includes(t)));
    }

    private closeModalDrawer() {
        this.isTagDrawerVisible = false;
        this.selectedTagItems = [];
        this.onTagsChanged();
    }

    private onTagsChanged() {        
        if (this.isTagDrawerVisible === false && this.tagsChanged === true) {
            this.getData('data');
            this.getData('similar');
        }
    }

    itemSelected(item: any, matchType: string, event: MouseEvent) {
        if ((event.ctrlKey || event.metaKey) && !event.shiftKey) {
            this.itemMultiSelect(item, matchType);
        } else {
            this.itemSingleSelect(item, matchType);
        }
    }

    itemMultiSelect(item: any, matchType: string) {
        if (matchType === "duplicate") {
            if (this.duplicatesSelection?.find((x) => x === item)) {
                this.duplicatesSelection = this.duplicatesSelection.filter((x) => x !== item);
            } else {
                if (!this.duplicatesSelection) {
                    this.duplicatesSelection = [];
                }
                this.duplicatesSelection.push(item);
                this.duplicatesSelection = this.duplicatesSelection.filter((x) => x === x);//required for rows to highlight correctly
            }
            this.selectMatch(this.duplicatesSelection);
            
        } else if (matchType === "similar") {    
            if (this.similarSelection?.find((x) => x === item)) {
                this.similarSelection = this.similarSelection.filter((x) => x !== item);
            } else {
                if (!this.similarSelection) {
                    this.similarSelection = [];
                }
                this.similarSelection.push(item);
                this.similarSelection = this.similarSelection.filter((x) => x === x);//required for rows to highlight correctly
            }
            this.selectMatch(this.similarSelection);
        }
    }

    itemSingleSelect(item: any, matchType: string) {
        if (matchType === "duplicate") {
            this.duplicatesSelection = [];
            this.duplicatesSelection.push(item);
            this.selectMatch(this.duplicatesSelection);
        } else if (matchType === "similar") {
            this.similarSelection = [];
            this.similarSelection.push(item);
            this.selectMatch(this.similarSelection);
        }
    }    
}