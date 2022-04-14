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
import { SemanticSource, SemanticType } from '../../models/semantic-type.model';
import { AdvancedFilterFieldType, Filters, LookupValuesAPIModel, LookupValuesAPIParameters } from '../assets-grid/advanced-filtering/advanced-filtering.models';
import { Observable, of, ReplaySubject, Subscription } from 'rxjs';
import { FieldType } from '../../models/fieldtype-api.model';
import { LazyLoadEvent } from 'primeng/api';
import { StringConstants } from '../../static/string-constants';
import { SemanticBaseComponent } from './semantics-base.component';
import { FeatureFlagsService } from '../../services/featureflags.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { AuthenticationService } from '../../services/authentication.service';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-semantic-list',
    templateUrl: './semantic-type-list.component.html',
    styleUrls: ["semanticTypes.less"],
    providers: [DataProfileService],
})

export class SemanticTypeListComponent extends SemanticBaseComponent implements OnInit, OnDestroy {

    @Output() selectedTypeChanged = new EventEmitter();
    sub: any;


    selectedType: any = null;
    semanticTypes: SemanticType[];
    simpleFilter: string = "";
    advancedFilter: string = "";
    semanticsTotal: number = 0;
    rowsPerPage: number = 25;
    currentPageNumber: number = 1;
    showSidePanel: boolean = true;
    private sidePanelOpen: boolean = false;
    sidePanelTab: string = 'detail';
    sidePanelStorageKey: string;
    navigationItemsSubs: Subscription[] = [];
    sortField: string;
    sortOrder: number;
    isExportInProgress: boolean = false;
    theDeleteCallback: Function;

    filterFields$: Observable<AdvancedFilterFieldType[]>;
    private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);

    readonly menuKey = '~menu';

    exportTooltip: string = "";

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
            Type: new FieldType("Lookup"),
            Category: "",
            ValueLoader: this.getFilterValues.bind(this, "status"),
            RemovePopulatedOperator: true
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
            Type: new FieldType("Lookup"),
            ValueLoader: this.getFilterValues.bind(this, "baseType"),
            Category: ""
        },
        {
            Name: 'Description',
            FriendlyName: 'Description',
            Type: new FieldType("Html"),
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
            Type: new FieldType("Lookup"),
            Category: "",
            ValueLoader: this.getFilterValues.bind(this, "matchType"),
            RemovePopulatedOperator: true
        },
        {
            Name: 'Source',
            FriendlyName: 'Semantic Source',
            Type: new FieldType("Lookup"),
            Category: "",
            ValueLoader: this.getFilterValues.bind(this, "source"),
            RemovePopulatedOperator: true
        },
        {
            Name: 'CreatedOn',
            FriendlyName: 'Date Created',
            Type: new FieldType("DateTime"),
            Category: ""
        },
        {
            Name: 'UpdatedOn',
            FriendlyName: 'Date Last Modified',
            Type: new FieldType("DateTime"),
            Category: ""
        }
    ]

    sourceValues: string[] = ["Built-In", "User-Defined"];
    statusValues: string[] = ["Certified", "Draft", "Under Review"];
    matchTypeValues: string[] = ["Advanced (JSON)", "List of Values", "Number", "Pattern in Data"];
    baseTypeValues: string[] = ["True/False (Boolean)", "Number (Double)", "Number (Long)", "String", "LocalDate", "LocalTime", "LocalDateTime", "OffsetDateTime", "ZonedDateTime",];

    advancedFilterMap = new Map([
        ["Built-In", "BuiltIn"],
        ["User-Defined", "UserDefined"],
        ["Under%20Review", "InReview"],
        ["Advanced%20\\(JSON\\)", "Advanced"],
        ["List%20of%20Values", "List"],
        ["Pattern%20in%20Data", "Pattern"],
        ["True%2FFalse%20\\(Boolean\\)", "Boolean"],
        ["Number%20\\(Double\\)", "Double"],
        ["Number%20\\(Long\\)", "Long"],
    ]);
    secondarySidePanel: string;
    resourceUid: any;
    secondarySidePanelOpen: boolean;
    showDelete: boolean = false;

    constructor(private route: ActivatedRoute,
        protected router: Router,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title,
        webAnalyticsService: WebAnalyticsService,
        private dataProfileService: DataProfileService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        private featureFlagService: FeatureFlagsService,
        private messagesService: MessagesObservableService,
        private authenticationService: AuthenticationService) {
        super(headerBreadcrumbService, settingsService, router, featureFlagService, secondaryNavService, webAnalyticsService);
        this.theDeleteCallback = this.deleteSemanticType.bind(this);

        this.exportTooltip = this.canExportRecords() ? $localize`Export to Excel` : $localize`Export not available for over ${this.maxExportRows} rows`
    }

    ngOnInit() {

        this.sidePanelStorageKey = 'SemanticTypes_' + CurrentResourceID;

        this.filterFields$ = this.filterFieldsSubject.asObservable();
        this.filterFieldsSubject.next(this.filterFieldList);
        this.filterFieldsSubject.complete();

        this.displayBreadCrumbs();
    }

    getData(selectedIndex: number = 0) {
        this.isLoading = true;
        this.dataProfileService.getSemanticTypes(this.currentPageNumber, this.rowsPerPage, this.simpleFilter, this.advancedFilter, this.sortField, this.sortOrder).subscribe((p) => {
            this.semanticTypes = p.items;
            this.semanticsTotal = p.total;
            if (this.semanticTypes && !this.selectedType || !p.items.some((x) => (x.uid === this.selectedType.uid))) {
                this.selectRow(this.semanticTypes[selectedIndex]);
            }

            this.semanticTypes.forEach((i) => {

                i[this.menuKey] = [
                    { title: $localize`Open` },
                    { title: $localize`Open in New Tab` },
                ];

                if (this.authenticationService.isAdmin && SemanticSource[i.source.toString()] === SemanticSource.UserDefined) {
                    if (!i.hasQualifiedAssets) {
                        i[this.menuKey].push({ title: $localize`Delete` });
                    } else {
                        i[this.menuKey].push({ title: $localize`Delete`, disabled: true, tooltip: $localize`This semantic type cannot be removed as it has already been used for classifying assets.` });
                    }
                }
            });

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
                case 'inreview':
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
        this.sortField = event.sortField;
        this.sortOrder = event.sortOrder;
        this.currentPageNumber = (event.first / event.rows) + 1;
        this.getData();
    }

    onFiltersLoaded() {
        this.getData();
    }

    advancedFiltersChanged($event: Filters) {
        this.advancedFilter = $event.filter;
        this.advancedFilterMap.forEach((value, key) => {
            this.advancedFilter = this.advancedFilter.replace(new RegExp(key, "g"), value);
        });
        this.getData();
    }

    onSimpleSearch(event: any) {
        this.getData();
    }

    selectRow(row: any) {
        this.secondarySidePanelOpen = false;
        this.selectedType = row;
        if (this.selectedType) {
            this.buildSecondaryNavigation(this.selectedType.uid, 0, 'SemanticType', null, null, this.displayBreadCrumbs.bind(this), null);
        }
        this.selectedTypeChanged.emit(row);
    }

    selectSemanticType(semanticType: SemanticType, newTab: boolean = false) {
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

        switch (key) {
            case $localize`Open`.toLowerCase():
                this.selectSemanticType(item);
                break;
            case $localize`Open in New Tab`.toLowerCase():
                this.selectSemanticType(item, true);
                break;
            case $localize`Delete`.toLowerCase():
                this.showDelete = true;
                break;
        }
    }

    displayBreadCrumbs() {
        this.sub = this.route.params.subscribe((params) => {
            this.headerBreadcrumbService.getFolderTitle('#SemanticTypes').then((res) => {
                this.folderTitle = res;
                this.setBrowserTitle(this.titleService, res);
                this.area = res;
                this.headerBreadcrumbService.clearBreadcrumbs();
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(res, SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT));

                this.headerBreadcrumbService.getFolderIcon(res).subscribe((icon) => {
                    this.secondaryNavService.setCurrentArea(res, icon, StringConstants.Section_SemanticTypes);
                });
            });

        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }

    getFilterValues(params: LookupValuesAPIParameters, lookupType: string): Observable<LookupValuesAPIModel> {
        if (params === "source") {
            const values = this.sourceValues.filter((s) => s.toLowerCase().indexOf(params.filter?.toLowerCase() ?? "") !== -1);
            return of({
                items: values,
                count: values.length
            });
        }

        if (params === "status") {
            const values = this.statusValues.filter((s) => s.toLowerCase().indexOf(params.filter?.toLowerCase() ?? "") !== -1);
            return of({
                items: values,
                count: values.length
            });
        }

        if (params === "matchType") {
            const values = this.matchTypeValues.filter((s) => s.toLowerCase().indexOf(params.filter?.toLowerCase() ?? "") !== -1);
            return of({
                items: values,
                count: values.length
            });
        }

        if (params === "baseType") {
            const values = this.baseTypeValues.filter((s) => s.toLowerCase().indexOf(params.filter?.toLowerCase() ?? "") !== -1);
            return of({
                items: values,
                count: values.length
            });
        }
    }

    canExportRecords() {
        return this.semanticsTotal <= this.maxExportRows;
    }

    export() {
        this.isExportInProgress = true;
        this.dataProfileService.getSemanticTypes(1, this.maxExportRows, this.simpleFilter, this.advancedFilter, this.sortField, this.sortOrder, true, () => { this.isExportInProgress = false; });
    }

    getBaseTypeText(baseType: string) {
        return SemanticType.getBaseTypeText(baseType);
    }

    handleSecondarySidePanelLinkClicked(event: any) {
        this.secondarySidePanelOpen = true;
        if (event && event.resourceUid) {
            this.secondarySidePanel = "user";
            this.resourceUid = event.resourceUid;
        } else {
            this.secondarySidePanel = "status";
        }
    }

    deleteSemanticType(item: SemanticType) {
        this.dataProfileService.deleteSemanticType(item.qualifier)
            .subscribe(
                (result) => {
                    this.showMessageForResult(this.messagesService, result, $localize`Semantic Type successfully deleted`);
                    this.showDelete = false;
                    if (result.type !== 'error') {
                        let currentIndex = this.semanticTypes.findIndex((s) => s.uid === this.selectedType.uid);
                        let nextRow = currentIndex === this.semanticsTotal - 1 ? this.semanticsTotal - 2 : currentIndex;
                        this.getData(nextRow);
                    }
                }
            );
    }
}
