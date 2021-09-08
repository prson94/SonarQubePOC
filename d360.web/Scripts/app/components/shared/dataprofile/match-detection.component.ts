import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { Router } from '@angular/router';
import { LazyLoadEvent } from 'primeng/api';
import { Observable, ReplaySubject } from 'rxjs';
import { FieldType } from '../../../models/fieldtype-api.model';
import { AssetService } from '../../../services/asset.service';
import { DataProfileService } from '../../../services/dataprofile.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { AdvancedFilterFieldType, Filters } from '../../assets-grid/advanced-filtering/advanced-filtering.models';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'match-detection',
    templateUrl: './match-detection.component.html',
    styleUrls: ['match-detection.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [AssetService, DataProfileService]
})

export class MatchDetectionComponent extends BaseComponent implements OnChanges {
    @Input() isVisible: boolean = false;
    @Input() showDuplicates: boolean = true;
    @Input() showSimilar: boolean = true;

    @Input() assetUid: string = '';
    @Input() matchType: string = '';
    @Output() onClose = new EventEmitter();

    assetPathText: string = '';

    duplicatesData: any[] = [];
    duplicatesDataTotalCount: number = 0;
    duplicatesSelection: any;
    duplicatesDataLoading: boolean = true;
    duplicatesSimpleFilter: string = '';
    duplicateAdvancedFilter: string = "";
    duplicateRowsPerPage: number = 10;
    duplicateCurrentPageNumber: number = 0;

    similarData: any[] = [];
    similarDataTotalCount: number = 0;
    similarSelection: any;
    similarDataLoading: boolean = true;
    similarSimpleFilter: string = '';
    similarAdvancedFilter: string = "";
    similarRowsPerPage: number = 10;
    similarCurrentPageNumber: number = 0;

    filterFields$: Observable<AdvancedFilterFieldType[]>;
    private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);

    menuItems = [
        {
            title: 'Open'
        },
        {
            title: 'Open in New Tab'
        }
    ]

    filterFieldList: AdvancedFilterFieldType[] = [
        {
            Name: 'Tag',
            FriendlyName: 'Tag',
            Type: new FieldType("Tag"),
            Category: "",           
            RemovePopulatedOperator: true
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
            this.loadData();
        }
    }

    private loadData() {
        this.assetService.getAsset(this.assetUid)
            .subscribe((res) => {
                var path = res.Path as string;

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
        this.duplicatesDataLoading = true;

        this.duplicateRowsPerPage = e.rows
        this.duplicateCurrentPageNumber = (e.first / e.rows) + 1;

        localStorage.setItem("duplicate-rows", e.rows.toString());
        this.getData('data');
        
    }

    lazyLoadSimiliar(e: LazyLoadEvent) {
        this.similarDataLoading = true;

        this.similarRowsPerPage = e.rows
        this.similarCurrentPageNumber = (e.first / e.rows) + 1;

        localStorage.setItem("similar-rows", e.rows.toString());


        this.getData('similar');
    }

    onMenuSelect(event, item) {
        if (event.value === "Open") {
            this.router.navigate([`${SiteUrlHelpers.SITE_URL_ASSET_ROOT}/${item.uid}`]);
        }

        if (event.value === "Open in New Tab") {
            window.open(`${SiteUrlHelpers.SITE_URL_ASSET_ROOT}/${item.uid}`, '_blank');
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
        if (type.toLowerCase() == "data") {
            this.dataProfileService.getMatchesByMatchType(this.assetUid, "Data", this.duplicateCurrentPageNumber, this.duplicateRowsPerPage, this.duplicatesSimpleFilter, this.duplicateAdvancedFilter)
                .subscribe((res) => {
                    this.duplicatesDataTotalCount = +res.total;
                    this.duplicatesData = res.items;
                    this.duplicatesDataLoading = false;
                    this.cdRef.markForCheck();
                });
        } else if (type.toLowerCase() == "similar") {
            this.dataProfileService.getMatchesByMatchType(this.assetUid, "Structure", this.similarCurrentPageNumber, this.similarRowsPerPage, this.similarSimpleFilter, this.similarAdvancedFilter)
                .subscribe((res) => {
                    this.similarData = res.items;
                    this.similarDataTotalCount = +res.total;
                    this.similarDataLoading = false;
                    this.cdRef.markForCheck();
                });
        }                
    }

    advancedFiltersChanged($event: Filters, type: string) {
        if (type == 'data') {
            this.duplicateAdvancedFilter = $event.filter;            
            this.getData(type);
        } else if(type == 'similar')  {
            this.similarAdvancedFilter = $event.filter
            this.getData(type);
        }                  
    }
    onFiltersLoaded(type: string) {        
        this.getData(type);        
    }
}   