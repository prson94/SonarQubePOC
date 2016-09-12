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
                                    <d3s-object-relationships-tile [objectType]="'Taxonomy'" [objectID]="selected?.ID"></d3s-object-relationships-tile>
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
    hierarchyId: number;

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
            this.modelId = +params['modelId'];
            let newHierarchyId = params['hierarchyId'] ? +params['hierarchyId'] : 0;

            if (this.hierarchyId > 0 && this.hierarchyId == newHierarchyId) return;
            this.hierarchyId = newHierarchyId;
            
            this.isLoading = true;            
            this.modelsService.getModel(this.modelId)
                .then(result => {
                    this.isLoading = false;
                    this.model = result;
                    
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Information Models'));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.Name));

                    this.loadModelHierarchy(this.modelId, this.hierarchyId);

                    this.setBrowserTitle(this.titleService, this.model.Name);

                });           
            
        });        
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
        this.treeSub.unsubscribe();
    }

    private loadModelHierarchy(modelId: number, selectedHierarchyId: number) {
        this.modelsService.getModelHierarchy(modelId)
            .then(result => {
                this.modelHierarchy = result;

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
                
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.selected.Name, undefined, true, 'Taxonomy', this.selected.ID, this.buildTreeNodeArray(result)));
            });
    }

    private buildTreeNodeArray(models: ModelHierarchy[], Parent?: number): TreeNode[] {
        //find the root items then 

        let rootNodes = models.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));
        
        if (rootNodes.length == 0) return null;

        let res: TreeNode[] = [];

        for (let root of rootNodes) {
            res.push({
                label : root.Name,
                data: root.ID,                
                children: (root.HasChildren ? this.buildTreeNodeArray(models, root.ID) : null) //recursively find its children
            });
        }       

        return res;
    }

    private showHierarchy(id: number) {
        this.router.navigateByUrl(`/a/model/${this.modelId};hierarchyId=${id}`);
       /* let sel = this.modelHierarchy.filter(x => x.ID == id);

        if (sel.length <= 0) {
            console.log("ERROR UNABLE TO FIND SPECIFIED HIERARCHY ID");

            return;
        }
        this.hierarchyId = id;
       // this.route.params['hierarchyId'] = this.hierarchyId;

        this.selected = sel[0];*/
    }
};