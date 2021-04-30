import { Component, OnInit, ViewChild } from '@angular/core';
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

@Component({
    selector: 'd3s-hierarchy-item-structure',
    providers: [
        AssetTypeService,
        GridDefinitionService,
        PermissionsService,
        AssetService,
        WebAnalyticsService,
    ],
    templateUrl: 'hierarchy-item-structure.component.html'
})

export class HierarchyItemStructureComponent extends BaseComponent implements OnInit {

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

    @ViewChild("treeTable", { static: false }) treeTable: TreeTable;
    @ViewChild("inputBox", { static: false }) filterText: any;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private assetTypeService: AssetTypeService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        protected gridDefinitionService: GridDefinitionService,
        protected titleService: Title,
        private headerActionsService: HeaderActionsService,
        protected secondaryNavService: SecondaryNavService,
        private assetService: AssetService,
        webAnalyticsService: WebAnalyticsService
    ) {
        super();

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
        
        this.routeSub = this.route.params.subscribe((params) => {
            this.objectTypeId = +params['typeId'];
            this.assetTypeUid = params['uid'];
            let uriParams: any = {};

            if (this.assetTypeUid) {
                uriParams.assetTypeUid = this.assetTypeUid;
            } else {
                uriParams.obj = this.objectType;
                uriParams.objId = this.objectTypeId;
                this.logAction("open", this.objectType, this.objectTypeId);
            }
            uriParams.includelevels = "true";

            this.isLoading = true;
            this.assetTypeService.getAssetTypes(uriParams).subscribe((result) => {
                this.assetType = result[0];
                this.assetTypeUid = result[0].uid;
                this.levels = result[0].Levels;
                this.load();

                this.loadNodes();
            });
        });
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
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.assetType.Name, SiteUrlHelpers.getObjectUrl(this.objectType, this.assetType.ID), undefined, this.objectType, this.assetType.ID, undefined, undefined, true));

            this.headerBreadcrumbService.getAssetFolderIcon(this.objectType, this.objectTypeId, this.currentAreaName ? this.currentAreaName : res)
                .subscribe((icon) => {
                    this.secondaryNavService.setCurrentArea(this.assetType.Name, icon, this.objectName);
                    this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject(this.objectType, this.assetType.ID, this.assetType.Name, null, true, null, this.assetType.AssetTypeUID));
                    this.setCommonSecondaryNavTabs(true, false, this.assetType.HasDashboards);

                    if (this.showDiagram) {
                        this.secondaryNavService.showItem(new SecondaryNavItem('Diagram', 'modeldiagram', ['fa-sitemap'], `/sidebar/visualization/diagram/${this.objectID}`, null, 7))
                    }

                    if (this.auditSidebar) {
                        this.auditSidebar.url = `/sidebar/audit/${this.assetType.AssetTypeUID}`;
                    }

                    this.secondaryNavService.showHeader(true);
                });

            //this.loadHierarchy();
            this.setBrowserTitle(this.titleService, this.assetType.Name);
            this.isLoading = false;
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
            }
        );
    }

    private buildTreeNodeArray(hierarchies: any[], levelNumber: number, Parent?: string): TreeNode[] {
        let rootNodes = hierarchies.filter(x => (Parent != undefined ? x.ParentAssetUid == Parent : !x.ParentAssetUid));

        if (rootNodes.length == 0) return null;

        let res: TreeNode[] = [];

        for (let root of rootNodes) {
            root.Level = levelNumber;
            res.push({
                key: root.AssetUid,
                label: root.Path,
                expanded: false,
                data: root,
                children: (this.buildTreeNodeArray(hierarchies, levelNumber + 1, root.AssetUid))
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
                            i[field.name + '_threshold'] = this.getThreshold(i[field.name], s.LowerThreshold, s.UpperThreshold);
                        }
                    });
                });
            }
        }
    }

    public onDeleted() {
        this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was removed        
        this.deleteSelectedTreeNode(this.selected.data.AssetUid);
        this.hierarchy = this.hierarchy.filter(x => x.AssetUid != this.selected.data.AssetUid);
        this.treeNodeArray = this.buildTreeNodeArray(this.hierarchy, 1);

        this.selected = null;
        this.showDelete = false;
    }

    private deleteSelectedTreeNode(id: number): TreeNode {
        let nodes: TreeNode[] = [];

        // add root nodes
        for (let i = 0; i < this.treeNodeArray.length; i++) {
            if (this.treeNodeArray[i].data.AssetUid && this.treeNodeArray[i].data.AssetUid == id) {
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
            if (node.data.AssetUid && node.data.AssetUid == id) {
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

    private save(event) {
        this.loadNodes();
        this.headerActionsService.emitFavoritesChange();
        this.showEditor = false;
    }

    private closeEditor() {
        this.showEditor = false;
    }

    private exportExcel(level: number) {
        var params = new V2ApiFilters();
        params._onlyListableFields = false;
        params._direction = this.treeTable._sortOrder == 1 ? 'ASC' : 'DESC';
        if (this.treeTable._sortField != undefined) {
            params._order = this.treeTable._sortField;
        }
        else {
            params.useTypeLevelDefaultSorts = true;
            delete params._order;
        }
        if (this.filterText.nativeElement.value != '')
            params._simpleFilter = '*' + this.filterText.nativeElement.value;
        else
            delete params._simpleFilter;
        params._isHierachyItem = true;
        this.assetService.downloadAssetsExcel(this.assetTypeUid, params,'Filtered ' + this.assetType.Name);
    }

    private showAdd(level: number) {
        this.showEditor = true;
        this.selectedParentId = level == 0 ? undefined : this.selected ? this.selected.data.AssetUid : undefined;
        this.selectedLevel = level;
        this.selected = null;
    }

    setTreeNodeStyles(node) {
        if (!node.data) return null;

        let styles = {
            'font-weight': node.data.hasRelations ? 'bold' : 'normal',
        };
        return styles;
    }

    get assetTypeTitle(): string {
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

    private showHierarchy(id: number) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl(this.object, id, this.objectTypeId));
    }

    private expandNodes() {        
        if (this.treeTable.filters["global"]) { // only expand if global filter populated.
            this.totalRecordsFiltered = 0;
            console.log(this.treeTable.filteredNodes);
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

    loadNodes() {
        this.isLoading = true;

        if (this.assetTypeUid) {
            let uriParams: any = {
                _pageSize: 50000,
                _includeParent: "true",
                _pageNum: 1,
                _loadPermissionDetails: "true"
            };
            this.assetService.getAssets(this.assetTypeUid, uriParams).subscribe((result) => {
                this.totalRecords += result.total;
                this.buildScoreAllocationThresholds();
                this.hierarchy = result.items;
                this.treeNodeArray = this.buildTreeNodeArray(this.hierarchy, 1);
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
}