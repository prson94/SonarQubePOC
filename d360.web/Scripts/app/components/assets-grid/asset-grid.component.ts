import { of as observableOf, Subject, Subscription } from "rxjs";
import { debounceTime, delay, distinctUntilChanged, map, mergeMap, takeUntil } from "rxjs/operators";
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    ElementRef,
    EventEmitter,
    HostListener,
    Input,
    OnChanges,
    OnDestroy,
    Output,
    QueryList,
    SimpleChange,
    ViewChild,
    ViewChildren
} from "@angular/core";
import { LazyLoadEvent } from "primeng/api";
import { Table } from "primeng/table";
import { ActivatedRoute, Router } from "@angular/router";
import { GridColumn, GridField, GridFilterColumn, GridScoreAllocation } from "../../models/grid-definition.model";
import { GridDefinitionService } from "../../services/grid-definition.service";
import { ArtifactService } from "../../services/artifacts.service";
import { AssetService } from "../../services/asset.service";
import { PermissionsService } from "../../services/permissions.service";
import { StateService } from "../../services/state.service";
import { HeaderActionsService } from "../../services/header-actions.service";
import { AssetTypeExportTemplate } from "../../models/artifact-type.model";
import { BaseComponent } from "../shared/base.component";
import { SiteUrlHelpers } from "../../static/site-url-helpers";
import { StringConstants } from "../../static/string-constants";
import { ObjectDetailService } from "../../services/object-detail.service";
import * as _ from "lodash";
import { V2ApiFilters } from "../../models/asset-search.model";
import { SortOrder } from "../../models/enums.model";
import { AssetGridObject } from "./asset-grid.model";
import { Filters } from "./advanced-filtering/advanced-filtering.models";
import { CompanySettingsService } from "../../services/settings.service";
import { AssetEditorComponent } from "../shared/asset-editor/asset-editor.component";
import { AppConstants } from "../../static/constants";
import { NumberOfRowsByCategoryService } from "../../services/number-of-rows-by-category.service";
import { FeatureFlags, FeatureFlagsService } from "../../services/featureflags.service";
import { PopupMenu } from "../shared/controls/popup-menu/popup-menu.component";
import { LinkClickInterceptor } from "../../services/href-click-service";
import { AssetTypeApiModel } from "../../models/asset.model";
import { LocalStorageKey } from "../../enums/localstorage.enum";
import { AssetGridCustomExportComponent } from "./asset-grid-custom-export.component";

@Component({
    selector: "d3s-asset-grid",
    providers: [GridDefinitionService, ArtifactService, PermissionsService, ObjectDetailService, AssetService],
    templateUrl: "./asset-grid.component.html",
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["asset-grid.less"],
    host: {
        "(document:click)": "clickedOutside()",
    },
})

export class AssetGridComponent extends BaseComponent implements OnChanges, OnDestroy {
    @Input() assetTypeApiModel: AssetTypeApiModel;
    @Input() rowID: string = 'ObjectID';
    @Input() gridObject: AssetGridObject;
    @Output() selectedChange = new EventEmitter();
    @Output() isLoadingChange = new EventEmitter();
    @Output() isDefinitionLoadedChange = new EventEmitter();

    @Input() titlePostfix: string = ''; // added to end of header title.
    @Input() rowsPerPage: number = AppConstants.DEFAULT_ROWS_PER_PAGE;

    @ViewChild('dt', { static: false }) dt: Table;
    @ViewChild('dynamicEditor', { static: false }) dynamicEditor: AssetEditorComponent;
	@ViewChild('gridCustomExport', { static: false }) gridCustomExport: AssetGridCustomExportComponent;
    @ViewChildren('tableRow') tableRows: QueryList<ElementRef>;

    @HostListener('document:keydown.arrowup', ['$event'])
    @HostListener('document:keydown.arrowdown', ['$event'])
    onArrowKeysDownHandler($event: KeyboardEvent) {
        $event.preventDefault();
        const selectedRow = this.tableRows.toArray().find((elRef) => {
            return elRef.nativeElement.classList.contains('p-highlight');
        });
        if (selectedRow && document.activeElement !== selectedRow.nativeElement) {
            selectedRow.nativeElement.dispatchEvent(
                new KeyboardEvent($event.type, { key: $event.key })
            );
        }
    }

    showEditButton: boolean = true;
    showDeleteButton: boolean = true;
    showAddButton: boolean = true;
    showCustomExport: boolean = false;
    isEditing: boolean = false;
    isMenuOpen: boolean = false;
	isContainsSearchDefault: boolean = false;
    deleteName: string = 'Artifact';
    previousEvent: LazyLoadEvent;
    totalRecords: number;
    initialTotalRecords: number = null;

    searchValue: string = "";

    searchDelayMilliSeconds: number = 500;
    error: any;
    items: any[];
    columns: GridColumn[] = [];
    fields: GridField[] = [];
    filtercolumns: GridFilterColumn[] = [];
    topLevelFilters: GridFilterColumn[] = [];
    scoreAllocations: GridScoreAllocation[] = [];
    hasProfiling: boolean = false;
    @Output() hasProfilingChange = new EventEmitter<boolean>();
    @Output() showEditorChange = new EventEmitter<boolean>();

    showDelete: boolean = false;
    showEditor: boolean = false;
    isLoading: boolean = false;
    isDefinitionLoaded: boolean = false;
    areFiltersLoaded: boolean = false;
    hasNoListableColumns: boolean = false;
    linkColumnIndex: number = -1;
	flexGrow: number = 0;
    readonly excludedLinkColumnTypes = [
        'Tag',
        'OwnershipLookup',
        'Boolean'
    ];

    selected: any = null;
    itemUrl: string;

    readonly menuKey = '~menu';
    baseMenuItems: any[] = [
        { title: $localize`Open` },
        { title: $localize`Open in New Tab` },
    ];

    hideDescLabel = $localize`Hide Description`;
    showDescLabel = $localize`Show Description`;

    public simpleSearch = new Subject<any>();
    private assetSearchSub: Subscription;

    isExportInProgress = false;

    isDebugMode: boolean = false;
    initialLoadInterval: any;
    destroy = new Subject<void>();
	isDescriptionVisible: boolean = false;

    get exportTooltip(): string {
        return this.canExportRecords() ? $localize`Export to Excel` : $localize`Export not available for over ${this.maxExportRows} rows`;
    }

    get globalFilterFields(): string[] {
        return this.columns.map((c) => c.datafield);
    }

    get assetEditorTitle(): string {
        return this.selected ? $localize`Edit Asset` : $localize`Create New Asset`;
    }

    constructor(
        public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
        private headerActionsService: HeaderActionsService,
        public stateService: StateService,
        private permissionsService: PermissionsService,
        protected settingsService: CompanySettingsService,
        private router: Router,
        private gridDefinitionService: GridDefinitionService,
        private changeDetectorRef: ChangeDetectorRef,
        private assetService: AssetService,
        private route: ActivatedRoute,
        private featureFlagService: FeatureFlagsService,
		private linkClickInterceptor: LinkClickInterceptor
    ) {
        super(settingsService);

        var me = this;
        this.route.queryParams.subscribe((params) => {
            if (params["debug"]) {
                this.isDebugMode = true;
            }
        });

        const subscription = this.simpleSearch.pipe(
            map((event) => event.target.value),
            debounceTime(1000),
            distinctUntilChanged(),
            mergeMap(
                (search) => observableOf(search).pipe(delay(500))
            )
        ).subscribe((data) => {
			this.doSimpleSearch(me.dt, me.isLoading);
		});
		
		this.isContainsSearchDefault = this.featureFlagService.flags[FeatureFlags.ContainsSearchDefaultUiFlag];
	}

    ngOnInit() {
        this.setRowsPerPage();
        this.numberOfRowsByCategoryService.defineNumberOfRows();
    }

    setRowsPerPage(): void {
        this.numberOfRowsByCategoryService.rowsPerPage.pipe(
            takeUntil(this.destroy)
        ).subscribe((rowsPerPage) => {
            this.rowsPerPage = rowsPerPage['Main'];
        });
    }

    canExportRecords() {
        return this.totalRecords <= this.maxExportRows;
    }

    onFiltersLoaded() {
        this.areFiltersLoaded = true;
        this.showAssetListPage();
        this.changeDetectorRef.markForCheck();
    }

    showAssetListPage() {
        this.isDefinitionLoaded = true;
        this.isDefinitionLoadedChange.emit(true);
        if (this.initialLoadInterval) {
            clearInterval(this.initialLoadInterval);
        }
        this.changeDetectorRef.markForCheck();
    }

    selectRow(row: any, forceRefresh: boolean = false) {
        this.selected = row;
        this.selectedChange.emit({ row, forceRefresh });
    }

    clickMenuItem(menuItem: any, item: any) {
        const key = menuItem.value.toLowerCase();
		const event = menuItem.event;
		if (key === $localize`View Information`.toLowerCase()) {
			event['from-context-method'] = 'info';
			this.selectArtifact(event, item);
		} else if (key === $localize`Open`.toLowerCase()) {
			event['from-context-method'] = 'open';
			this.selectArtifact(event, item);
        } else if (key === $localize`Open in New Tab`.toLowerCase()) {
			event['from-context-method'] = 'new-tab';
			this.selectArtifact(event, item);
        } else if (key === $localize`Edit`.toLowerCase()) {
            this.onEdit(item);
        } else if (key === $localize`Delete`.toLowerCase()) {
            this.onDelete(item);
        }
    }

	positionContextMenu(
		$event: MouseEvent, container: HTMLElement, floatMenu: PopupMenu, assetGridTools: HTMLElement
	): void {
		if (!assetGridTools.contains(<Node>$event.target) && !this.isElementLink(<HTMLElement>$event.target)) {
			container.style.top = `${$event['layerY']}px`;
			container.style.left = `${$event['layerX']}px`;
			floatMenu.toggle($event);
			$event.preventDefault();
		}
	}
	
	private isElementLink(element: HTMLElement): boolean {
		while (element.parentElement) {
			if (element.tagName === 'A') {return true;}
			element = element.parentElement;
		}
		return false;
	}

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['gridObject'] && this.gridObject != null) {
            this.load();
        }

        //clear out the filters if the artifacttype is different
        this.stateService.resetArtifactTypeFilterIfRequired(this.gridObject.ID);
    }

    ngOnDestroy() {
        if (this.assetSearchSub) {
            this.assetSearchSub.unsubscribe();
        }

        this.destroy.next();
        this.destroy.complete();
    }

    load() {
        const descriptionVisibilitySavedState = localStorage.getItem(
            `${LocalStorageKey.IsAssetTypeDescriptionVisible}_${this.assetTypeApiModel.uid}`
        );

        if (descriptionVisibilitySavedState !== null) {
            this.isDescriptionVisible = JSON.parse(descriptionVisibilitySavedState);
        } else {
            this.isDescriptionVisible = this.assetTypeApiModel.IsDescriptionVisibleByDefault;
        }

        this
            .loadPermissions(this.permissionsService, this.gridObject.ObjectType, this.gridObject.ID)
            .then(() => this.changeDetectorRef.markForCheck());

        this.getFieldsDefinition();
    }

    setDescriptionVisibility(state: boolean): void {
        this.isDescriptionVisible = state;
        localStorage.setItem(
            `${LocalStorageKey.IsAssetTypeDescriptionVisible}_${this.assetTypeApiModel.uid}`,
            state.toString()
        );
    }

    public filterGridData(dt: Table) {
        this.isLoading = true;
        this.isLoadingChange.emit(true);
        this.stateService.artifactTypeFilters.currentPageNumber = 0;
        if (dt) {
            dt.reset();
        }
        this.getData();
    }

    resetFilters(dt: Table, val) {
        this.stateService.artifactTypeFilters.showSimpleFilter = val;
        this.stateService.artifactTypeFilters.simpleTextFilter = '';
        this.stateService.artifactTypeFilters.filters = [];
        this.stateService.artifactTypeFilters.relationships = [];
        this.stateService.artifactTypeFilters.owners = null;

        this.filterGridData(dt);
    }

    onDeleted() {
        this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was removed        
        this.getData();
        this.showDelete = false;
        this.changeDetectorRef.markForCheck();
    }

    getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.gridObject.ID, this.gridObject.ObjectType).subscribe(
            (result) => {
                this.columns = result.Columns.filter((x) => x.datafield !== 'Name');
                this.filtercolumns = result.FilterColumns;
                this.fields = result.Fields;
                this.topLevelFilters = result.TopLevelFilterColumns;
                this.scoreAllocations = result.ScoreAllocations;
                if (this.featureFlagService.flags[FeatureFlags.DataProfilingUiFlag]) {
                    this.hasProfiling = result.HasProfiling;
                    this.hasProfilingChange.emit(this.hasProfiling);
                }

                if (result.Columns && result.Columns.length === 0) {
                    this.hasNoListableColumns = true;
                }
                else {
					this.hasNoListableColumns = false;
					//If all columns have defined width, they must be allowed to grow to allow the table to go to 100% width
					this.flexGrow = this.columns.every((c) => c.columnWidth) ? 1 : 0;

                    for (let i = 0; i < this.columns.length; i++) {
                        if (this.excludedLinkColumnTypes.findIndex((e) => e === (this.columns[i] as any).fieldType) === -1) {
                            this.linkColumnIndex = i;
                            break;
                        }
                    }
                }

                this.initialLoadInterval = setTimeout(() => this.showAssetListPage(), 3000);
                this.changeDetectorRef.markForCheck();
            }
        );
    }

    getFieldAPINameByOldName(oldname: string) {
        return this.fields.find((x) => x.name === oldname).apiName;
    }

	_oldParamsJSON: string = '';
    getParams() {
        const autoDisplayParentSetting = this.gridObject.AutoDisplayParent === null ? true : this.gridObject.AutoDisplayParent;
        var params = new V2ApiFilters();
        params._includeParent = this.gridObject.ObjectType === StringConstants.ObjectArtifactType ? autoDisplayParentSetting : true;
        params._loadPermissionDetails = true;
        params._pageSize = this.rowsPerPage;
        params._pageNum = this.stateService.artifactTypeFilters.currentPageNumber + 1;
        params._listColorsAsJSON = true;
        params._includeProfilingCheck = true;
		params.usecachedfilters = true;

        if (this.stateService.artifactTypeFilters.sortField) {
            params._order = this.getFieldAPINameByOldName(this.stateService.artifactTypeFilters.sortField);
            params.useTypeLevelDefaultSorts = false;
        }
        else {
            params.useTypeLevelDefaultSorts = true;
            delete params['_order'];
        }

        if (this.stateService.artifactTypeFilters.sortOrder !== SortOrder.None)
            {params._direction = this.stateService.artifactTypeFilters.sortOrder === SortOrder.Ascending ? "asc" : "desc";}
        else {
            delete params['_direction'];
        }

        if (this.stateService.artifactTypeFilters.simpleTextFilter && this.stateService.artifactTypeFilters.simpleTextFilter.length > 0) {
            if (this.isContainsSearchDefault) {
				params._simpleFilter = encodeURIComponent(
					`*${this.stateService.artifactTypeFilters.simpleTextFilter}*`
				);
			} else {
				params._simpleFilter = encodeURIComponent(this.stateService.artifactTypeFilters.simpleTextFilter);
			}
        }
        else {
            delete params['_simpleFilter'];
        }

        if (this.stateService.artifactTypeFilters.filters && this.stateService.artifactTypeFilters.filters.length > 0) {
            const expressions: string[] = [];
            this.stateService.artifactTypeFilters.filters.forEach((f) => {
                expressions.push(f.getAsV2ApiFilter(this.filtercolumns));
            });
            params._filter = expressions.join(' and ');
        }
        else {
            delete params['_filter'];
        }

        if (this.stateService.artifactTypeFilters.relationships && this.stateService.artifactTypeFilters.relationships.length > 0) {
            const expressions: string[] = [];
            this.stateService.artifactTypeFilters.relationships.forEach((f) => {
                expressions.push(f.getAsV2ApiFilter());
            });
            if (expressions.length > 0) {
                params._relationFilter = expressions.join(' and ');
            }
            else {
                delete params['_relationFilter'];
            }
        }
        else {
            delete params['_relationFilter'];
        }

        if (this.stateService.artifactTypeFilters.owners) {
            var filter = this.stateService.artifactTypeFilters.owners.getAsV2ApiFilter();
            if (filter.length > 0) {
                params._ownedBy = filter;
            }
            else {
                delete params['_ownedBy'];
            }
        }
        else {
            delete params['_ownedBy'];
        }

        if (this.initialTotalRecords != null && this.initialTotalRecords < 1000) {
            params.usegraphforparent = false;
        }
        else {
            delete params['usegraphforparent'];
		}

        if (this.newAdvancedFilters) {
            this.newAdvancedFilters.applyFilters(params);
		}

		params._includeTotal = true;
		const paramsJson: string = params.countUpdateFilters();
		if (paramsJson === this._oldParamsJSON) {
			params._includeTotal = false;
		}

        return params;
    }

    getData(autoSelect: boolean = true, edit?: { keyFieldChanged: boolean }) {
        this.isLoading = true;
        this.isLoadingChange.emit(true);
        if (this.assetSearchSub) {
            this.assetSearchSub.unsubscribe();
		}
		const params = this.getParams();
		this.assetSearchSub = this.assetService.getAssets(this.gridObject.AssetTypeUID, params, true)
            .pipe(debounceTime(200))
			.subscribe((res) => {
				this._oldParamsJSON = params.countUpdateFilters();

                this.items = res.items;
                const hasScoring = this.scoreAllocations && this.scoreAllocations.length > 0;
				let isRowSelected = false;

                this.items.forEach((item) => {

                    item[this.menuKey] = [
						{ title: $localize`View Information` },
						{ title: $localize`Open` },
                        { title: $localize`Open in New Tab` },
                    ];

                    if (item.Permissions.ModifyAsset) {
                        item[this.menuKey].push({ title: $localize`Edit` });
                    }

                    if (item.Permissions.DeleteAsset) {
                        item[this.menuKey].push({ title: $localize`Delete` });
                    }

                    if (hasScoring) {
                        this.scoreAllocations.forEach((s) => {
                            item[s.Name + '_threshold'] = this.getThreshold(item[s.Name], s.LowerThreshold, s.UpperThreshold);
                        });
                    }

                    if (this.selected && autoSelect && edit && !edit.keyFieldChanged) {
						if (item.AssetId === this.selected.AssetId) {
                            this.selectRow(item, true);
							isRowSelected = true;
                        }
                    }

                });

                if (!this.showEditor && autoSelect && (!edit || !isRowSelected)) {
                    if (this.items && this.items.length > 0) {
                        this.selectRow(this.items[0]);
                    } else {
                        this.selectRow(null);
                    }
                }

				if (params._includeTotal) {
					this.totalRecords = res.total;
				}
                if (this.initialTotalRecords == null) {
                    this.initialTotalRecords = res.total;
                }
                this.isLoading = false;
                this.isLoadingChange.emit(false);
                this.changeDetectorRef.markForCheck();
            },
                (err) => {
                    this.isLoading = false;
                    this.isLoadingChange.emit(false);
                    this.items = [];
                    this.totalRecords = 0;
                    this.changeDetectorRef.markForCheck();
                });
    }    

    getThreshold(value: string, lower: number, upper: number): string {
        if (value == null || value.length < 1)
            {return '';}
        if (value.indexOf('%') > -1) {
            value = value.replace('%', '');
        }
        if (isNaN(+value))
            {return '';}

        const v = +value;

        if (v <= lower)
            {return 'poor';}
        else if (v > lower && v <= upper)
            {return 'average';}
        else
            {return 'good';}

    }

    closeEditor() {
        this.showEditor = false;
        this.showEditorChange.emit(false);
    }

    add() {
		this.selectRow(null);
        this.showEditor = true;
        //reload dynamic editor if it already exists to trigger change detection
        if (this.dynamicEditor) {
            this.dynamicEditor.load();
        }
    }

    export(listableOnly) {
        if (this.gridObject.HasCustomExportTemplates) {
            this.customExport();
            return;
        }

        this.isExportInProgress = true;
        this.assetService
            .downloadAssetsExcel(
                this.gridObject.AssetTypeUID,
                this.getParams(),
                $localize`Filtered ${this.gridObject.Name} List`,
                () => { this.isExportInProgress = false; }
            );
    }

    downloadCustomExcel(option: AssetTypeExportTemplate) {
        var params = JSON.parse(JSON.stringify(this.getParams()));
		params['_exporttemplateuid'] = option.Uid;

		this.gridCustomExport.setExportState(option, true);
		this.assetService.downloadAssetsExcel(this.gridObject.AssetTypeUID, params, $localize`Filtered ${this.gridObject.Name} List`, () => { this.gridCustomExport.setExportState(option, false); });
    }

    customExport() {
        //show the custom export screen        
        this.showCustomExport = !this.showCustomExport;
    }

    saveItem($event) {
        this.isEditing = false;
        if ($event.item.Uid) {this.headerActionsService.emitFavoritesChange();} // favorites need to be reloaded if an object was edited                
        if ($event && $event.addAnother) {
            this.add();
            this.getData(false);
        }
        else if ($event && $event.action === 'new') {
            var newUrl = '/asset/' + $event.assetUid;
            this.router.navigateByUrl(newUrl);
        }
        else {
            this.getData(true, { keyFieldChanged: $event.keyFieldChanged });
            this.isLoading = false;
            this.isLoadingChange.emit(false);
            this.showEditor = false;
            this.showEditorChange.emit(false);
            this.changeDetectorRef.markForCheck();
        }

        this.changeDetectorRef.markForCheck();
    }

    selectArtifact($event, artifact) {
        this.assetService.getUIDetailsForAssetUID(artifact.AssetUid)
            .subscribe((res) => {
                if (this.gridObject.ObjectType === StringConstants.ObjectArtifactType) {
					this.itemUrl = SiteUrlHelpers.getAssetUrl(artifact.AssetUid);
                }
                else if (this.gridObject.ObjectType === StringConstants.ObjectRuleType) {
					this.itemUrl = SiteUrlHelpers.getAssetUrl(artifact.AssetUid);
                }
                else {
                    console.warn("onRightClick => Invalid object type");
                }
				if ($event['from-context-method']) {
					this.linkClickInterceptor.sendEvent($event, {
						Values: [{
							TooltipContext: "Preview",
							TooltipID: res.ObjectId,
							TooltipType: "Artifact",
							Value: artifact.Name,
							assetTypeUid: artifact.AssetTypeUid,
							uid: artifact.AssetUid,
						}],
						DataType: 'Lookup'
					}, this.itemUrl);
				} else {
					this.router.navigateByUrl(this.itemUrl);
				}
            });

    }

    private loadArtifactsLazy(event: LazyLoadEvent) {
        //if its the same filter then no need to load same data 
        if (_.isEqual(event, this.previousEvent)) {
            return;
        }
        this.previousEvent = event;
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        //filters: FilterMetadata object having field as key and filter value, filter matchMode as value  
        this.stateService.artifactTypeFilters.sortOrder = event.sortOrder;
        this.stateService.artifactTypeFilters.sortField = event.sortField == null ? "" : event.sortField;
        this.rowsPerPage = event.rows;
        this.stateService.artifactTypeFilters.currentPageNumber = event.first / event.rows;
        this.getData();
    }

    private doSimpleSearch(dt: Table, isLoading: boolean) {

        if (isLoading) {
            return;
        }

        isLoading = true;
        if (dt) {
            dt.reset();
            this.previousEvent = null;
        }
    }

    protected onRightClick(event, rightMenu, artifact, grid) {
        var gridRect = grid.el.nativeElement.getBoundingClientRect();
        var itemRect = event.srcElement.getBoundingClientRect();

        this.isMenuOpen = true;

        rightMenu.style.top = (event.screenY - gridRect.top) + 'px';
        rightMenu.style.left = (event.offsetX) + 'px'; //correct

        this.assetService.getUIDetailsForAssetUID(artifact.AssetUid)
            .subscribe((res) => {
				if (this.gridObject.ObjectType === StringConstants.ObjectArtifactType) {
					this.itemUrl = SiteUrlHelpers.getAssetUrl(artifact.AssetUid);
				}
				else if (this.gridObject.ObjectType === StringConstants.ObjectRuleType) {
					this.itemUrl = SiteUrlHelpers.getAssetUrl(artifact.AssetUid);
				}
				else {
					console.warn("onRightClick => Invalid object type");
				}
            });

        return false;
    }

    clickedOutside() {
        if (this.isMenuOpen) {
            this.isMenuOpen = false;
        }
    }

    private onEdit(item) {
        this.selectRow(item);
        this.showEditor = true;
        this.showEditorChange.emit(true);
        this.changeDetectorRef.markForCheck();
    }

    private onDelete(item) {
        this.deleteName = item['Path'].slice(1, -1);
		this.selectRow(item);
        this.showDelete = true;
        this.changeDetectorRef.markForCheck();
    }

    private newAdvancedFilters: Filters;
    public advancedFiltersChanged($event) {
		this.newAdvancedFilters = $event;
		this.resetPageNumber();
        this.getData();
    }

    public onSimpleSearch($event) {
		this.resetPageNumber();
        this.getData();
    }

	resetPageNumber() {
		this.stateService.artifactTypeFilters.currentPageNumber = 0;
		if (this.dt) {
			this.dt.first = 0;
		}
	}

    public triggerEdit() {
        this.onEdit(this.selected);
    }

    getAssetPath() {
        var assetTypePath = this.gridObject?.Object === "Rule" ? this.gridObject?.Name : this.gridObject?.AssetTypePath;

        if (this.selected && this.selected.Path) {
            let path = this.selected.Path as string;
            path = path.substring(1, path.length - 1);
            path = path.split("].[").join(` > `);
            return assetTypePath + ' > ' + path;
        }

        return assetTypePath;
    }
}
