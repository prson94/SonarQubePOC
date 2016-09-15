///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, ModelsService, RightSidebarService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Model, ModelHierarchy } from '../../models/model.model';
import { TreeNode } from 'primeng/primeng';

@Component({
    selector: 'd3s-model-item',
    providers: [ModelsService],
    template: ` <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="selected?.ID" [objectName]="selected?.Name" [objectType]="'Taxonomy'"></d3s-audit>                
                <div class="row" *ngIf="!isLoading && isOwnershipVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">   
                            <d3s-people-responsibilities-tile [objectID]="selected?.ID" [objectType]="'Taxonomy'" [title]="'Ownership of ' + selected?.Name"></d3s-people-responsibilities-tile>
                        </div>
                    </div>
                </div>
                <div *ngIf="isLoading">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div *ngIf="!isLoading && !isAuditVisible && !isOwnershipVisible" class="row">
                    <div class="col s12">
                        <div class="row">
                            <div class="col s12">
                                 <div class="tile tile-detail" style="padding-left:0;padding-right:0;">
                                    <d3s-object-governance-tile [objectType]="'Taxonomy'" [objectID]="selected?.ID" [objectName]="selected?.Name"></d3s-object-governance-tile>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <d3s-object-definition-tile [objectType]="'Taxonomy'" [objectID]="selected?.ID"></d3s-object-definition-tile>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <d3s-object-relationships-tile [objectType]="'Taxonomy'" [objectID]="selected?.ID" [objectName]="selected?.Name"></d3s-object-relationships-tile>
                                </div>
                            </div>
                        </div>
                    </div>                   
                </div>
                `
})

export class ModelItemComponent extends BaseComponent implements OnInit, OnDestroy {
    sub: any;
    treeSub: any;
    model: Model;
    modelHierarchy: ModelHierarchy[] = [];
    selected: ModelHierarchy;
    modelId: number;
    treeNodeArray: TreeNode[] = [];
    
    constructor(private route: ActivatedRoute,
            private router: Router,
            rightSidebarService: RightSidebarService,
            protected modelsService: ModelsService,
            protected titleService: Title,
            protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super(rightSidebarService);

        this.setCommonRightSideBar(true, true);
    }

    ngOnInit() {        
        this.treeSub = this.headerBreadcrumbService.breadcrumbTreeSource$.subscribe(
            id => {
                this.showHierarchy(id);  
            });
        
        this.sub = this.route.params.subscribe(params => {
            
            let newModelId = +params['modelId'];
            let hierarchyId = params['hierarchyId'] ? +params['hierarchyId'] : 0;
            if (hierarchyId != 0)
                this.headerBreadcrumbService.setCurrentObjectInfo('Taxonomy', hierarchyId);
            else
                this.headerBreadcrumbService.setCurrentObjectInfo('TaxonomyType', newModelId);

            if (this.modelId != newModelId) {
                this.modelId = newModelId;
                this.isLoading = true;
                this.modelsService.getModel(this.modelId)
                    .then(result => {
                        this.isLoading = false;
                        this.model = result;

                        this.headerBreadcrumbService.clearBreadcrumbs();
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Models', '/a/model/classification'));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.ClassificationName, `/a/model/classification/${this.model.ClassificationName}`));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.Name, `/a/model/${this.model.ID}/structure`));

                        this.loadModelHierarchy(this.modelId, hierarchyId);

                        this.setBrowserTitle(this.titleService, this.model.Name);

                    });
            }
            else {
                // pop last breadcrumb
                this.headerBreadcrumbService.popLastBreadcrumb();
                this.selectModelHierarchy(hierarchyId)
            }
            
        });        
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
        this.treeSub.unsubscribe();
    }
    

    private selectModelHierarchy(selectedHierarchyId: number) {
        if (selectedHierarchyId > 0) {
            let selArray = this.modelHierarchy.filter(x => x.ID == selectedHierarchyId);
            if (selArray.length > 0) this.selected = selArray[0];
            else {
                console.log("ERROR INVALID SELECTED HIERARCY ID SPECIFIED.", selectedHierarchyId);

                this.selected = (this.modelHierarchy.length && this.modelHierarchy.length > 0) ? this.modelHierarchy[0] : null;
            }
        }
        else {
            this.selected = (this.modelHierarchy.length && this.modelHierarchy.length > 0) ? this.modelHierarchy[0] : null;
        }

        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.selected.Name, undefined, true, 'Taxonomy', this.selected.ID, this.treeNodeArray, this.findSelectedTreeNode(selectedHierarchyId)));
    }

    private loadModelHierarchy(modelId: number, selectedHierarchyId: number) {
        this.modelsService.getModelHierarchy(modelId)
            .then(result => {
                this.modelHierarchy = result;

                this.treeNodeArray = this.buildTreeNodeArray(this.modelHierarchy)

                this.selectModelHierarchy(selectedHierarchyId);                
            });
    }

    private findSelectedTreeNode(id: number): TreeNode {
        let nodes: TreeNode[] = [];

        // add root nodes
        for (let rNode of this.treeNodeArray) {
            nodes.push(rNode);
        }

        //do a breadth first search for the given treenode
        if (nodes.length == 0) return;

        let node = nodes[0];

        while (node) {
            if (node.data.id && node.data.id == id) return node;

            //push children
            if (node.children) {
                for (let cNode of node.children) {
                    nodes.push(cNode);
                }
            }

            //remove this node
            nodes.splice(0, 1);

            if (nodes.length == 0) return null;
            node = nodes[0];
        }
    }

    private buildTreeNodeArray(models: ModelHierarchy[], Parent?: number): TreeNode[] {
        //find the root items then 

        let rootNodes = models.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));
        
        if (rootNodes.length == 0) return null;

        let res: TreeNode[] = [];

        for (let root of rootNodes) {
            res.push({
                label: root.Name,
                data: {
                    id: root.ID, hasRelations: root.HasChildren
                },                
                children: (this.buildTreeNodeArray(models, root.ID)) //recursively find its children
            });
        }       

        return res;
    }

    private showHierarchy(id: number) {
        this.router.navigateByUrl(`/a/model/${this.modelId};hierarchyId=${id}`);       
    }
};