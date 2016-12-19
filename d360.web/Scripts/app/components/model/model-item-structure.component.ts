import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { ModelsService } from '../../services/models.service';
import { MessagesService } from '../../services/messages.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Model, ModelHierarchy } from '../../models/model.model';
import { TreeNode } from 'primeng/primeng';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';

@Component({
    selector: 'd3s-model-item-structure',
    providers: [ModelsService, PermissionsService],
    template: ` <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="model?.ID" [objectName]="model?.Name" [objectType]="'TaxonomyType'"></d3s-audit>
                <d3s-model-diagram *ngIf="!isLoading && isModelDiagramVisible" [id]="modelId"></d3s-model-diagram>                
                <div class="row" *ngIf="!isLoading && isOwnershipVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">   
                            <d3s-people-responsibilities-tile [objectID]="model?.ID" [objectType]="'TaxonomyType'" [title]="'Ownership of ' + model?.Name"></d3s-people-responsibilities-tile>
                        </div>
                    </div>
                </div>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="tile tile-detail" *ngIf="!isLoading && !isAuditVisible && !isOwnershipVisible && !isModelDiagramVisible">                            
                    <header *ngIf="!showDelete && !showEditor">{{model.Name}}
                        <d3s-tile-actions [hasAdd]="hasRootCreatePermissions()" (addClick)="showAdd()"></d3s-tile-actions>                            
                    </header>                                                
                    <div *ngIf="!showDelete && !showEditor && model.Description && model.Description.length >0" [innerHtml]="model.Description" class="item-description"></div>  
                    <input type="text" pInputText [(ngModel)]="searchValue" placeholder="Search" style="width: 100%;margin-bottom:10px;" *ngIf="!showDelete && !showEditor">                      
                    <p-treeTable *ngIf="!showDelete && !showEditor" [value]="treeNodeArray | treeSearch: searchValue" selectionMode="single" [(selection)]="selected" styleClass="breadcrumbTree" [style]="{'line-height':'25px'}">
                        <p-column field="name" header="Name">
                            <template let-item="rowData" pTemplate type="body">
                                <a (click)="showHierarchy(item.data.id)" [ngStyle]="setTreeNodeStyles(item)" class="link">{{item.data.name}} <i *ngIf="item.data?.hasRelations" class="fa fa-share-alt" aria-hidden="true" title="Item has relationships" style="color:#999;"></i></a>                                
                            </template>

                        </p-column>                        
                        <p-column field="description" header="Description">
                            <template let-item="rowData" pTemplate type="body">
                               <div class="truncate" [title]="item.data.description">{{item.data.description}}</div>
                            </template>
                        </p-column>
                        <p-column [style]="{width:'40px'}" *ngIf="hasRootCreatePermissions()" >
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;showAdd()"><i class="fa fa-plus"></i></a>                                        
                                        </div>
                                    </template>
                        </p-column>     
                        <p-column [style]="{width:'40px'}" *ngIf="hasRootUpdatePermissions()" >
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </template>
                        </p-column>                            
                        <p-column  [style]="{width:'40px'}" *ngIf="hasRootDeletePermissions()">
                                    <template let-item="rowData" pTemplate type="body">
                                        <div class="RowTools">                                
                                            <a *ngIf="!item.children || item.children?.length == 0" style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </template>
                        </p-column>       
                    </p-treeTable>                                   
                    <d3s-delete-form *ngIf="showDelete"
                        [callback]="theDeleteCallback"
                        [itemId]="selected?.data?.id"
                        [method]="'callback'"
                        [prompt]="'Are you sure you want to delete the model item [' + [selected?.data?.name] + ']?'"                                         
                        (onCancel)="showDelete=false;"
                    ></d3s-delete-form>        
                    <d3s-dynamic-editor rowID="id" *ngIf="showEditor" [objectID]="model.ID" [objectType]="'Taxonomy'" [parentID]="selectedParentID" [title]="'Model Taxonomy'" [selection]="selected?.data" (saveClick)="saveTaxonomy($event)" (closeClick)="closeEditor()"></d3s-dynamic-editor>              
                </div>                
                `
})

export class ModelItemStructureComponent extends BaseComponent implements OnInit, OnDestroy {
    sub: any;
    rightSub: any;
    
    model: Model;
    modelHierarchy: ModelHierarchy[] = [];
    
    modelId: number;
    selectedParentID: number;
    treeNodeArray: TreeNode[] = [];
    selected: TreeNode;

    searchValue: string;
    showEditor: boolean;
    showDelete: boolean;
    isModelDiagramVisible = false;

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
        protected permissionsService: PermissionsService
    ) {
        super(rightSidebarService);

        this.setCommonRightSideBar(true, true);
        this.rightSidebarService.showItem({
            icons: ['fa-sitemap'],
            tag: 'modeldiagram',
            title: 'Hierarchy Diagram',
            active: false
        });

        this.theDeleteCallback = this.deleteModelHierarchy.bind(this);
    }

    ngOnInit() {
        
        this.sub = this.route.params.subscribe(params => {

            this.modelId = +params['modelId'];

            this.loadPermissions(this.permissionsService, StringConstants.ObjectTaxonomyType, this.modelId);

            this.headerBreadcrumbService.setCurrentObjectInfo('TaxonomyType', this.modelId);
            this.isLoading = true;
            this.modelsService.getModel(this.modelId)
                    .then(result => {
                        this.isLoading = false;
                        this.model = result;

                        this.headerBreadcrumbService.clearBreadcrumbs();
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Models', `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION}`));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.ClassificationName, `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION}/${this.model.ClassificationName}`));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.Name, SiteUrlHelpers.getObjectUrl('TAXONOMYTYPE', this.model.ID)));

                        this.loadModelHierarchy(this.modelId);

                        this.setBrowserTitle(this.titleService, this.model.Name);

                });

            this.rightSub = this.rightSidebarService.rightSidebarClicked$.subscribe(r => {
                if (r.tag == 'modeldiagram') {
                    this.isModelDiagramVisible = !this.isModelDiagramVisible;
                } else {
                    this.isModelDiagramVisible = false;
                }
            });
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
        this.rightSub.unsubscribe();        
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
                    id: root.ID, hasRelations: root.HasChildren, name: root.Name, description: root.Description.replace(/<[^>]+>/gm, '')
                },
                children: (this.buildTreeNodeArray(models, root.ID)) //recursively find its children
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
            this.isLoading = false;
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

    private showAdd() {
        this.showEditor = true;        
        this.selectedParentID = this.selected ? this.selected.data.id : undefined;        
        this.selected = null;
    }
    
};