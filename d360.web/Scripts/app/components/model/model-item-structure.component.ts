import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, ModelsService, RightSidebarService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Model, ModelHierarchy } from '../../models/model.model';
import { TreeNode } from 'primeng/primeng';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-model-item-structure',
    providers: [ModelsService],
    template: ` <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="model?.ID" [objectName]="model?.Name" [objectType]="'TaxonomyType'"></d3s-audit>                
                <div class="row" *ngIf="!isLoading && isOwnershipVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">   
                            <d3s-people-responsibilities-tile [objectID]="model?.ID" [objectType]="'TaxonomyType'" [title]="'Ownership of ' + model?.Name"></d3s-people-responsibilities-tile>
                        </div>
                    </div>
                </div>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="tile tile-detail" *ngIf="!isLoading && !isAuditVisible && !isOwnershipVisible">                            
                    <header *ngIf="!showDelete && !showEditor">{{model.Name}}
                        <d3s-tile-actions [hasAdd]="true" (addClick)="showAdd()"></d3s-tile-actions>                            
                    </header>                              
                    <input type="text" pInputText [(ngModel)]="searchValue" placeholder="Search" style="width: 100%;" *ngIf="!showDelete && !showEditor">                      
                    <p-treeTable *ngIf="!showDelete && !showEditor" [value]="treeNodeArray | treeSearch: searchValue" selectionMode="single" [(selection)]="selected" styleClass="breadcrumbTree" [style]="{'line-height':'25px'}">
                        <p-column field="name" header="Name">
                            <template let-item="rowData" pTemplate type="body">
                                <a (click)="showHierarchy(item.data.id)" [ngStyle]="setTreeNodeStyles(item)">{{item.data.name}} <i *ngIf="item.data?.hasRelations" class="fa fa-share-alt" aria-hidden="true" title="Item has relationships" style="color:#999;"></i></a>                                
                            </template>

                        </p-column>                        
                        <p-column field="description" header="Description">
                            <template let-item="rowData" pTemplate type="body">
                               <span [innerHtml]="item.data.description"></span>
                            </template>
                        </p-column>
                        <p-column [style]="{width:'40px'}" >
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;showAdd()"><i class="fa fa-plus"></i></a>                                        
                                        </div>
                                    </template>
                        </p-column>     
                        <p-column [style]="{width:'40px'}" >
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </template>
                        </p-column>                            
                        <p-column  [style]="{width:'40px'}">
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">                                
                                            <a *ngIf="!item.children" style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </template>
                        </p-column>       
                    </p-treeTable>                                   
                    <delete-form *ngIf="showDelete"
                        [callback]="theDeleteCallback"
                        [itemId]="selected?.data?.id"
                        [method]="'callback'"
                        [prompt]="'Are you sure you want to delete the model item [' + [selected?.data?.name] + ']?'"                                         
                        (onCancel)="showDelete=false;"
                    ></delete-form>        
                    <d3s-dynamic-editor rowID="id" *ngIf="showEditor" [objectID]="model.ID" [objectType]="'Taxonomy'" [parentID]="selectedParentID" [title]="'Model Taxonomy'" [selection]="selected?.data" (saveClick)="saveTaxonomy($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>              
                </div>                
                `
})

export class ModelItemStructureComponent extends BaseComponent implements OnInit, OnDestroy {
    sub: any;
    
    model: Model;
    modelHierarchy: ModelHierarchy[] = [];
    
    modelId: number;
    selectedParentID: number;
    treeNodeArray: TreeNode[] = [];
    selected: TreeNode;

    searchValue: string;
    showEditor: boolean;
    showDelete: boolean;

    theDeleteCallback: Function;

    constructor(private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService,
        protected modelsService: ModelsService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super(rightSidebarService);

        this.setCommonRightSideBar(true, true);

        this.theDeleteCallback = this.deleteModelHierarchy.bind(this);
    }

    ngOnInit() {
        
        this.sub = this.route.params.subscribe(params => {

            this.modelId = +params['modelId'];
            
            this.isLoading = true;
            this.modelsService.getModel(this.modelId)
                    .then(result => {
                        this.isLoading = false;
                        this.model = result;

                        this.headerBreadcrumbService.clearBreadcrumbs();
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Models', `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION}`));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.ClassificationName, `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION }/${this.model.ClassificationName}`));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.Name, `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${this.model.ID}/structure`));

                        this.loadModelHierarchy(this.modelId);

                        this.setBrowserTitle(this.titleService, this.model.Name);

                });
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();        
    }

    private loadModelHierarchy(modelId: number) {
        this.modelsService.getModelHierarchy(modelId, true)
            .then(result => {
                this.modelHierarchy = result;

                this.treeNodeArray = this.buildTreeNodeArray(this.modelHierarchy)                
            });
    }
    
    private buildTreeNodeArray(models: ModelHierarchy[], Parent?: number): TreeNode[] {
        //find the root items then 

        let rootNodes = models.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));

        if (rootNodes.length == 0) return null;

        let res: TreeNode[] = [];

        for (let root of rootNodes) {
            res.push({
                label: root.Name,
                expanded: true,
                data: {
                    id: root.ID, hasRelations: root.HasChildren, name: root.Name, description: root.Description
                },
                children: (this.buildTreeNodeArray(models, root.ID)) //recursively find its children
            });
        }

        return res;
    }

    private showHierarchy(id: number) {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${this.modelId};hierarchyId=${id}`);
    }
    
    setTreeNodeStyles(node) {
        if (!node.data) return null;

        let styles = {
            'font-weight': node.data.hasRelations ? 'bold' : 'normal',
        };
        return styles;
    }

    deleteModelHierarchy(id: number) {
        this.modelsService.deleteModelHierarchy(id).then(res => {
            if (res.type && res.type != "error")
                this.deleteSelectedTreeNode(id);
        });
        this.showDelete = false;
    }

    private deleteSelectedTreeNode(id: number): TreeNode {
        let nodes: TreeNode[] = [];

        // add root nodes
        for (var i = 0; i < this.treeNodeArray.length; i++) {
            if (this.treeNodeArray[i].data.id && this.treeNodeArray[i].data.id == id) {
                this.treeNodeArray.splice(i, 1);
                return
            }

            nodes.push(this.treeNodeArray[i]);
        }

        //do a breadth first search for the given treenode
        if (nodes.length == 0) return;

        let node = nodes[0];

        while (node) {
            if (node.data.id && node.data.id == id) {

                return node;
            }

            //push children
            if (node.children) {
                for (var i = 0; i < node.children.length; i++) {                    
                    if (node.children[i].data.id && node.children[i].data.id == id) {
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
        this.modelsService.saveModelHierarchy(event.item)
            .then(result => {
                this.loadModelHierarchy(this.modelId);
                this.showEditor = false;
            });
    }

    private closeEditor() {
        this.showEditor = false;        
    }

    private showAdd() {
        this.showEditor = true;

        this.selectedParentID = this.selected ? this.selected.data.id : undefined;
        this.selected = null;
    }
    
};