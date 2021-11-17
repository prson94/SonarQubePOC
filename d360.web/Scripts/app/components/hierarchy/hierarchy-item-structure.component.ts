import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { AssetTypeClass, AssetTypeLevelApiModel } from '../../models/asset.model';
import { ActivatedRoute, Router } from '@angular/router';
import { AssetTypeService } from '../../services/asset-type.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { StringConstants } from '../../static/string-constants';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { AssetService } from '../../services/asset.service';
import { PermissionsService } from '../../services/permissions.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { Title } from '@angular/platform-browser';
import { SecondaryNavCurrentObject, SecondaryNavItem } from '../../models/secondaryNav.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { TreeNode } from 'primeng/api';
import { GridColumn, GridField, GridScoreAllocation } from '../../models/grid-definition.model';
import { HeaderActionsService } from '../../services/header-actions.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { TreeTable } from 'primeng/treetable';
import { V2ApiFilters } from '../../models/asset-search.model';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { Filters } from '../assets-grid/advanced-filtering/advanced-filtering.models';
import { forkJoin, Observable, Subscription } from 'rxjs';
import { DataProfileService } from '../../services/dataprofile.service';
import { CompanySettingsService } from '../../services/settings.service';
import { ChangeDetectorRef } from '@angular/core';
import { AssetEditorComponent } from '../shared/asset-editor/asset-editor.component';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-hierarchy-item-structure',
    providers: [
        AssetTypeService,
        GridDefinitionService,
        PermissionsService,
        AssetService,
        WebAnalyticsService,
        DataProfileService,
    ],
    templateUrl: 'hierarchy-item-structure.component.html',
    styleUrls: ['hierarchy-item-structure.component.less']
})

export class HierarchyItemStructureComponent extends BaseComponent implements OnInit, OnDestroy {

    rowsPerPage: number = 25;

    assetTypeClass: AssetTypeClass;
    assetTypeUid: string;
    objectTypeId: number;
    object: string;
    assetType: any;
    type: string;
    navFolderName: string;
    showDiagram: boolean = false;

    levels: AssetTypeLevelApiModel[] = [];
    maxLevelAllowed: number = 1;
    hierarchy: any[] = [];

    rowID: string = 'AssetUid';
    routeSub: any;
    currentAreaNameSub: any;
    filterTimer: any;

    currentAreaName: string;
    selectedParentId: number;
    treeNodeArray: TreeNode[] = [];
    selected: TreeNode;

    columns: GridColumn[] = [];
    fields: GridField[] = [];
    scoreAllocations: GridScoreAllocation[] = [];

    searchValue: string = "";
    showEditor: boolean;
    showDelete: boolean;
    selectedLevel: number = 0;
    filterColumns: string[] = ['Path'];
    totalRecords: number = 0;
    totalRecordsFiltered: number = 0;
    linkColumnIndex: number = -1;
    readonly excludedLinkColumnTypes = [
        'Tag',
        'OwnershipLookup',
        'Boolean'
    ];

    @ViewChild("treeTable", { static: false }) treeTable: TreeTable;
    @ViewChild("inputBox", { static: false }) filterText: any;
    @ViewChild('dynamicEditor', { static: false }) dynamicEditor: AssetEditorComponent;

    simpleFilterValue: string = '';
    areAllExpanded: boolean = false;
    loadNodesSub: Subscription;

    sidePanelOpen: boolean = false;
    sidePanelLoading: boolean = false;
    sidePanelTab: string;
    sidePanelStorageKey: string;

    hasProfiling: boolean = false;
    dataProfile: any;
    private dataProfileList: any[];

    readonly menuKey: string = '~menu';
    baseMenuItems: any[] = [
        { title: "Open" },
        { title: "Open in New Tab" },
    ];

    constructor(
        private assetService: AssetService,
        private assetTypeService: AssetTypeService,
        private dataProfileService: DataProfileService,
        protected gridDefinitionService: GridDefinitionService,
        private headerActionsService: HeaderActionsService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        protected titleService: Title,
        protected secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        webAnalyticsService: WebAnalyticsService,
        private route: ActivatedRoute,
        private router: Router,
        private changeDetectorRef: ChangeDetectorRef
    ) {
        super(settingsService);

        this.webAnalyticsService = webAnalyticsService;
        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {
        this.type = this.route.parent.snapshot.data.type;

        switch (this.type) {
            case SiteUrlHelpers.SITE_URL_MODEL_ROOT:
                this.assetTypeClass = AssetTypeClass.Model;
                this.objectType = StringConstants.ObjectTaxonomyType;
                this.object = StringConstants.ObjectTaxonomy;
                this.objectName = 'Model';
                this.navFolderName = '#Models';
                this.showDiagram = true;
                break;
            case SiteUrlHelpers.SITE_URL_POLICY_ROOT:
                this.assetTypeClass = AssetTypeClass.Policy;
                this.objectType = StringConstants.ObjectPolicyType;
                this.objectName = 'Policy';
                this.object = StringConstants.ObjectPolicy;
                this.navFolderName = '#Policy';
                this.showDiagram = false;
                break;
        }

        this.sidePanelStorageKey = 'list_' + AssetTypeClass[this.assetTypeClass] + '_' + CurrentResourceID;

        this.routeSub = this.route.params.subscribe((params) => {
            this.objectTypeId = +params['typeId'];
            this.assetTypeUid = params['uid'];
            let uriParams: any = {};
            let useUid: boolean = false;

            const obs = new Observable((observer) => {
                if (this.assetTypeUid) {
                    this.assetTypeService.getAssetTypeObjectAndID(this.assetTypeUid).subscribe((response) => {
                        this.objectTypeId = response.ObjectID;
                        observer.next();
                    });
                }
                else {
                    observer.next();
                }
            });

            obs.subscribe((r) => {
                uriParams.obj = this.objectType;
                uriParams.objId = this.objectTypeId;
                uriParams.includelevels = "true";
                uriParams.includedashboardflag = "true";
                this.logAction("open", this.objectType, this.objectTypeId);

                this.assetTypeService.getAssetTypes(uriParams).subscribe((result) => {
                    this.assetType = result[0];
                    this.assetTypeUid = result[0].uid;
                    this.uid = this.assetTypeUid;

                    this.levels = result[0].Levels;
                    this.maxLevelAllowed = result[0].HierarchyMaximumDepth;
                    this.load();
                });
            });
        });
    }

    ngOnDestroy() {
        if (this.loadNodesSub) {
            this.loadNodesSub.unsubscribe();
        }
    }

    selectAsset(event: any) {
        this.selected = event;

        if (this.selected && this.selected.data && this.selected.data.HasProfiling) {
            this.sidePanelLoading = true;
            let startDate = new Date();
            startDate.setDate(-367);
            this.dataProfileService.getDataProfiles(this.selected.data.AssetUid, startDate).subscribe(
                (r) => {
                    if (r && r.items && r.items.length > 0) {
                        this.dataProfileList = r.items;
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
    }

    get panelApplies(): boolean {
        if (this.selected == null || this.selected.data == null || this.sidePanelTab === 'detail') {
            return true;
        }
        if (this.selected != null && this.selected.data != null && this.sidePanelTab === 'dataprofile') {
            return this.selected.data.HasProfiling;
        }
    }


    clickMenuItem(event: any, item: any) {
        let key = event.value.toLowerCase();

        if (key === 'open') {
            this.showHierarchy(item.data);
        } else if (key === 'open in new tab') {
            this.showHierarchy(item.data, true);
        } else if (key === 'edit') {
            this.selectAsset(item);
            this.showEditor = true;
        } else if (key === 'delete') {
            this.selectAsset(item);
            this.showDelete = true;
        } else if (key === 'add child') {
            this.showAdd(item.data.Level, item.data.AssetUid);

        }
    }


    load() {
        this.setObjectInfo(this.objectType, this.objectTypeId);
        this.setCommonSecondaryNavTabs(true);
        this.currentAreaNameSub = this.headerBreadcrumbService
            .getAreaName(this.objectType, this.objectTypeId)
            .subscribe((result) => { this.currentAreaName = result });

        this.getFieldsDefinition();
        this.loadPermissions(this.permissionsService, this.objectType, this.objectTypeId);
        this.setObjectInfo(this.objectType, this.objectTypeId);
        this.headerBreadcrumbService.setCurrentObjectInfo(this.objectType, this.objectTypeId);

        this.searchValue = "";
        this.buildNav();
    }

    buildNav() {
        this.headerBreadcrumbService.getFolderTitle(this.navFolderName).then((res) => {
            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.currentAreaName ? this.currentAreaName : res, `${this.type}/${SiteUrlHelpers.SITE_URL_HIERARCHY_CLASSIFICATION}`));
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.assetType.Name, SiteUrlHelpers.getAssetTypeUrl(this.objectType, this.assetTypeUid), undefined, this.objectType, this.assetType.ID, undefined, undefined, true));

            this.headerBreadcrumbService.getAssetFolderIcon(this.objectType, this.objectTypeId, this.currentAreaName ? this.currentAreaName : res)
                .subscribe((icon) => {
                    this.secondaryNavService.setCurrentArea(this.assetType.Name, icon, this.objectName);
                    this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject(this.objectType, this.assetType.ID, this.assetType.Name, null, true, null, this.assetType.AssetTypeUID));
                    this.setCommonSecondaryNavTabs(true, false, this.assetType.HasDashboards);

                    if (this.showDiagram) {
                        this.secondaryNavService.showItem(new SecondaryNavItem('Diagram', 'modeldiagram', ['fa-sitemap'], `/sidebar/visualization/diagram/${this.objectID}`, null, 7))
                    }

                    if (this.auditSidebar) {
                        this.auditSidebar.url = `/sidebar/audit/${this.assetType.uid}`;
                    }

                    this.secondaryNavService.showHeader(true);
                });

            this.setBrowserTitle(this.titleService, this.assetType.Name);
        });
    }

    private getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.objectTypeId, this.objectType).subscribe(
            (result) => {
                this.scoreAllocations = result.ScoreAllocations;
                this.columns = result.Columns;
                this.fields = result.Fields;
                var filterfields = this.fields.filter(function (item) { return item.apiName && item.name.startsWith("Field") });
                this.filterColumns = this.filterColumns.concat(filterfields.map(({ name }) => name));

                for (let i = 0; i < this.columns.length; i++) {
                    if (this.excludedLinkColumnTypes.findIndex((e) => e === (this.columns[i] as any).fieldType) === -1) {
                        this.linkColumnIndex = i;
                        break;
                    }
                }
            }
        );
    }

    private buildTreeNodeArray(hierarchies: any[], levelNumber: number, Parent?: string): TreeNode[] {
        let rootNodes = hierarchies.filter((x) => (Parent !== undefined ? x.ParentAssetUid === Parent : !x.ParentAssetUid));

        if (rootNodes.length === 0) {
            return null;
        }

        let res: TreeNode[] = [];


        for (let root of rootNodes) {
            let isExpanded = this.expandedNodes.indexOf(root.AssetUid) !== -1 || this.areAllExpanded;
            root.Level = levelNumber;

            root[this.menuKey] = [
                { title: 'Open' },
                { title: 'Open in New Tab' },
            ];

            if (this.displayChildAdd(levelNumber) && this.hasAddAssetPermissions()) {
                root[this.menuKey].push({ title: 'Add Child' });
            }

            if (root.Permissions.ModifyAsset) {
                root[this.menuKey].push({ title: 'Edit' });
            }

            let children = (this.buildTreeNodeArray(hierarchies, levelNumber + 1, root.AssetUid));

            if (root.Permissions.DeleteAsset && (!children || children?.length === 0)) {
                root[this.menuKey].push({ title: 'Delete' });
            }

            res.push({
                key: root.AssetUid,
                label: root.Path,
                expanded: isExpanded,
                data: root,
                children
            });
        }
        return res;
    }

    private buildScoreAllocationThresholds() {
        if (this.scoreAllocations && this.scoreAllocations.length > 0) {
            if (this.hierarchy) {
                this.hierarchy.forEach((i) => {
                    this.scoreAllocations.forEach((s) => {
                        var field = this.fields.find(f => f.apiName == s.Name);
                        if (field) {
                            i[field.apiName + '_threshold'] = this.getThreshold(i[field.apiName], s.LowerThreshold, s.UpperThreshold);
                        }
                    });
                });
            }
        }
    }

    public onDeleted() {
        this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was removed        
        this.deleteSelectedTreeNode(this.selected.data.AssetUid);
        this.hierarchy = this.hierarchy.filter((x) => x.AssetUid !== this.selected.data.AssetUid);
        this.treeNodeArray = this.buildTreeNodeArray(this.hierarchy, 1);

        this.selected = null;
        this.selectedLevel = null;
        this.selectedParentId = null;
        this.showDelete = false;
        this.isLoading = false;
    }

    private deleteSelectedTreeNode(id: number): TreeNode {
        let nodes: TreeNode[] = [];

        // add root nodes
        for (let i = 0; i < this.treeNodeArray.length; i++) {
            if (this.treeNodeArray[i].data.AssetUid && this.treeNodeArray[i].data.AssetUid === id) {
                this.treeNodeArray.splice(i, 1);
                return
            }
            nodes.push(this.treeNodeArray[i]);
        }

        //do a breadth first search for the given treenode
        if (nodes.length == 0) {
            return;
        }

        let node = nodes[0];

        while (node) {
            if (node.data.AssetUid && node.data.AssetUid === id) {
                return node;
            }

            //push children
            if (node.children) {
                for (let i = 0; i < node.children.length; i++) {
                    if (node.children[i].data.AssetUid && node.children[i].data.AssetUid == id) {
                        node.children.splice(i, 1);
                        return
                    }
                    nodes.push(node.children[i]);
                }
            }

            //remove this node
            nodes.splice(0, 1);

            if (nodes.length == 0) {
                return null;
            }
            node = nodes[0];
        }
    }

    private save($event) {
        if ($event && $event.addAnother) {
            this.showAdd(this.selectedLevel, this.selectedParentId);
            this.loadNodes(false);
        }
        else if ($event && $event.action === 'new') {
            var newUrl = '/asset/' + $event.assetUid;
            this.router.navigateByUrl(newUrl);
        }
        else {
            this.showEditor = false;
            this.selected = null;
            this.selectedLevel = null;
            this.selectedParentId = null;
            this.loadNodes();
            this.headerActionsService.emitFavoritesChange();
            this.isLoading = false;
        }
        this.changeDetectorRef.markForCheck();

    }

    private closeEditor() {
        this.showEditor = false;
        this.selected = null;
        this.selectedLevel = null;
        this.selectedParentId = null;

    }

    private exportExcel(level: number) {
        var params = new V2ApiFilters();
        params._onlyListableFields = false;
        params._direction = this.treeTable._sortOrder == 1 ? 'ASC' : 'DESC';
        if (this.treeTable._sortField != undefined) {
            var field = this.columns.filter((f) => f.datafield === this.treeTable._sortField)[0];
            params._order = field["apiName"];
        }
        else {
            params.useTypeLevelDefaultSorts = true;
            delete params._order;
        }

        if (this.simpleFilterValue) {
            params["_simpleFilter"] = "*" + this.simpleFilterValue;
            this.areAllExpanded = true;
        }

        if (this.newAdvancedFilters && this.newAdvancedFilters.filter) {
            params["_filter"] = this.newAdvancedFilters.filter;
            this.areAllExpanded = true;
        }

        params["isForTreeGrid"] = true;

        params._isHierachyItem = true;
        this.isLoading = true;
        this.assetService.downloadAssetsExcel(this.assetTypeUid, params, 'Filtered ' + this.assetType.Name, () => { this.isLoading = false; });
    }

    private showAdd(level: number, parentId: number) {
        this.selectedParentId = parentId;
        this.selectedLevel = level;
        this.selected = null;
        this.showEditor = true;
        //reload dynamic editor if it already exists to trigger change detection
        if (this.dynamicEditor) {
            this.dynamicEditor.load();
        }
    }

    private displayChildAdd(level: number) {
        return (level < this.maxLevelAllowed);
    }

    setTreeNodeStyles(node) {
        if (!node.data) return null;

        let styles = {
            'font-weight': node.data.hasRelations ? 'bold' : 'normal',
        };
        return styles;
    }

    get assetTypeTitle(): string {
        if (this.levels == null) {
            return '(Level Unknown Item)';
        }

        if (!this.selected) {
            let thisLevel = this.levels.filter(x => x.Level == this.selectedLevel + 1);

            if (thisLevel && thisLevel.length > 0)
                return thisLevel[0].Name;
            else
                return `(Level ${this.selectedLevel + 1}) Item`;
        }

        let thisLevel = this.levels.filter(x => x.Level == this.selected.data.Level);

        if (thisLevel && thisLevel.length > 0) return thisLevel[0].Name;
        return `(Level ${this.selected.data.Level}) Item`;
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

    private showHierarchy(asset, newTab: boolean = false) {
        this.assetService.getUIDetailsForAssetUID(asset.AssetUid)
            .subscribe((res) => {
                let url = SiteUrlHelpers.getObjectUrl(this.object, res.ObjectId, this.objectTypeId);
                if (newTab) {
                    window.open(url, '_blank');
                } else {
                    this.router.navigateByUrl(url);
                }
            });
    }

    private expandNodes() {
        if (this.treeTable.filters["global"]) { // only expand if global filter populated.
            this.totalRecordsFiltered = 0;
            this.totalRecordsFiltered = this.treeTable.filteredNodes ? this.treeTable.filteredNodes.length : 0;
            this.expandChildNodes(this.treeTable.filteredNodes, this.treeTable.globalFilterFields, this.treeTable.filters["global"].value);
        }
    }

    private expandChildNodes(nodes: TreeNode[], fields: string[], search: string) {
        nodes.forEach((node) => {
            var match = false;
            fields.forEach((field) => { if (node.data[field] && String(node.data[field]).toLowerCase().includes(search.toLowerCase())) { match = true; } }); //check each of the global filterfields for filter value
            if (!match) { // if we haven't found a match expand the node and check children.
                node.expanded = true;
                if (node.children && node.children.length > 0) {
                    this.totalRecordsFiltered = this.totalRecordsFiltered + node.children.length;
                    this.expandChildNodes(node.children, fields, search);
                }
            }
            else { // if matched then count number of child and futher child
                if (node.children && node.children.length > 0) {
                    this.totalRecordsFiltered = this.totalRecordsFiltered + node.children.length;
                    this.expandChildNodesCount(node.children);
                }
            }
        }
        );
    }

    private expandChildNodesCount(nodes: TreeNode[]) {
        nodes.forEach((node) => {
            if (node.children && node.children.length > 0) {
                this.totalRecordsFiltered = this.totalRecordsFiltered + node.children.length;
                this.expandChildNodesCount(node.children);
            }
        }
        );
    }

    onSort() {
        setTimeout(() => this.loadNodes(), 20);
    }

    loadNodes(autoSelect: boolean = true) {
        this.expandedNodes = this.treeState;
        this.areAllExpanded = false;
        if (this.assetTypeUid) {
            this.isLoading = true;

            if (this.loadNodesSub) {
                this.loadNodesSub.unsubscribe();
            }

            let uriParams: any = {
                _pageSize: 50000,
                _includeParent: "true",
                _pageNum: 1,
                _loadPermissionDetails: "true",
                _listColorsAsJSON: "true",
                isForTreeGrid: true
            };

            if (this.treeTable) {
                uriParams._direction = this.treeTable._sortOrder === 1 ? 'ASC' : 'DESC';
            }
            if (this.treeTable && this.treeTable._sortField && this.treeTable._sortField !== "") {
                var field = this.columns.filter((f) => f.datafield === this.treeTable._sortField)[0];
                uriParams._order = field["apiName"];
            }
            else {
                uriParams.useTypeLevelDefaultSorts = true;
                delete uriParams._order;
            }

            if (this.simpleFilterValue) {
                uriParams["_simpleFilter"] = "*" + this.simpleFilterValue;
                this.areAllExpanded = true;
            }

            if (this.newAdvancedFilters && this.newAdvancedFilters.filter) {
                uriParams["_filter"] = this.newAdvancedFilters.filter;
                this.areAllExpanded = true;
            }

            this.loadNodesSub = this.assetService.getAssets(this.assetTypeUid, uriParams, true).subscribe((result) => {
                this.totalRecords += result.total;
                this.hierarchy = result.items;

                if (this.hierarchy.length !== 0) {
                    this.treeNodeArray = this.buildTreeNodeArray(this.hierarchy, 1, undefined);
                    if (autoSelect) {
                        if (this.treeNodeArray.length > 0) {
                            this.selectAsset(this.treeNodeArray[0]);
                        } else {
                            this.selectAsset(null);
                        }
                    }
                    this.buildScoreAllocationThresholds();
                }
                else {
                    this.treeNodeArray = [];
                    this.selectAsset(null);
                }

                this.isLoading = false;
            });
        }
    }

    canExportRecords() {
        var isfilter = false;

        if (this.treeTable != null) {
            if (this.treeTable.filters != null) {
                if (this.treeTable.filters["global"]) {
                    isfilter = true;
                }
            }
        }

        if (isfilter) {
            return this.totalRecordsFiltered <= this.maxExportRows;
        }
        else {
            return this.totalRecords <= this.maxExportRows;
        }
    }

    newAdvancedFilters: Filters;
    advancedFiltersChanged($event) {
        this.newAdvancedFilters = $event;
        this.loadNodes();
    }

    onFiltersLoaded() {
        this.loadNodes();
    }

    expandedNodes: string[] = [];
    nodeExpanded($event) {
        this.expandedNodes.push($event.node.key);
        this.saveTreeState();
    }
    nodeCollapsed($event) {
        var idx = this.expandedNodes.indexOf($event.node.key);
        if (idx !== -1) {
            this.expandedNodes.splice(idx, 1);
        }
        this.saveTreeState();
    }

    saveTreeState() {
        localStorage.setItem(this.getNodesStateKey, JSON.stringify(this.expandedNodes));
    }

    get treeState(): string[] {
        var loadedData = localStorage.getItem(this.getNodesStateKey);
        if (!loadedData) {
            return [];
        }
        else {
            return JSON.parse(loadedData) as string[];
        }
    }

    get getNodesStateKey() {
        return "nodeState_" + this.assetTypeUid;
    }

    getAssetPath() {
        if (this.selected && this.selected?.data?.Path) {
            let path = this.selected.data.Path as string;
            path = path.substring(1, path.length - 1);
            path = path.split("].[").join(` > `);
            return this.assetType?.Path + ' > ' + path;
        }
        return this.assetType?.Path;
    }
}
