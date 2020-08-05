import { Component, OnInit, ViewChild } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { AssetTypeClass } from '../../models/asset.model';
import { ActivatedRoute, Router } from '@angular/router';
import { AssetTypeService } from '../../services/asset-type.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { StringConstants } from '../../static/string-constants';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { PermissionsService } from '../../services/permissions.service';
import { LevelsService } from '../../services/levels.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { Title } from '@angular/platform-browser';
import { SecondaryNavCurrentObject, SecondaryNavItem } from '../../models/secondaryNav.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { TreeNode } from 'primeng/api';
import { GridColumn, GridField, GridScoreAllocation } from '../../models/grid-definition.model';
import { ModelsService } from '../../services/models.service';
import { PoliciesService } from '../../services/policies.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { TreeTableModule, TreeTable } from 'primeng/treetable';
import { V2ApiFilters } from '../../models/asset-search.model';

@Component({
    selector: 'd3s-hierarchy-item-structure',
    providers: [
        AssetTypeService,
        LevelsService,
        GridDefinitionService,
        ModelsService,
        PoliciesService,
        PermissionsService,
    ],
    templateUrl: 'hierarchy-item-structure.component.html'
})

export class HierarchyItemStructureComponent extends BaseComponent implements OnInit {
    assetTypeClass: AssetTypeClass;
    assetTypeUid: string;
    objectTypeId: number;
    object: string;
    assetType: any;
    type: string;
    navFolderName: string;
    showDiagram: boolean = false;

    levels: any[] = [];
    hierarchy: any[] = [];
    

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
    filterColumns: string[] = ['DisplayValue'];

    @ViewChild("treeTable", { static: false }) treeTable: TreeTable;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private assetTypeService: AssetTypeService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        protected levelsService: LevelsService,
        protected gridDefinitionService: GridDefinitionService,
        protected titleService: Title,
        private modelsService: ModelsService,
        private policiesService: PoliciesService,
        private headerActionsService: HeaderActionsService,
        protected secondaryNavService: SecondaryNavService
    ) {
        super();

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

        this.routeSub = this.route.params.subscribe(params => {
            this.objectTypeId = +params['typeId'];
            this.assetTypeUid = params['uid'];

            if (this.assetTypeUid) {
                this.assetTypeService.getAssetTypeObjectAndID(this.assetTypeUid).subscribe(res => {
                    this.isLoading = true;
                    this.objectTypeId = res.ObjectID
                    this.load();
                });
            } else {
                this.isLoading = true;
                this.load();
            }
        });
    }


    load() {
        this.setObjectInfo(this.objectType, this.objectTypeId);
        this.setCommonSecondaryNavTabs(true);
        this.currentAreaNameSub = this.headerBreadcrumbService
            .getAreaName(this.objectType, this.objectTypeId)
            .subscribe(result => { this.currentAreaName = result });  
        
            this.getFieldsDefinition();        
            this.loadPermissions(this.permissionsService, this.objectType, this.objectTypeId);
            this.setObjectInfo(this.objectType, this.objectTypeId);
            this.headerBreadcrumbService.setCurrentObjectInfo(this.objectType, this.objectTypeId);            
            
            switch (this.assetTypeClass) {
                case AssetTypeClass.Model:
                    this.isLoading = true;
                    this.modelsService.getModel(this.objectTypeId)
                        .subscribe(result => {
                            this.searchValue = "";
                            this.assetType = result;
                            this.buildNav();
                        });
                    break;
                case AssetTypeClass.Policy:
                    this.isLoading = true;
                    this.policiesService.getPolicyType(this.objectTypeId)
                        .subscribe(result => {
                            this.searchValue = "";
                            this.assetType = result;
                            this.buildNav();
                        });
                    break;         
            }
       
        this.levelsService.getObjectLevels(this.objectTypeId, this.objectType)
            .subscribe(result => {
                this.levels = result;
            });
    }


    buildNav() {
        this.headerBreadcrumbService.getFolderTitle(this.navFolderName).then((res) => {
            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.currentAreaName ? this.currentAreaName : res, `${this.type}/${SiteUrlHelpers.SITE_URL_HIERARCHY_CLASSIFICATION}`));
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.assetType.Name, SiteUrlHelpers.getObjectUrl(this.objectType, this.assetType.ID), undefined, this.objectType, this.assetType.ID, undefined, undefined, true));

            this.headerBreadcrumbService.getAssetFolderIcon(this.objectType, this.objectTypeId, this.currentAreaName ? this.currentAreaName : res)
                .subscribe(icon => {
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

            this.loadHierarchy();
            this.setBrowserTitle(this.titleService, this.assetType.Name);
            this.isLoading = false;
        });
    }

    private getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.objectTypeId, this.objectType).subscribe(
            result => {
                this.scoreAllocations = result.ScoreAllocations;
                this.columns = result.Columns;                
                this.fields = result.Fields;                 
                var filterfields = this.fields.filter(function (item) { return item.apiName && item.name.startsWith("Field") });
                this.filterColumns = this.filterColumns.concat(filterfields.map(({ name }) => name));     
            }
        );
    }

    private loadHierarchy() {
        switch (this.assetTypeClass) {
            case AssetTypeClass.Model:
                this.modelsService.getModelHierarchy(this.objectTypeId, true, true).subscribe(
                    result => {
                        this.hierarchy = result;

                        this.buildScoreAllocationThresholds();
                        this.treeNodeArray = this.buildTreeNodeArray(this.hierarchy, 1);
                        this.isLoading = false;
                    }
                );
                break;
            case AssetTypeClass.Policy:
                this.policiesService.getPolicies(this.objectTypeId, true).subscribe(
                    result => {
                        this.hierarchy = result;
                        this.buildScoreAllocationThresholds();
                        this.treeNodeArray = this.buildTreeNodeArray(this.hierarchy, 1);
                        this.isLoading = false;
                    }
                );
                break;
        }
    }

    private buildTreeNodeArray(hierarchies: any[], levelNumber: number, Parent?: number): TreeNode[] {
        let rootNodes = hierarchies.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));

        if (rootNodes.length == 0) return null;

        let res: TreeNode[] = [];

        for (let root of rootNodes) {
            root.Level = levelNumber;
            res.push({
                label: root.DisplayValue,
                expanded: false,
                data: root,
                children: (this.buildTreeNodeArray(hierarchies, levelNumber + 1, root.ID))
            });
        }
        return res;
    }

    private buildScoreAllocationThresholds() {
        if (this.scoreAllocations && this.scoreAllocations.length > 0) {
            if (this.hierarchy) {
                this.hierarchy.forEach(i => {
                    this.scoreAllocations.forEach(s => {
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
        this.deleteSelectedTreeNode(this.selected.data.ID);
        this.hierarchy = this.hierarchy.filter(x => x.ID != this.selected.data.ID);
        this.treeNodeArray = this.buildTreeNodeArray(this.hierarchy, 1);

        this.selected = null;
        this.showDelete = false;
    }

    private deleteSelectedTreeNode(id: number): TreeNode {
        let nodes: TreeNode[] = [];

        // add root nodes
        for (var i = 0; i < this.treeNodeArray.length; i++) {
            if (this.treeNodeArray[i].data.ID && this.treeNodeArray[i].data.ID == id) {
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
            if (node.data.ID && node.data.ID == id) {
                return node;
            }

            //push children
            if (node.children) {
                for (var i = 0; i < node.children.length; i++) {
                    if (node.children[i].data.ID && node.children[i].data.ID == id) {
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
        this.isLoading = true;
        this.loadHierarchy();
        this.headerActionsService.emitFavoritesChange();
        this.showEditor = false;
    }

    private closeEditor() {
        this.showEditor = false;
    }

    private exportExcel(level: number) {
        var params = new V2ApiFilters();
        switch (this.assetTypeClass) {
            case AssetTypeClass.Model:
                params._isHierachyItem = 'Model';
                this.policiesService.getHierarchyExcel(this.assetType.AssetTypeUID, this.assetType.Name, params, 'Model', true);
                break;
            case AssetTypeClass.Policy:
                params._isHierachyItem = 'Policy';
                this.policiesService.getHierarchyExcel(this.assetType.AssetTypeUID, this.assetType.Name, params,'Policy', true);
                break;
        }
    }

    private showAdd(level: number) {
        this.showEditor = true;
        this.selectedParentId = level == 0 ? undefined : this.selected ? this.selected.data.ID : undefined;
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
            this.expandChildNodes(this.treeTable.filteredNodes, this.treeTable.globalFilterFields, this.treeTable.filters["global"].value);
        }
    }

    private expandChildNodes(nodes: TreeNode[], fields: string[], search: string) {
        nodes.forEach((node) => {
            var match = false;
            fields.forEach(field => { if (node.data[field] && String(node.data[field]).toLowerCase().includes(search.toLowerCase())) { match = true; } }); //check each of the global filterfields for filter value
            if (!match) { // if we haven't found a match expand the node and check children.
                node.expanded = true;
                if (node.children && node.children.length > 0) {
                    this.expandChildNodes(node.children, fields, search);
                }
            }            
        }
        );
    }
}