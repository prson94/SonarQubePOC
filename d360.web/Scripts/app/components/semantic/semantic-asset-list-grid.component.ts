import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChange, SimpleChanges } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AssetGridBaseComponent } from '../assets-grid/asset-grid-base.component';
import { DataProfileService } from '../../services/dataprofile.service';
import { CompanySettingsService } from '../../services/settings.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SemanticType, SemanticTypeAsset } from '../../models/semantic-type.model';
import { Observable, ReplaySubject } from 'rxjs';
import { AdvancedFilterFieldType, Filters } from '../assets-grid/advanced-filtering/advanced-filtering.models';
import { FieldType } from '../../models/fieldtype-api.model';
import { LazyLoadEvent } from 'primeng/api';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { SemanticBaseComponent } from './semantics-base.component';
import { FeatureFlagsService } from '../../services/featureflags.service';

@Component({
    selector: 'semantic-asset-list-grid',
    templateUrl: './semantic-asset-list-grid.component.html',
    styleUrls: ["semanticTypes.less"],
    providers: [DataProfileService],
})

export class SemanticAssetListGridComponent extends SemanticBaseComponent implements OnInit, OnChanges {
    @Input() semanticType: SemanticType;
    @Input() isSidePanel: boolean = false;
    @Output() assetCountUpdated = new EventEmitter();
    @Output() selectedAssetChanged = new EventEmitter();

    rowsPerPage: number = 25;
    currentPageNumber: number = 1;
    assets: SemanticTypeAsset[];
    assetsTotal: number;
    selectedAsset: SemanticTypeAsset;
    sortField: string;
    sortOrder: number;

    simpleFilter: string = "";
    advancedFilter: string = "";
    isExportInProgress: boolean = false;
    semanticEffectiveDate: Date;

    menuItems: any[] = [
        { title: "Open" },
        { title: "Open in New Tab" },
    ];

    filterFields$: Observable<AdvancedFilterFieldType[]>;
    private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);

    filterFieldList: AdvancedFilterFieldType[] = [
        {
            Name: 'path',
            FriendlyName: 'Asset Path',
            Type: new FieldType("Text"),
            Category: ""
        },
        {
            Name: 'assetTypePath',
            FriendlyName: 'Asset Type Path',
            Type: new FieldType("Text"),
            Category: ""
        },
        {
            Name: 'outOfDate',
            FriendlyName: 'Out of date classification',
            Type: new FieldType("Boolean"),
            Category: "",
            RemovePopulatedOperator: true
        }     
    ]    

    constructor(private route: ActivatedRoute,
        protected router: Router,
        headerBreadcrumbService: HeaderBreadcrumbService,
        webAnalyticsService: WebAnalyticsService,
        private dataProfileService: DataProfileService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        private featureFlagService: FeatureFlagsService,
    ) {
        super(headerBreadcrumbService, settingsService, router, featureFlagService, secondaryNavService, webAnalyticsService);
    }

    ngOnInit() {
        this.filterFields$ = this.filterFieldsSubject.asObservable();
        this.filterFieldsSubject.next(this.filterFieldList);
        this.filterFieldsSubject.complete();
        
        this.getData();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.getData();
    }

    getData() {
        this.semanticEffectiveDate = new Date(this.semanticType.effectiveDate);
        this.semanticEffectiveDate.setUTCHours(0, 0, 0, 0);
        if (this.semanticTypesEnabled) {
            this.isLoading = true;
            this.dataProfileService.getSemanticTypeMatchingAssets(this.semanticType.qualifier, this.currentPageNumber, this.rowsPerPage, this.semanticType.threshold, this.simpleFilter, this.advancedFilter, this.sortField, this.sortOrder).subscribe((result) => {
                this.assets = result.items;
                if (!this.selectedAsset || !result.items.some((x) => (x.uid === this.selectedAsset.uid))) {
                    this.selectedAsset = result.items[0];
                    this.selectedAssetChanged.emit(this.selectedAsset);
                }
                this.assetsTotal = result.total;
                this.assetCountUpdated.emit({ assetCount: this.assetsTotal });
                this.isLoading = false;
            });
        }       
    }

    advancedFiltersChanged($event: Filters) {
        this.advancedFilter = $event.filter;
        this.getData();
    }

    onSimpleSearch(event: any) {
        this.getData();
    }

    onFiltersLoaded() {
        this.getData();
    }

    lazyLoad(event: LazyLoadEvent) {
        this.rowsPerPage = event.rows;
        this.sortField = event.sortField;
        this.sortOrder = event.sortOrder;
        this.currentPageNumber = (event.first / event.rows) + 1;
        this.getData();
    }

    selectSemanticTypeAsset(asset: SemanticTypeAsset, newTab: boolean = false) {
        let url = `${SiteUrlHelpers.SITE_URL_ASSET_ROOT}/${asset.uid}`;
        if (url) {
            if (newTab) {
                window.open(url, '_blank');
            } else {
                this.router.navigateByUrl(url);
            }
        }
    }
    selectRow(row: any) {
        this.selectedAsset = row;
        this.selectedAssetChanged.emit(row);
    }


    clickMenuItem(event: any, item: SemanticTypeAsset) {
        let key = event.value.toLowerCase();

        if (key === 'open') {
            this.selectSemanticTypeAsset(item);
        } else if (key === 'open in new tab') {
            this.selectSemanticTypeAsset(item, true);
        }
    }

    canExportRecords() {
        return this.assetsTotal <= this.maxExportRows;
    }

    export() {
        this.isExportInProgress = true;
        this.dataProfileService.getSemanticTypeMatchingAssets(this.semanticType.qualifier, 1, this.maxExportRows, this.semanticType.threshold, this.simpleFilter, this.advancedFilter, this.sortField, this.sortOrder, true, this.semanticType.name, () => { this.isExportInProgress = false; });
    }

    isOutOfDate(profileDate) {
        return this.semanticEffectiveDate > new Date(profileDate);
    }
    
}