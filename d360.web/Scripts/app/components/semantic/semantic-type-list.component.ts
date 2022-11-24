import { ChangeDetectorRef, Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
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
import { FeatureFlags, FeatureFlagsService } from '../../services/featureflags.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { AuthenticationService } from '../../services/authentication.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { SidePanelService } from '../../services/side-panel.service';
import { IOutputData } from 'angular-split';

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
    sidePanelOpen: boolean = false;
    sidePanelTab: string = 'detail';
    sidePanelStorageKey: string;
    navigationItemsSubs: Subscription[] = [];
    sortField: string;
    sortOrder: number;
    isExportInProgress: boolean = false;
	isContainsSearchDefault: boolean = false;
	theDeleteCallback: Function;
    theDisableCallback: Function;

    filterFields$: Observable<AdvancedFilterFieldType[]>;
    private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);

    readonly menuKey = '~menu';

    filterFieldList: AdvancedFilterFieldType[] = [
        {
            Name: 'Name',
            FriendlyName: $localize`Name`,
            Type: new FieldType("Text"),
            Category: ""
        },
        {
            Name: 'Qualifier',
            FriendlyName: $localize`Qualifier`,
            Type: new FieldType("Text"),
            Category: ""
        },
        {
            Name: 'Status',
            FriendlyName: $localize`Status`,
            Type: new FieldType("Lookup"),
            Category: "",
            ValueLoader: this.getFilterValues.bind(this, "status"),
            RemovePopulatedOperator: true
        },
        {
            Name: 'Priority',
            FriendlyName: $localize`Priority`,
            Type: new FieldType("Number"),
            Category: ""
        },
        {
            Name: 'BaseType',
            FriendlyName: $localize`Base Type`,
            Type: new FieldType("Lookup"),
            ValueLoader: this.getFilterValues.bind(this, "baseType"),
            Category: ""
        },
        {
            Name: 'Description',
            FriendlyName: $localize`Description`,
            Type: new FieldType("Html"),
            Category: ""
        },
        {
            Name: 'Threshold',
            FriendlyName: $localize`Threshold`,
            Type: new FieldType("Number"),
            Category: ""
        },
        {
            Name: 'MatchType',
            FriendlyName: $localize`Match Type`,
            Type: new FieldType("Lookup"),
            Category: "",
            ValueLoader: this.getFilterValues.bind(this, "matchType"),
            RemovePopulatedOperator: true
        },
        {
            Name: 'Source',
            FriendlyName: $localize`Semantic Source`,
            Type: new FieldType("Lookup"),
            Category: "",
            ValueLoader: this.getFilterValues.bind(this, "source"),
            RemovePopulatedOperator: true
        }
    ];

    sourceValues: string[] = ["Built-In", "User-Defined"];
    statusValues: string[] = ["Certified", "Draft", "Under Review"];
    matchTypeValues: string[] = ["List of Values", "Pattern in Data", "Numbers", "Advanced (JSON)"];
    baseTypeValues: string[] = ["True/False (Boolean)", "Number (Double)", "Number (Long)", "String", "LocalDate", "LocalTime", "LocalDateTime", "OffsetDateTime", "ZonedDateTime",];

    advancedFilterMap = new Map([
        ["Built-In", "BuiltIn"],
        ["User-Defined", "UserDefined"],
        ["Under%20Review", "InReview"],
        ["Advanced%20\\(JSON\\)", "Advanced"],
        ["List%20of%20Values", "List"],
        ["Pattern%20in%20Data", "Pattern"],
        ["Numbers", "Number"],
        ["True%2FFalse%20\\(Boolean\\)", "Boolean"],
        ["Number%20\\(Double\\)", "Double"],
        ["Number%20\\(Long\\)", "Long"],
    ]);
    secondarySidePanel: string;
    resourceUid: any;
    secondarySidePanelOpen: boolean;
    showDelete: boolean = false;
    showEditor: boolean = false;
    showAddButton: boolean = false;
    isAdd: boolean = false;
    showDisableDialog: boolean = false;
    showDisabled: boolean = false;

    constructor(private route: ActivatedRoute,
        protected router: Router,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title,
        public sidePanelService: SidePanelService,
        webAnalyticsService: WebAnalyticsService,
        private dataProfileService: DataProfileService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        private featureFlagService: FeatureFlagsService,
        private messagesService: MessagesObservableService,
        private authenticationService: AuthenticationService,
        private headerActionsService: HeaderActionsService,
        private cdRef: ChangeDetectorRef) {
        super(headerBreadcrumbService, settingsService, router, featureFlagService, secondaryNavService, webAnalyticsService);
        this.theDeleteCallback = this.deleteSemanticType.bind(this);
        this.theDisableCallback = this.changeSemanticDisabledStatus.bind(this);
		this.isContainsSearchDefault = this.featureFlagService.flags[FeatureFlags.ContainsSearchDefaultUiFlag];
	}

    ngOnInit() {

        this.sidePanelStorageKey = 'SemanticTypes_' + CurrentResourceID;

        this.filterFields$ = this.filterFieldsSubject.asObservable();
        this.filterFieldsSubject.next(this.filterFieldList);
        this.filterFieldsSubject.complete();

        this.displayBreadCrumbs();
    }

    getData(selectedIndex: number = 0, autoSelect: boolean = true) {
        this.isLoading = true;        
		const simpleFilter = this.isContainsSearchDefault ? `*${this.simpleFilter}*` : this.simpleFilter;
		
        this.dataProfileService.getSemanticTypes(this.currentPageNumber, this.rowsPerPage, simpleFilter, this.advancedFilter, this.sortField, this.sortOrder, false, null, this.showDisabled).subscribe((p) => {
            this.semanticTypes = p.items;
            this.semanticsTotal = p.total;
            if (this.semanticTypes && !this.selectedType || !p.items.some((x) => (x.uid === this.selectedType.uid))) {
                this.selectRow(autoSelect ? this.semanticTypes[selectedIndex] : null);
            }

            this.semanticTypes.forEach((i) => {

                i[this.menuKey] = [
                    { title: $localize`Open` },
                    { title: $localize`Open in New Tab` },
                ];

                if (this.authenticationService.isAdmin) {
                    this.showAddButton = true;      

                    if (i.isDisabled) {
                        i[this.menuKey].push({ title: $localize`Edit`, disabled: true, tooltip: $localize`Built-In semantic types cannot be deleted.` });
                        i[this.menuKey].push({ title: $localize`Enable` });
                    } else {
						i[this.menuKey].push({ title: $localize`Edit` });
						if (SemanticSource[i.source.toString()] === SemanticSource.BuiltIn) {
							i[this.menuKey].push({ title: $localize`Disable`, disabled: true, tooltip: $localize`Built-In semantic types cannot be disabled.` });
						} else {
							i[this.menuKey].push({ title: $localize`Disable` });
						}   						
                    }

					if (SemanticSource[i.source.toString()] === SemanticSource.UserDefined) {						
                        if (!i.hasQualifiedAssets) {
                            i[this.menuKey].push({ title: $localize`Delete` });
                        } else {
                            i[this.menuKey].push({ title: $localize`Delete`, disabled: true, tooltip: $localize`This semantic type cannot be removed as it has already been used for classifying assets.` });
                        }
					} else if (SemanticSource[i.source.toString()] === SemanticSource.BuiltIn) {						
                        i[this.menuKey].push({ title: $localize`Delete`, disabled: true, tooltip: $localize`Built-In semantic types cannot be deleted.` });

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
			this.baseSemanticTypeUid = this.selectedType.uid;
			this.buildSecondaryNavigation({ assetUid: this.selectedType.uid, objectId: 0, objectType: 'SemanticType', buildBreadcrumbOverride: this.displayBreadCrumbs.bind(this) });
        }
        this.selectedTypeChanged.emit(row);
    }

    selectSemanticType(semanticType: SemanticType, newTab: boolean = false) {
        const url = `${SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT}/${semanticType.uid}`;
        if (url) {
            if (newTab) {
                window.open(url, '_blank');
            } else {
                this.router.navigateByUrl(url);
            }
        }
    }

    clickMenuItem(event: any, item: SemanticType) {
        const key = event.value.toLowerCase();

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
			case $localize`Disable`.toLowerCase():
			case $localize`Enable`.toLowerCase():
                this.showDisableDialog = true;
				break;
            case $localize`Edit`.toLowerCase():
                this.isAdd = false;
                this.showEditor = true;
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
		this.dataProfileService.getSemanticTypes(1, this.maxExportRows, this.simpleFilter, this.advancedFilter, this.sortField, this.sortOrder, true, () => { this.isExportInProgress = false; }, this.showDisabled);
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
                        const currentIndex = this.semanticTypes.findIndex((s) => s.uid === this.selectedType.uid);
                        const nextRow = currentIndex === this.semanticsTotal - 1 ? this.semanticsTotal - 2 : currentIndex;
                        this.getData(nextRow);
                    }
                }
            );
    }

    addSemantic() {
        this.isAdd = true;
        this.selectedType = null;
        this.showEditor = true;
    }

    saveSemantic($event) {
        if ($event && $event.addAnother) {
            this.addSemantic();
            this.getData(0, false);
        }
        else if ($event && $event.action.toLowerCase() === "new") {
            var newUrl = "/semantics/" + $event.item.uid;
            this.router.navigateByUrl(newUrl);
        }
        else {
            if ($event.item.uid) {
                this.headerActionsService.emitFavoritesChange();
            }
            this.getData();
            this.isLoading = false;
            this.showEditor = false;
            if (this.semanticTypes.some((x) => (x.uid === $event.item.uid))) {
                this.selectRow($event.item);
            }
        }

        this.cdRef.markForCheck();
    }

    changeSemanticDisabledStatus(item: SemanticType) {
        this.isLoading = true;
        this.dataProfileService.changeSemanticDisabledStatus(item.qualifier, !item.isDisabled)
            .subscribe((res) => {
                this.showDisableDialog = false;
                this.getData();

            },
                (err) => {
                this.showDisableDialog = false;
                this.isLoading = true;
            });
    }

	get disableModalTitle() {
		return this.selectedType && this.selectedType.isDisabled ? $localize`Enable Semantic Type` : $localize`Disable Semantic Type`;
	}
	get disableButtonText() {
		return this.selectedType && this.selectedType.isDisabled ? $localize`Enable` : $localize`Disable`;
	}
	
    getSidePanelWidth(): number {
        return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
    }

    getSidePanelMaxWidth(): number {
        return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
    }

    getSidePanelMinWidth(): number {
        return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
    }

    onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
        this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
    }
}
