import {Input, Component, EventEmitter, Output, OnInit, OnDestroy} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {BaseComponent} from '../shared/base.component';
import {Title} from '@angular/platform-browser';
import {ModelsService} from '../../services/models.service';
import {MessagesService} from '../../services/messages.service';
import {HeaderBreadcrumbService} from '../../services/header-breadcrumb.service';
import {PermissionsService} from '../../services/permissions.service';
import {RightSidebarService} from '../../services/right-sidebar.service';
import {HeaderActionsService} from '../../services/header-actions.service';
import {Breadcrumb} from '../../models/breadcrumb.model';
import {Model, ModelHierarchy} from '../../models/model.model';
import {TreeNode} from 'primeng/primeng';
import {SiteUrlHelpers} from '../../static/site-url-helpers';
import {StringConstants} from '../../static/string-constants';
import {LevelsService} from '../../services/levels.service';
import {RightSidebarItem} from '../../models/rightsidebar.model';
import {GridColumn, GridField} from '../../models/grid-definition.model';
import {GridDefinitionService} from '../../services/grid-definition.service';

@Component({
    selector: 'd3s-model-item-structure',
    providers: [GridDefinitionService, ModelsService, PermissionsService, LevelsService],
    templateUrl: './model-item-structure.component.html'
})

export class ModelItemStructureComponent extends BaseComponent implements OnInit, OnDestroy {
    sub: any;
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

    searchValue: string;
    showEditor: boolean;
    showDelete: boolean;
    selectedLevel: number = 0;

    theDeleteCallback: Function;

    constructor(
        private headerActionsService: HeaderActionsService,
        private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService,
        protected modelsService: ModelsService,
        protected titleService: Title,
        protected messagesService: MessagesService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        protected levelsService: LevelsService,
        protected gridDefinitionService: GridDefinitionService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;

        this.theDeleteCallback = this.deleteModelHierarchy.bind(this);
        router.events.subscribe((value) => {
            this.showEditor = false;
        });
    }

    ngOnInit() {

        this.sub = this.route.params.subscribe(params => {

            this.modelId = +params['modelId'];

            this.setObjectInfo('TaxonomyType', this.modelId);
            this.setCommonRightSideBar(true);

            this.getFieldsDefinition();

            this.rightSidebarService.showItem(new RightSidebarItem('Hierarchy Diagram', 'modeldiagram', ['fa-sitemap'], `/sidebar/visualization/diagram/${this.objectID}`))
            this.loadPermissions(this.permissionsService, StringConstants.ObjectTaxonomyType, this.modelId);
            this.setObjectInfo(StringConstants.ObjectTaxonomyType, this.modelId);

            this.headerBreadcrumbService.setCurrentObjectInfo('TaxonomyType', this.modelId);

            this.modelsService.getModel(this.modelId)
                .then(result => {
                    this.searchValue = "";
                    this.model = result;

                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Models', `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION}`));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.Name, SiteUrlHelpers.getObjectUrl('TAXONOMYTYPE', this.model.ID)));

                    this.loadModelHierarchy(this.modelId);

                    this.setBrowserTitle(this.titleService, this.model.Name);

                });

            this.levelsService.getObjectLevels(this.modelId, StringConstants.ObjectTaxonomyType).subscribe(
                result => {
                    this.levels = result;
                }
            );
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
    }

    private loadModelHierarchy(modelId: number) {
        this.isLoading = true;
        this.modelsService.getModelHierarchy(modelId, true, true)
            .then(result => {
                this.modelHierarchy = result;

                this.treeNodeArray = this.buildTreeNodeArray(this.modelHierarchy, 1);
                this.isLoading = false;
            });
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

    deleteModelHierarchy(id: number) {
        this.isLoading = true;
        this.modelsService.deleteModelHierarchy(id).then(res => {
            if (!res.isError) {
                this.deleteSelectedTreeNode(id);
            }

            this.showMessageForResult(this.messagesService, res);
            this.headerActionsService.emitFavoritesChange();
            this.selected = null;
            this.isLoading = false;
        });
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
        if (nodes.length == 0) return;

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

            if (nodes.length == 0) return null;
            node = nodes[0];
        }
    }

    private saveTaxonomy(event) {
        this.isLoading = true;
        this.modelsService.saveModelHierarchy(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.loadModelHierarchy(this.modelId);
                this.headerActionsService.emitFavoritesChange();
                this.isLoading = false;
                this.showEditor = false;
            });
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
};
