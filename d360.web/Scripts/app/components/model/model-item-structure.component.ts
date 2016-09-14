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
                <div *ngIf="isLoading">
                    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div class="tile tile-detail" *ngIf="!isLoading && !isAuditVisible && !isOwnershipVisible">                            
                    <header>{{model.Name}}
                        <d3s-tile-actions [hasAdd]="true" [hasEdit]="true"></d3s-tile-actions>                            
                    </header>                              
                    <input type="text" [(ngModel)]="searchValue" placeholder="Search" style="width: 100%;">                      
                    <p-tree [value]="treeNodeArray | breadcrumbTreeSearch: searchValue" selectionMode="single" [(selection)]="selected" styleClass="breadcrumbTree" [style]="{'line-height':'25px'}">
                            <template let-node>
                                <span [ngStyle]="setTreeNodeStyles(node)"><span (dblclick)="showHierarchy(node.data?.id);">{{node.label}}</span> <i *ngIf="node.data?.hasRelations" class="fa fa-share-alt" aria-hidden="true" title="Item has relationships" style="color:#999;padding-left:20px"></i></span>
                            </template>
                    </p-tree>                                   
                </div>
                `
})

export class ModelItemStructureComponent extends BaseComponent implements OnInit, OnDestroy {
    sub: any;
    
    model: Model;
    modelHierarchy: ModelHierarchy[] = [];
    
    modelId: number;
    treeNodeArray: TreeNode[] = [];
    selected: TreeNode;

    searchValue: string;

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
        
        this.sub = this.route.params.subscribe(params => {

            this.modelId = +params['modelId'];
            
            this.isLoading = true;
            this.modelsService.getModel(this.modelId)
                    .then(result => {
                        this.isLoading = false;
                        this.model = result;

                        this.headerBreadcrumbService.clearBreadcrumbs();
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Models', '/a/model/classification'));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.ClassificationName, `/a/model/classification/${this.model.ClassificationName}`));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.Name, `/a/model/${this.model.ID}/structure`));

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
        this.modelsService.getModelHierarchy(modelId)
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
    
    setTreeNodeStyles(node) {
        if (!node.data) return null;

        let styles = {
            'font-weight': node.data.hasRelations ? 'bold' : 'normal',
        };
        return styles;
    }
};