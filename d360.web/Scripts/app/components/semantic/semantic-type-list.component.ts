import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { AssetGridBaseComponent } from '../assets-grid/asset-grid-base.component';
import { DataProfileService } from '../../services/dataprofile.service';
import { CompanySettingsService } from '../../services/settings.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { SemanticType } from '../../models/semantic-type.model';
import { AdvancedFilterFieldType, Filters } from '../assets-grid/advanced-filtering/advanced-filtering.models';
import { Observable, ReplaySubject, Subscription } from 'rxjs';
import { FieldType } from '../../models/fieldtype-api.model';
import { LazyLoadEvent } from 'primeng/api';
import { StringConstants } from '../../static/string-constants';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-semantic-list',
    templateUrl: './semantic-type-list.component.html',
    styleUrls: ["semanticTypes.less"],
    providers: [DataProfileService],
})

export class SemanticTypeListComponent extends AssetGridBaseComponent implements OnInit, OnDestroy {

    @Output() selectedTypeChanged = new EventEmitter();    
    sub: any;


    selectedType: any = null;
    semanticTypes: SemanticType[];   
    simpleFilter: string = "";
    advancedFilter: string = "";
    semanticsTotal: number = 0;
    rowsPerPage: number = this.defaultInitialItemsPerPage;
    currentPageNumber: number = 1;
    showSidePanel: boolean = true;
    private sidePanelOpen: boolean = false;
    sidePanelTab: string = 'detail';
    sidePanelStorageKey: string;
    navigationItemsSubs: Subscription[] = [];

    filterFields$: Observable<AdvancedFilterFieldType[]>;
    private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);

    menuItems: any[] = [
        { title: "Open" },
        { title: "Open in New Tab" },
    ];

    filterFieldList: AdvancedFilterFieldType[] = [
        {
            Name: 'Name',
            FriendlyName: 'Name',
            Type: new FieldType("Text"),
            Category: ""
        },
        {
            Name: 'Qualifier',
            FriendlyName: 'Qualifier',
            Type: new FieldType("Text"),
            Category: ""
        },
        {
            Name: 'Status',
            FriendlyName: 'Status',
            Type: new FieldType("Text"),
            Category: ""
        },
        {
            Name: 'Priority',
            FriendlyName: 'Priority',
            Type: new FieldType("Number"),
            Category: ""
        },
        {
            Name: 'BaseType',
            FriendlyName: 'Base Type',
            Type: new FieldType("Text"),
            Category: ""
        },
        {
            Name: 'Description',
            FriendlyName: 'Description',
            Type: new FieldType("Text"),
            Category: ""
        },
        {
            Name: 'Threshold',
            FriendlyName: 'Threshold',
            Type: new FieldType("Number"),
            Category: ""
        },
        {
            Name: 'MatchType',
            FriendlyName: 'Match Type',
            Type: new FieldType("Text"),
            Category: ""
        },
        {
            Name: 'Source',
            FriendlyName: 'Semantic Source',
            Type: new FieldType("Text"),
            Category: ""
        },
        {
            Name: 'CreatedOn',
            FriendlyName: 'Date Created',
            Type: new FieldType("DateTime"),
            Category: ""
        },
        {
            Name: 'UpdatedOn',
            FriendlyName: 'Date Modified',
            Type: new FieldType("DateTime"),
            Category: ""
        },
        {
            Name: 'CreatedBy',
            FriendlyName: 'Created By',
            Type: new FieldType("Text"),
            Category: ""
        },
        {
            Name: 'UpdatedBy',
            FriendlyName: 'Modified By',
            Type: new FieldType("Text"),
            Category: ""
        }
    ]
    

    constructor(private route: ActivatedRoute,
        private router: Router,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title,
        webAnalyticsService: WebAnalyticsService,
        private dataProfileService: DataProfileService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService) {
        super(headerBreadcrumbService, settingsService, secondaryNavService, webAnalyticsService);
    }

    ngOnInit() {

        this.sidePanelStorageKey = 'SemanticTypes_' + CurrentResourceID;

        this.filterFields$ = this.filterFieldsSubject.asObservable();
        this.filterFieldsSubject.next(this.filterFieldList);
        this.filterFieldsSubject.complete();           

        this.displayBreadCrumbs();
    }

    getData() {
        this.isLoading = true;
        this.dataProfileService.getSemanticTypes(this.currentPageNumber, this.rowsPerPage, this.simpleFilter, this.advancedFilter).subscribe((p) => {
            this.semanticTypes = p.items;
            this.semanticsTotal = p.total;
            if (this.semanticTypes && !this.selectedType || !p.items.some((x) => (x.uid === this.selectedType.uid))) {
                this.selectRow(this.semanticTypes[0]);
            }            
            this.isLoading = false;
        });
    }

    getCertificationStatusColor(status: string) {
        status = status?.toLowerCase().trim();
        if (status) {
            switch (status) {
                case 'draft':
                    return '#BBBBBB';
                case 'certified':
                    return '#3f9d40';
                case 'under review':
                    return '#e2792a';
                default:
                    //custom status, we need to generate a color
                    let hash = 0;
                    for (let i = 0; i < status.length; i++) {
                        hash = status.charCodeAt(i) + ((hash << 5) - hash);
                        hash = hash & hash;
                    }
                    return `hsl(${(hash * 2) % 360}, 70%, 70%)`;
            }
        }
    }

    lazyLoad(event: LazyLoadEvent) {
        this.rowsPerPage = event.rows;
        this.currentPageNumber = (event.first / event.rows) + 1;
        this.getData();
    }

    onFiltersLoaded() {
        this.getData();
    }

    advancedFiltersChanged($event: Filters) {
        this.advancedFilter = $event.filter;
        this.getData();
    }

    onSimpleSearch(event: any) {
        this.getData();
    }

    selectRow(row: any) {
        this.selectedType = row;
        if (this.selectedType) {
            this.buildSecondaryNavigation(this.selectedType.uid, 0, 'SemanticType', null, null, null, null);
        }        
        this.selectedTypeChanged.emit(row);
    }

    selectSemanticType(semanticType: SemanticType, newTab: boolean = false)
    {
        let url = `${SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT}/${semanticType.uid}`;
        if (url) {
            if (newTab) {
                window.open(url, '_blank');
            } else {
                this.router.navigateByUrl(url);
            }
        }
    }

    clickMenuItem(event: any, item: SemanticType) {
        let key = event.value.toLowerCase();

        if (key === 'open') {
            this.selectSemanticType(item);
        } else if (key === 'open in new tab') {
            this.selectSemanticType(item, true);
        }
    }    

    displayBreadCrumbs() {
        this.secondaryNavService.showHeader(true);

        this.sub = this.route.params.subscribe((params) => {
            this.setBrowserTitle(this.titleService, 'SemanticTypes');

            this.headerBreadcrumbService.getFolderTitle('#SemanticTypes').then((res) => {
                this.folderTitle = res;
                this.setBrowserTitle(this.titleService, res);
                this.area = res;

                this.headerBreadcrumbService.clearBreadcrumbs();
                this.headerBreadcrumbService.clearCurrentObjectInfo();
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(res, SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT));

                var breadCrumbsSub = this.headerBreadcrumbService.getFolderIcon(res).subscribe((icon) => {
                    this.secondaryNavService.clearItems();
                    this.secondaryNavService.clearCurrentObject();
                    this.secondaryNavService.setCurrentArea(res, icon, StringConstants.Section_SemanticTypes);
                    this.buildSecondaryNavigationForObject(0, 'SemanticType');
                    this.secondaryNavService.showHeader(true);
                });
                this.navigationItemsSubs.push(breadCrumbsSub);
            });

        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}
