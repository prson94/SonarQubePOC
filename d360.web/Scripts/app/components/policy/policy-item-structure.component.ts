import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, PoliciesService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Policy, PolicyType } from '../../models/policy.model';
import { TreeNode } from 'primeng/primeng';

@Component({
    selector: 'd3s-policy-item-structure',
    template: `
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="tile tile-detail" *ngIf="!isLoading && !isAuditVisible && !isOwnershipVisible">                            
                    <header *ngIf="!showDelete && !showEditor">{{policyType.Name}}
                        <d3s-tile-actions [hasAdd]="true" (addClick)="showAdd()"></d3s-tile-actions>                            
                    </header>                              
                    <input type="text" pInputText [(ngModel)]="searchValue" placeholder="Search" style="width: 100%;" *ngIf="!showDelete && !showEditor">                      
                    <p-treeTable *ngIf="!showEditor && !showDelete" [value]="treeNodeArray | treeSearch: searchValue" selectionMode="single" [(selection)]="selected" styleClass="breadcrumbTree" [style]="{'line-height':'25px'}">
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
                `,
    providers: [PoliciesService]
})

export class PolicyItemStructureComponent extends BaseComponent implements OnInit, OnDestroy {
    sub: any;

    policyType: PolicyType;
    policies: Policy[] = [];

    policyTypeId: number;
    selectedParentID: number;
    treeNodeArray: TreeNode[] = [];
    selected: TreeNode;

    searchValue: string;
    showEditor: boolean;
    showDelete: boolean;

    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private policiesService: PoliciesService,
        private route: ActivatedRoute,
        private router: Router
    ) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {

            this.policyTypeId = +params['policyTypeId'];

            this.isLoading = true;
            this.policiesService.getPolicyType(this.policyTypeId)
                .then(result => {
                    this.isLoading = false;
                    this.policyType = result;
                    console.log(this.policyType);
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Policy', '/a/policy'));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.policyType.Name, `/a/policy/${this.policyTypeId}/structure`));

                    this.loadModelHierarchy(this.policyTypeId);

                    this.setBrowserTitle(this.titleService, this.policyType.Name);

                });
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();

    }


    private loadModelHierarchy(policyTypeId: number) {
        this.policiesService.getPolicies(policyTypeId)
            .then(result => {
                this.policies = result;
                console.log(this.policies);
                this.treeNodeArray = this.buildTreeNodeArray(this.policies)
            });
    }

    private buildTreeNodeArray(models: Policy[], Parent?: number): TreeNode[] {
        //find the root items then 

        let rootNodes = models.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));

        if (rootNodes.length == 0) return null;

        let res: TreeNode[] = [];

        for (let root of rootNodes) {
            res.push({
                label: root.Name,
                expanded: true,
                data: {
                    id: root.ID, name: root.Name, description: root.Description
                },
                children: (this.buildTreeNodeArray(models, root.ID)) //recursively find its children
            });
        }

        return res;
    }

    private showHierarchy(id: number) {
        this.router.navigateByUrl(`/a/policy/${this.policyTypeId};hierarchyId=${id}`);
    }

    setTreeNodeStyles(node) {
        if (!node.data) return null;

        let styles = {
            'font-weight': node.data.hasRelations ? 'bold' : 'normal',
        };
        return styles;
    }

};