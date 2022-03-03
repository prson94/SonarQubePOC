import { of as observableOf, Subject, Subscription } from "rxjs";
import { debounceTime, map, distinctUntilChanged, delay, mergeMap } from "rxjs/operators";
import {
    Component,
    Input,
    OnChanges,
    SimpleChange,
    ViewChild,
    ChangeDetectionStrategy,
    ChangeDetectorRef,

    OnDestroy,
    EventEmitter,
    Output
} from "@angular/core";
import { LazyLoadEvent } from "primeng/api";
import { Table } from "primeng/table";
import { ActivatedRoute, Router } from "@angular/router";
import {
    GridColumn,
    GridField,
    GridFilterColumn,
    GridScoreAllocation
} from "../../models/grid-definition.model";
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
import { HeaderBreadcrumbService } from "../../services/header-breadcrumb.service";
import { Breadcrumb } from "../../models/breadcrumb.model";
import { LocalStorageKey } from "../../enums/general.enum";
import { LocalStorageHelper } from "../../static/localstorage-helper";

export interface OnPageEvent {
    first: number;
    rows: number;
}

export interface NumberOfItemsToViewByAssetType {
    [key: string]: number;
}

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
    @Input() rowID: string = 'ObjectID';
    @Input() gridObject: AssetGridObject;
    @Output() selectedChange = new EventEmitter();
    @Output() isLoadingChange = new EventEmitter();
    @Output() isDefinitionLoadedChange = new EventEmitter();

    @Input() titlePostfix: string = ''; // added to end of header title.
    @Input() rowsPerPage: number = 25;
    @ViewChild('dt', { static: false }) dt: Table;
    @ViewChild('dynamicEditor', { static: false }) dynamicEditor: AssetEditorComponent;

    showEditButton: boolean = true;
    showDeleteButton: boolean = true;
    showAddButton: boolean = true;
    showCustomExport: boolean = false;
    isEditing: boolean = false;
    isMenuOpen: boolean = false;
    showArtifactDetails: boolean = false;
    showCertificationStatus: boolean = false;
    certificationStatusIndex: string = null;
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
    readonly excludedLinkColumnTypes = [
        'Tag',
        'OwnershipLookup',
        'Boolean'
    ];

    selected: any = null;
    itemUrl: string;

    readonly menuKey = '~menu';
    baseMenuItems: any[] = [
        { title: "Open" },
        { title: "Open in New Tab" },
    ];

    public simpleSearch = new Subject<any>();
    private assetSearchSub: Subscription;

    isExportInProgress = false;
    statusHasColor: boolean;

    isDebugMode: boolean = false;
    initialLoadInterval: any;

    get globalFilterFields(): string[] {
        return this.columns.map(c => c.datafield);
    }

    constructor(
        private headerBreadcrumbService: HeaderBreadcrumbService,
        private headerActionsService: HeaderActionsService,
        public stateService: StateService,
        private permissionsService: PermissionsService,
        protected settingsService: CompanySettingsService,
        private router: Router,
        private gridDefinitionService: GridDefinitionService,
        private changeDetectorRef: ChangeDetectorRef,
        private assetService: AssetService,
        private route: ActivatedRoute
    ) {
        super(settingsService);

        var me = this;
        this.route.queryParams.subscribe((params) => {
            if (params["debug"]) {
                this.isDebugMode = true;
            }
        });

        const subscription = this.simpleSearch.pipe(
            map(event => event.target.value),
            debounceTime(1000),
            distinctUntilChanged(),
            mergeMap(
                search => observableOf(search).pipe(delay(500))
            )
        )
            .subscribe(
                data => {
                    this.doSimpleSearch(me.dt, me.isLoading);
                }
            );
    }

    ngOnInit() {
        this.headerBreadcrumbService.breadcrumbIsSetToStorage.pipe().subscribe(() => {
            this.setRowsPerPage();
        });
    }

    onPage(event: OnPageEvent): void {
        this.setNumberOfItemsToViewByAssetTypeToLocalStorage(event.rows);
    }

    setNumberOfItemsToViewByAssetTypeToLocalStorage(numberOfRows: number): void {
        let numberOfItemsToViewByAssetType: NumberOfItemsToViewByAssetType;
        let folderTitle: string = this.getFolderTitleFromBreadcrumbsInStorage();

        if (LocalStorageHelper.isLocalStorageKeyExist(LocalStorageKey.NumberOfItemsToViewByAssetType)) {
            numberOfItemsToViewByAssetType = JSON.parse(localStorage.getItem(LocalStorageKey.NumberOfItemsToViewByAssetType));
        } else {
            numberOfItemsToViewByAssetType = {}
        }

        numberOfItemsToViewByAssetType[folderTitle] = numberOfRows;
        localStorage.setItem(LocalStorageKey.NumberOfItemsToViewByAssetType, JSON.stringify(numberOfItemsToViewByAssetType));
    }

    getFolderTitleFromBreadcrumbsInStorage(): string {
        let breadcrumb: Breadcrumb[] = this.headerBreadcrumbService.getBreadcrumbsFromStorage();
        return breadcrumb[0].text;
    }

    setRowsPerPage() {
        let folderTitle: string = this.getFolderTitleFromBreadcrumbsInStorage();
        if (LocalStorageHelper.isLocalStorageKeyExist(LocalStorageKey.NumberOfItemsToViewByAssetType)) {
            let numberOfItemsToViewByAssetType = JSON.parse(localStorage.getItem(LocalStorageKey.NumberOfItemsToViewByAssetType));
            if (numberOfItemsToViewByAssetType.hasOwnProperty(folderTitle)) {
                this.rowsPerPage = numberOfItemsToViewByAssetType[folderTitle];
            }
        }
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

    selectRow(row: any) {
        this.selected = row;
        this.selectedChange.emit(row);
    }

    clickMenuItem(event: any, item: any) {
        let key = event.value.toLowerCase();

        if (key === 'open') {
            this.selectArtifact(item);
        } else if (key === 'open in new tab') {
            this.selectArtifact(item, true);
        } else if (key === 'edit') {
            this.onEdit(item);
        } else if (key === 'delete') {
            this.onDelete(item);
        }
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
    }

    load() {
        this
            .loadPermissions(this.permissionsService, this.gridObject.ObjectType, this.gridObject.ID)
            .then(() => this.changeDetectorRef.markForCheck());

        this.getFieldsDefinition();

        if (this.gridObject.AutoDisplayDescription) {
            this.toggleArtifactDetail();
        }
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
            result => {
                let statusField;

                this.columns = result.Columns.filter(x => x.datafield != 'Name');
                this.filtercolumns = result.FilterColumns;
                this.fields = result.Fields;
                this.topLevelFilters = result.TopLevelFilterColumns;
                this.scoreAllocations = result.ScoreAllocations;
                this.hasProfiling = result.HasProfiling;
                this.hasProfilingChange.emit(this.hasProfiling);

                statusField = this.fields.find(x => x.apiName != null && x.apiName.toLowerCase() == "status");

                if (statusField != null) {
                    this.showCertificationStatus = true;
                    this.certificationStatusIndex = statusField.apiName;
                }

                if (result.Columns && result.Columns.length == 0) {
                    this.hasNoListableColumns = true;
                }
                else {
                    this.hasNoListableColumns = false;

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
        return this.fields.find(x => x.name == oldname).apiName;
    }

    getParams() {
        let autoDisplayParentSetting = this.gridObject.AutoDisplayParent === null ? true : this.gridObject.AutoDisplayParent;
        var params = new V2ApiFilters();
        params._includeParent = this.gridObject.ObjectType == StringConstants.ObjectArtifactType ? autoDisplayParentSetting : true;
        params._loadPermissionDetails = true;
        params._pageSize = this.rowsPerPage;
        params._pageNum = this.stateService.artifactTypeFilters.currentPageNumber + 1;
        params._listColorsAsJSON = true;
        params._includeProfilingCheck = true;

        if (this.stateService.artifactTypeFilters.sortField) {
            params._order = this.getFieldAPINameByOldName(this.stateService.artifactTypeFilters.sortField);
            params.useTypeLevelDefaultSorts = false;
        }
        else {
            params.useTypeLevelDefaultSorts = true;
            delete params['_order'];
        }

        if (this.stateService.artifactTypeFilters.sortOrder != SortOrder.None)
            params._direction = this.stateService.artifactTypeFilters.sortOrder == SortOrder.Ascending ? "asc" : "desc";
        else {
            delete params['_direction'];
        }

        if (this.stateService.artifactTypeFilters.simpleTextFilter && this.stateService.artifactTypeFilters.simpleTextFilter.length > 0) {
            params._simpleFilter = encodeURIComponent(this.stateService.artifactTypeFilters.simpleTextFilter);
        }
        else {
            delete params['_simpleFilter'];
        }

        if (this.stateService.artifactTypeFilters.filters && this.stateService.artifactTypeFilters.filters.length > 0) {
            let expressions: string[] = [];
            this.stateService.artifactTypeFilters.filters.forEach(f => {
                expressions.push(f.getAsV2ApiFilter(this.filtercolumns));
            });
            params._filter = expressions.join(' and ');
        }
        else {
            delete params['_filter'];
        }

        if (this.stateService.artifactTypeFilters.relationships && this.stateService.artifactTypeFilters.relationships.length > 0) {
            let expressions: string[] = [];
            this.stateService.artifactTypeFilters.relationships.forEach(f => {
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

        return params;
    }

    getData(autoSelect: boolean = true) {
        this.isLoading = true;
        this.isLoadingChange.emit(true);
        if (this.assetSearchSub) {
            this.assetSearchSub.unsubscribe();
        }
        this.assetSearchSub = this.assetService.getAssets(this.gridObject.AssetTypeUID, this.getParams(), true)
            .pipe(debounceTime(200))
            .subscribe(res => {
                this.items = res.items;
                let hasScoring = this.scoreAllocations && this.scoreAllocations.length > 0;

                this.items.forEach((i) => {

                    i[this.menuKey] = [
                        { title: 'Open' },
                        { title: 'Open in New Tab' },
                    ];

                    if (i.Permissions.ModifyAsset) {
                        i[this.menuKey].push({ title: 'Edit' });
                    }

                    if (i.Permissions.DeleteAsset) {
                        i[this.menuKey].push({ title: 'Delete' });
                    }

                    if (hasScoring) {
                        this.scoreAllocations.forEach((s) => {
                            i[s.Name + '_threshold'] = this.getThreshold(i[s.Name], s.LowerThreshold, s.UpperThreshold);
                        });
                    }

                    if (this.selected != null && autoSelect) {
                        if (i.AssetId === this.selected.AssetId) {
                            this.selectRow(i);
                        }
                    }

                });

                if (autoSelect) {
                    if (this.items && this.items.length > 0) {
                        this.selectRow(this.items[0]);
                    } else {
                        this.selectRow(null);
                    }
                }

                this.statusHasColor = this.items.filter(x => {
                    let foundColorToken = false;
                    for (var prop in x) {
                        if (Object.prototype.hasOwnProperty.call(x, prop) && prop.toLowerCase() == "status") {
                            if ((x[prop] + "").indexOf('"name":') > -1 && (x[prop] + "").indexOf('"color":') > -1) {

                                foundColorToken = true;
                            }

                        }

                    }
                    return foundColorToken;
                }).length > 0;


                this.totalRecords = res.total;
                if (this.initialTotalRecords == null) {
                    this.initialTotalRecords = res.total;
                }
                if (this.items && this.items.length > 0 && autoSelect) {
                    this.selected = this.items[0];
                }
                this.isLoading = false;
                this.isLoadingChange.emit(false);
                this.changeDetectorRef.markForCheck();
            },
                err => {
                    this.isLoading = false;
                    this.isLoadingChange.emit(false);
                    this.items = [];
                    this.totalRecords = 0;
                    this.changeDetectorRef.markForCheck();
                });
    }



    getCertificationStatusColor(status: string) {
        status = status.toLowerCase().trim();
        if (this.statusHasColor != true) {
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

    getThreshold(value: string, lower: number, upper: number): string {
        if (value == null || value.length < 1)
            return '';
        if (value.indexOf('%') > -1) {
            value = value.replace('%', '');
        }
        if (isNaN(+value))
            return '';

        let v = +value;

        if (v <= lower)
            return 'poor';
        else if (v > lower && v <= upper)
            return 'average';
        else
            return 'good';

    }

    closeEditor() {
        this.showEditor = false;
        this.showEditorChange.emit(false);
    }

    add() {
        this.selected = null;
        this.showEditor = true;
        this.selectedChange.emit(null);

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
                'Filtered ' + this.gridObject.Name + ' List',
                () => { this.isExportInProgress = false; }
            );
    }

    downloadCustomExcel(option: AssetTypeExportTemplate) {
        var params = JSON.parse(JSON.stringify(this.getParams()));
        params['_exporttemplateuid'] = option.Uid;

        this.assetService.downloadAssetsExcel(this.gridObject.AssetTypeUID, params, 'Filtered ' + this.gridObject.Name + ' List');
    }

    customExport() {
        //show the custom export screen        
        this.showCustomExport = !this.showCustomExport;
    }

    saveItem($event) {
        this.isEditing = false;
        if ($event.item.Uid) this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was edited                
        if ($event && $event.addAnother) {
            this.add();
            this.getData(false);
        }
        else if ($event && $event.action === 'new') {
            var newUrl = '/asset/' + $event.assetUid;
            this.router.navigateByUrl(newUrl);
        }
        else {
            this.getData();
            this.isLoading = false;
            this.isLoadingChange.emit(false);
            this.showEditor = false;
            this.showEditorChange.emit(false);
            this.changeDetectorRef.markForCheck();
        }

        this.changeDetectorRef.markForCheck();
    }

    selectArtifact(artifact, newTab: boolean = false) {

        this.assetService.getUIDetailsForAssetUID(artifact.AssetUid)
            .subscribe(res => {
                if (this.gridObject.ObjectType == StringConstants.ObjectArtifactType) {
                    this.itemUrl = SiteUrlHelpers.getObjectUrl('Artifact', res.ObjectId, this.gridObject.ID);
                }
                else if (this.gridObject.ObjectType == StringConstants.ObjectRuleType) {
                    this.itemUrl = SiteUrlHelpers.getObjectUrl('Rule', res.ObjectId, this.gridObject.ID);
                }
                else {
                    console.warn("onRightClick => Invalid object type");
                }
                if (newTab) {
                    window.open(this.itemUrl, '_blank');
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
        this.stateService.artifactTypeFilters.sortField = event.sortField == undefined ? "" : event.sortField;
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
            .subscribe(res => {
                if (this.gridObject.ObjectType == StringConstants.ObjectArtifactType) {
                    this.itemUrl = SiteUrlHelpers.getObjectUrl('Artifact', res.ObjectId, this.gridObject.ID);
                }
                else if (this.gridObject.ObjectType == StringConstants.ObjectRuleType) {
                    this.itemUrl = SiteUrlHelpers.getObjectUrl('Rule', res.ObjectId, this.gridObject.ID);
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

    private toggleArtifactDetail() {
        this.showArtifactDetails = !this.showArtifactDetails;
    }

    private onEdit(item) {
        this.selected = item;
        this.showEditor = true;
        this.showEditorChange.emit(true);
        this.changeDetectorRef.markForCheck();
    }

    private onDelete(item) {
        this.deleteName = item['Path'].slice(1, -1);
        this.selected = item;
        this.showDelete = true;
        this.changeDetectorRef.markForCheck();
    }

    private newAdvancedFilters: Filters;
    public advancedFiltersChanged($event) {
        this.newAdvancedFilters = $event;
        this.stateService.artifactTypeFilters.currentPageNumber = 0;
        if (this.dt) {
            this.dt.first = 0;
        }
        this.getData();
    }

    public onSimpleSearch($event) {
        this.getData();
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
