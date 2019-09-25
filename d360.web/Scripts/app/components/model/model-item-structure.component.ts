import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { ModelsService } from '../../services/models.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Model, ModelHierarchy } from '../../models/model.model';
import { TreeNode } from 'primeng/primeng';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import { LevelsService } from '../../services/levels.service';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { GridColumn, GridField } from '../../models/grid-definition.model';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { MessagesObservableService } from '../../services/messages-observable.service';

@Component({
    selector: 'd3s-model-item-structure',
    providers: [GridDefinitionService, ModelsService, PermissionsService, LevelsService],
    templateUrl: './model-item-structure.component.html'
})

export class ModelItemStructureComponent extends BaseComponent implements OnInit, OnDestroy {
    routeParamsSubscription: any;
    private currentAreaNameSubscription: any;
    private currentAreaName: string;
    rightSub: any;

    model: Model;
    modelHierarchy: ModelHierarchy[] = [];
    levels: any[] = [];

    modelId: number;
    selectedParentID: number;
    treeNodeArray: TreeNode[] = [];
    selected: TreeNode;

    columns: GridColumn[] = [];
    fields: GridField[] = [];

    searchValue: string = '';
    showEditor: boolean;
    showDelete: boolean;
    selectedLevel: number = 0;

    @ViewChild("treeTable") treeTable: any;
    unfilteredTreeNode: TreeNode[] = [];

    constructor(
        private headerActionsService: HeaderActionsService,
        private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService,
        protected modelsService: ModelsService,
        protected titleService: Title,
        protected messagesService: MessagesObservableService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        protected levelsService: LevelsService,
        protected gridDefinitionService: GridDefinitionService
    ) {
        super();

        this.rightSidebarService = rightSidebarService;
        router.events.subscribe(
            (value) => {
                this.showEditor = false;
                this.filter(null);
            }
        );
    }

    private filterQ: any;
    filter(event) {
        if (event) {
            this.searchValue = event.target.value;
        }
        window.clearTimeout(this.filterQ);
        this.filterQ = setTimeout(() => {
            this.filterTreeTable(this.unfilteredTreeNode, this.searchValue, this.treeTable);
        }, event ? 600 : 0);
    }
    
    ngOnInit() {

        this.routeParamsSubscription = this.route.params.subscribe(params => {

            this.modelId = +params['modelId'];

            this.setObjectInfo('TaxonomyType', this.modelId);
            this.setCommonRightSideBar(true);
            this.currentAreaNameSubscription =
                this.headerBreadcrumbService
                    .getAreaName('TaxonomyType', this.modelId)
                    .subscribe(result => { this.currentAreaName = result });

            this.getFieldsDefinition();


            this.loadPermissions(this.permissionsService, StringConstants.ObjectTaxonomyType, this.modelId);
            this.setObjectInfo(StringConstants.ObjectTaxonomyType, this.modelId);

            this.headerBreadcrumbService.setCurrentObjectInfo('TaxonomyType', this.modelId);

            this.modelsService.getModel(this.modelId).subscribe(
                result => {
                    this.searchValue = "";
                    this.model = result;
                    this.headerBreadcrumbService.getFolderTitle('#Models').then((res) => {
                        this.headerBreadcrumbService.clearBreadcrumbs();
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.currentAreaName ? this.currentAreaName : res, `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION}`));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.Name, SiteUrlHelpers.getObjectUrl('TAXONOMYTYPE', this.model.ID), undefined, 'TAXONOMYTYPE', this.model.ID, undefined, undefined, true));
                        this.headerBreadcrumbService.getFolderIcon(this.currentAreaName ? this.currentAreaName : res).then(icon => {
                            this.rightSidebarService.setCurrentArea(this.model.Name, icon, 'Model');
                            this.rightSidebarService.setCurrentObject('TaxonomyType', this.model.ID, this.model.Name, null, true);
                            this.setCommonRightSideBar(true, false, this.model.HasDashboards);
                            this.rightSidebarService.showItem(new RightSidebarItem('Diagram', 'modeldiagram', ['fa-sitemap'], `/sidebar/visualization/diagram/${this.objectID}`, null, 7))
                            this.rightSidebarService.showHeader(true);
                        });

                    });

                    this.loadModelHierarchy(this.modelId);

                    this.setBrowserTitle(this.titleService, this.model.Name);

                }
            );

            this.levelsService.getObjectLevels(this.modelId, StringConstants.ObjectTaxonomyType).subscribe(
                result => {
                    this.levels = result;
                }
            );
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.routeParamsSubscription.unsubscribe();
        this.currentAreaNameSubscription.unsubscribe();
    }

    private loadModelHierarchy(modelId: number) {
        this.isLoading = true;

        this.modelsService.getModelHierarchy(modelId, true, true).subscribe(
            result => {
                this.modelHierarchy = result;

                this.treeNodeArray = this.buildTreeNodeArray(this.modelHierarchy, 1);
                this.unfilteredTreeNode = JSON.parse(JSON.stringify(this.treeNodeArray));

                this.isLoading = false;
                this.filter(null);
            }
        );
    }

    private getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.modelId, StringConstants.ObjectTaxonomyType).subscribe(
            result => {
                this.columns = result.Columns;
                this.fields = result.Fields;
            }
        );
    }

    private modelTaxonomyTitle(): string {
        if (!this.selected) {
            let thisLevel = this.levels.filter(x => x.Level == this.selectedLevel + 1);

            if (thisLevel && thisLevel.length > 0)
                return thisLevel[0].Name;
            else
                return `(Level ${this.selectedLevel + 1}) Item`;
        }

        let thisLevel = this.levels.filter(x => x.Level == this.selected.data.Level);

        if (thisLevel && thisLevel.length > 0) return thisLevel[0].Name;
        return `(Level ${this.selected.data.Level + 1}) Item`;
    }

    private buildTreeNodeArray(models: ModelHierarchy[], levelNumber: number, Parent?: number): TreeNode[] {
        //find the root items then 

        let rootNodes = models.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));

        if (rootNodes.length == 0) return null;

        let res: TreeNode[] = [];

        for (let root of rootNodes) {
            root.Level = levelNumber;
            res.push({
                label: root.DisplayValue,
                expanded: false,
                data: root,
                children: (this.buildTreeNodeArray(models, levelNumber + 1, root.ID)) //recursively find its children
            });
        }

        return res;
    }

    private showHierarchy(id: number) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('TAXONOMY', id, this.modelId));
    }

    setTreeNodeStyles(node) {
        if (!node.data) return null;

        let styles = {
            'font-weight': node.data.hasRelations ? 'bold' : 'normal',
        };
        return styles;
    }

    public onDeleted() {
        this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was removed        
        this.deleteSelectedTreeNode(this.selected.data.ID);
        this.modelHierarchy = this.modelHierarchy.filter(x => x.ID != this.selected.data.ID);
        this.treeNodeArray = this.buildTreeNodeArray(this.modelHierarchy, 1);
        this.unfilteredTreeNode = JSON.parse(JSON.stringify(this.treeNodeArray));

        this.selected = null;
        this.showDelete = false;
        this.filter(null);
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

    private saveTaxonomy(event) {
        this.isLoading = true;
        this.modelsService.saveModelHierarchy(event.item).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.loadModelHierarchy(this.modelId);
                this.headerActionsService.emitFavoritesChange();
                this.isLoading = false;
                this.showEditor = false;
            }
        );
    }

    private closeEditor() {
        this.showEditor = false;
    }

    private showAdd(level: number) {
        this.showEditor = true;
        this.selectedParentID = level == 0 ? undefined : this.selected ? this.selected.data.ID : undefined;
        this.selectedLevel = level;
        this.selected = null;
    }
}
