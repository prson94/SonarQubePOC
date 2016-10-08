import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, PoliciesService, RightSidebarService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Policy, PolicyType } from '../../models/policy.model';
import { TreeNode } from 'primeng/primeng';
import { FormMode } from '../../models/form.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-policy-item-structure',
    template: `
                <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="policyType.ID" [objectName]="policyType.Name" [objectType]="'PolicyType'"></d3s-audit>                
                <div class="row" *ngIf="!isLoading && isOwnershipVisible">
                    <div class="col s12">
                        <div class="tile tile-detail">   
                            <d3s-people-responsibilities-tile [objectID]="policyType.ID" [objectType]="'PolicyType'" [title]="'Ownership of ' + policyType.Name"></d3s-people-responsibilities-tile>
                        </div>
                    </div>
                </div>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="tile tile-detail" *ngIf="!isLoading && !isAuditVisible && !isOwnershipVisible">                            
                    <header *ngIf="formMode == FormMode.Default">{{policyType.Name}}
                        <d3s-tile-actions [hasAdd]="true" (addClick)="add()"></d3s-tile-actions>                            
                    </header>                              
                    <input type="text" pInputText [(ngModel)]="searchValue" placeholder="Search" style="width: 100%;" *ngIf="formMode == FormMode.Default">                      
                    <p-treeTable *ngIf="formMode == FormMode.Default" [value]="treeNodeArray | treeSearch: searchValue" selectionMode="single" [(selection)]="selected" styleClass="breadcrumbTree" [style]="{'line-height':'25px'}">
                        <p-column field="Name" header="Name">
                            <template let-item="rowData" pTemplate type="body">
                                <a (click)="showHierarchy(item.data.ID)" [ngStyle]="setTreeNodeStyles(item)">{{item.data.Name}} <i *ngIf="item.data?.hasRelations" class="fa fa-share-alt" aria-hidden="true" title="Item has relationships" style="color:#999;"></i></a>                                
                            </template>
                        </p-column>                        
                        <p-column field="Description" header="Description">
                            <template let-item="rowData" pTemplate type="body">
                               <span [innerHtml]="item.data.Description"></span>
                            </template>
                        </p-column>
                        <p-column [style]="{width:'40px'}" >
                            <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools">
                                    <a style="cursor:pointer;" (click)="selected=item;add()"><i class="fa fa-plus"></i></a>                                        
                                </div>
                            </template>
                        </p-column>   
                        <p-column [style]="{width:'40px'}" >
                            <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools">
                                    <a style="cursor:pointer;" (click)="edit(item)"><i class="fa fa-pencil"></i></a>                                        
                                </div>
                            </template>
                        </p-column>                            
                        <p-column  [style]="{width:'40px'}">
                            <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools">                                
                                    <a *ngIf="!item.children" style="cursor:pointer;" (click)="delete(item)"><i class="fa fa-trash-o"></i></a>                                    
                                </div>
                            </template>
                        </p-column>       
                    </p-treeTable> 
                    <div *ngIf="formMode == FormMode.Editing" class="row">
                        <div class="col s12">
                                    <d3s-dynamic-editor
                                        [selection]="selected.data"
                                        [title]="selected.data.Name"
                                        objectType="policy"
                                        [objectID]="selected.data.ID"
                                        editUri="form/dynamicedit/edit/policy"
                                        (closeClick)="formMode = FormMode.Default"
                                        (saveClick)="formMode = FormMode.Default" >
                                    </d3s-dynamic-editor>
                        </div>
                    </div>
                    <div *ngIf="formMode == FormMode.Adding" class="row">
                        <div class="col s12">
                            <d3s-dynamic-editor [objectID]="selected?.data?.ID" [objectType]="'Policy'" [parentID]="selectedParentID" [title]="'Add Policy'" [selection]="selected?.data" (saveClick)="formMode = FormMode.Default" (closeClick)="formMode = FormMode.Default"></d3s-dynamic-editor>    
                        </div>
                    </div> 
                    <delete-form *ngIf="formMode == FormMode.Deleting"
                        [uri]="'form/DeletePolicyByID?id=' + selected?.data?.ID"
                        [method]="'delete'"
                        [prompt]="'Are you sure you want to delete the policy item ' + selected?.data?.Name + '?'"                                         
                        (onCancel)="formMode = FormMode.Default;"
                        (onDeleteComplete)="formMode = FormMode.Default"
                    ></delete-form>   
                `,
    providers: [PoliciesService]
})

export class PolicyItemStructureComponent extends BaseComponent implements OnInit, OnDestroy {
    sub: any;

    policyType: PolicyType;
    policies: Policy[] = [];

    policyTypeId: number;
    treeNodeArray: TreeNode[] = [];
    selected: TreeNode;
    selectedParentID: number;

    

    searchValue: string;

    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private policiesService: PoliciesService,
        private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService
    ) {

        super(rightSidebarService);
        this.clearSidebar();
        this.setCommonRightSideBar(true, true);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {

            this.policyTypeId = +params['policyTypeId'];
            
            this.isLoading = true;
            this.policiesService.getPolicyType(this.policyTypeId)
                .then(result => {
                    this.isLoading = false;
                    this.policyType = result;
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Policy', SiteUrlHelpers.SITE_URL_POLICY_ROOT)); // this route is missing!?!
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.policyType.Name, `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${this.policyTypeId}/structure`));

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
                    ID: root.ID, Name: root.Name, Description: root.Description, ParentID: root.ParentID
                },
                children: (this.buildTreeNodeArray(models, root.ID)) //recursively find its children
            });
        }

        return res;
    }

    private showHierarchy(id: number) {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${this.policyTypeId};hierarchyId=${id}`);
    }

    setTreeNodeStyles(node) {
        if (!node.data) return null;

        let styles = {
            'font-weight': node.data.hasRelations ? 'bold' : 'normal',
        };
        return styles;
    }

    private edit(item) {
        this.selected = item;
        this.formMode = FormMode.Editing;
    }

    private delete(item) {
        this.selected = item;
        this.formMode = FormMode.Deleting;
    }

    private add() {
        this.selectedParentID = this.selected ? this.selected.data.ID : null;
        this.selected = null;
        this.formMode = FormMode.Adding;
    }

};