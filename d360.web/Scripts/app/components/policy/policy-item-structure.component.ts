import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PoliciesService } from '../../services/policies.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { MessagesService } from '../../services/messages.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { PermissionsService } from '../../services/permissions.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Policy, PolicyType, PolicyStatus } from '../../models/policy.model';
import { TreeNode } from 'primeng/primeng';
import { FormMode } from '../../models/form.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';

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
                    <header *ngIf="!showDelete && !showEditor">{{policyType.Name}}
                        <d3s-tile-actions [hasAdd]="hasRootCreatePermissions()" (addClick)="add()"></d3s-tile-actions>                            
                    </header>                              
                    <input type="text" pInputText [(ngModel)]="searchValue" placeholder="Search" style="width: 100%;margin-bottom:10px;" *ngIf="!showDelete && !showEditor">                      
                    <p-treeTable *ngIf="!showDelete && !showEditor" [value]="treeNodeArray | treeSearch: searchValue" selectionMode="single" [(selection)]="selected" styleClass="breadcrumbTree" [style]="{'line-height':'25px'}">
                        <p-column field="Name" header="Name">
                            <template let-item="rowData" pTemplate type="body">
                                <a (click)="showHierarchy(item.data.ID)" [ngStyle]="setTreeNodeStyles(item)">{{item.data.Name}} <i *ngIf="item.data?.hasRelations" class="fa fa-share-alt" aria-hidden="true" title="Item has relationships" style="color:#999;"></i></a>                                
                            </template>
                        </p-column>                        
                         <p-column field="Description" header="Description">
                            <template let-item="rowData" pTemplate type="body">
                               <div class="truncate" [title]="item.data.Description">{{item.data.Description}}</div>
                            </template>
                        </p-column>
                        <p-column field="StatusName" header="Status" sortable="custom" [filter]="!showSimpleFilter" [style]="{width:'10%'}"></p-column>  
                        <p-column [style]="{width:'40px'}" *ngIf="hasRootCreatePermissions()">
                            <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools">
                                    <a style="cursor:pointer;" (click)="selected=item;add()"><i class="fa fa-plus"></i></a>                                        
                                </div>
                            </template>
                        </p-column>   
                        <p-column [style]="{width:'40px'}" *ngIf="hasRootUpdatePermissions()">
                            <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools">
                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                </div>
                            </template>
                        </p-column>                            
                        <p-column  [style]="{width:'40px'}" *ngIf="hasRootDeletePermissions()">
                            <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools">                                
                                    <a *ngIf="!item.children" style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                </div>
                            </template>
                        </p-column>       
                    </p-treeTable> 
                    <d3s-delete-form *ngIf="showDelete"
                        [callback]="theDeleteCallback"
                        [itemId]="selected?.data?.ID"
                        [method]="'callback'"
                        [prompt]="'Are you sure you want to delete the policy item [' + [selected?.data?.Name] + ']?'"                                         
                        (onCancel)="showDelete=false;"
                    ></d3s-delete-form>        
                    <d3s-dynamic-editor *ngIf="showEditor" [objectID]="policyType.ID" objectType="Policy" [parentID]="selectedParentID" [title]="'Policy'" [selection]="selected?.data" (saveClick)="savePolicy($event)" (closeClick)="showEditor=false"></d3s-dynamic-editor>
                </div>                    
                `,
    providers: [PoliciesService, PermissionsService]
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

    showDelete: boolean = false;
    showEditor: boolean = false;

    theDeleteCallback: Function;


    constructor(
        protected titleService: Title,
        private headerActionsService: HeaderActionsService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private policiesService: PoliciesService,
        private route: ActivatedRoute,
        private router: Router,
        private messagesService: MessagesService,
        rightSidebarService: RightSidebarService,
        private permissionsService: PermissionsService
    ) {

        super(rightSidebarService);
        this.clearSidebar();
        this.setCommonRightSideBar(true, true);

        this.theDeleteCallback = this.deletePolicyItem.bind(this);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {

            this.policyTypeId = +params['policyTypeId'];
            this.headerBreadcrumbService.setCurrentObjectInfo('PolicyType', this.policyTypeId);

            this.loadPermissions(this.permissionsService, StringConstants.ObjectPolicyType, this.policyTypeId);

            this.isLoading = true;
            this.policiesService.getPolicyType(this.policyTypeId)
                .then(result => {
                    this.isLoading = false;
                    this.policyType = result;
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Policies', `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${SiteUrlHelpers.SITE_URL_POLICY_CLASSIFICATION}`));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.policyType.PolicyTypeClass, SiteUrlHelpers.getObjectUrl('POLICYTYPECLASS', this.policyType.PolicyTypeClassID)));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.policyType.Name, `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${this.policyTypeId}/structure`));

                    this.loadPolicyHierarchy(this.policyTypeId);

                    this.setBrowserTitle(this.titleService, this.policyType.Name);
                });
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
    }

    private loadPolicyHierarchy(policyTypeId: number) {
        this.policiesService.getPolicies(policyTypeId)
            .then(result => {
                for (let policy of result) {
                    policy.StatusName = PolicyStatus[policy.Status];
                }
                this.policies = result;                
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
                    ID: root.ID, Name: root.Name, Description: (root.Description ? root.Description.replace(/<[^>]+>/gm, '') : ''), ParentID: root.ParentID, StatusName: root.StatusName
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

    deletePolicyItem(id: number) {
        this.isLoading = true;
        this.policiesService.deletePolicyItem(id).then(res => {
            this.showMessageForResult(this.messagesService, res);
            if (res.type != 'error') {
                this.deleteSelectedTreeNode(id);
                this.headerActionsService.emitFavoritesChange();
            }
            this.isLoading = false;
        });
        this.showDelete = false;
    }
    

    private add() {            
        this.selectedParentID = this.selected ? this.selected.data.ID : null;
        this.selected = null;
        this.showEditor = true;
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

    private savePolicy(event) {
        this.isLoading = true;
        this.policiesService.savePolicy(event.item)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.headerActionsService.emitFavoritesChange();
                this.loadPolicyHierarchy(this.policyTypeId);
                this.isLoading = false;
                this.showEditor = false;
            });
    }
};