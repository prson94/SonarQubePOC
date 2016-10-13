import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, PoliciesService, RightSidebarService, PermissionsService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Policy, PolicyType } from '../../models/policy.model';
import { TreeNode } from 'primeng/primeng';
import { FormMode } from '../../models/form.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';

@Component({
    selector: 'd3s-policy-item',
    template: `
                <d3s-audit *ngIf="!isLoading && isAuditVisible" [objectID]="selected?.ID" [objectName]="selected?.Name" [objectType]="'Policy'"></d3s-audit>                
                <d3s-lineage *ngIf="!isLoading && isLineageVisible" [objectID]="selected?.ID" [objectName]="selected?.Name" [objectType]="'Policy'"></d3s-lineage>
                <d3s-impact *ngIf="!isLoading && isImpactVisible" [objectID]="selected?.ID" [objectName]="selected?.Name" [objectType]="'Policy'"></d3s-impact>
                <div *ngIf="!isLoading && isRelationshipsVisible" class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">                
                            <d3s-object-relationships [objectPermissions]="permissions" [objectID]="selected?.ID" [objectName]="selected?.Name" [objectType]="'Policy'"></d3s-object-relationships>
                        </div>
                    </div>
                </div>

                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading && !isAuditVisible && !isOwnershipVisible && !isLineageVisible && !isDashboardVisible && !isRelationshipsVisible" class="row">                    
                    <div class="col s12">
                        <div class="row">
                            <div class="col s12">
                                 <div class="tile tile-detail" style="padding-left:0;padding-right:0;">
                                    <d3s-object-governance [objectType]="'Policy'" [objectID]="selected?.ID" [objectName]="selected?.Name"></d3s-object-governance>
                                </div>
                            </div>
                        </div>
                        <div *ngIf="formMode == FormMode.Default" class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <header><d3s-tile-actions hasEdit="hasRootUpdatePermissions()" (editClick)="edit()"></d3s-tile-actions></header>
                                    <object-detail [objectType]="'Policy'" [objectID]="selected?.ID"></object-detail>
                                </div>
                            </div>
                        </div>  
                        <div *ngIf="formMode == FormMode.Default" class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <d3s-people-responsibilities-tile [objectType]="'Policy'" [objectID]="selected?.ID"></d3s-people-responsibilities-tile>
                                </div>
                            </div>
                        </div>                        
                    </div> 
                    <div *ngIf="formMode == FormMode.Editing" class="col s12">
                         <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">
                                    <d3s-dynamic-editor
                                        [selection]="selected"
                                        [title]="selected.Name"
                                        objectType="policy"
                                        [objectID]="selected.ID"
                                        editUri="form/dynamicedit/edit/policy"
                                        hasCloseButton="true"
                                        (closeClick)="formMode = FormMode.Default"
                                        (saveClick)="formMode = FormMode.Default" >
                                    </d3s-dynamic-editor>
                                </div>
                            </div>
                        </div>
                    </div>                  
                </div>
                `,
    providers: [PoliciesService, PermissionsService]
})

export class PolicyItemComponent extends BaseComponent implements OnInit, OnDestroy {
    policyTypeId: number;
    policies: Policy[] = [];
    policyType: PolicyType;
    treeNodeArray: TreeNode[] = [];
    selected: Policy;
    sub: any;
    treeSub: any;

    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;


    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private policiesService: PoliciesService,
        private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService,
        private permissionsService: PermissionsService
    ) {
        super(rightSidebarService);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, '- Policy');
        
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Policy'));

        this.treeSub = this.headerBreadcrumbService.breadcrumbTreeSource$.subscribe(
            id => {
                this.showHierarchy(id);
            });


        this.sub = this.route.params.subscribe(params => {
            let newPolicyTypeId = +params['policyTypeId'];
            let hierarchyId = +params['hierarchyId'] || 0;


            if (hierarchyId != 0)
                this.headerBreadcrumbService.setCurrentObjectInfo('Policy', hierarchyId);
            else
                this.headerBreadcrumbService.setCurrentObjectInfo('PolicyType', newPolicyTypeId);


            if (this.policyTypeId != newPolicyTypeId) {
                this.policyTypeId = newPolicyTypeId;

                this.isLoading = true;
                this.policiesService.getPolicyType(this.policyTypeId)
                    .then(result => {
                        this.policyType = result;

                        this.headerBreadcrumbService.clearBreadcrumbs();
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Policy', SiteUrlHelpers.SITE_URL_POLICY_ROOT));
                        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.policyType.Name, `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${this.policyType.ID}/structure`));

                        this.loadPolicyItems(this.policyTypeId, hierarchyId);

                        this.setBrowserTitle(this.titleService, this.policyType.Name);

                        this.clearSidebar();
                        this.setCommonRightSideBar(true, false, false, true, true, true);

                        this.isLoading = false;
                    });
            } else {
                this.headerBreadcrumbService.popLastBreadcrumb();
                this.selectPolicyHierarchy(hierarchyId);
            }
        });

        
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.sub.unsubscribe();
        this.treeSub.unsubscribe();

    }

    loadPolicyItems(policyTypeId: number, selectedHierarchyId: number ) {
        this.policiesService.getPolicies(policyTypeId).then(r => {
            this.policies = r;
            this.treeNodeArray = this.buildTreeNodeArray(this.policies);
            this.selectPolicyHierarchy(selectedHierarchyId);
        });
    }


    private buildTreeNodeArray(policies: Policy[], Parent?: number): TreeNode[] {
        //find the root items then 

        let rootNodes = policies.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));

        if (rootNodes.length == 0) return null;

        let res: TreeNode[] = [];

        for (let root of rootNodes) {
            res.push({
                label: root.Name,
                expanded: true,
                data: {
                    id: root.ID
                },
                children: (this.buildTreeNodeArray(policies, root.ID)) //recursively find its children
            });
        }

        return res;
    }

    private selectPolicyHierarchy(selectedHierarchyId: number) {
        if (selectedHierarchyId > 0) {
            let selArray = this.policies.filter(x => x.ID == selectedHierarchyId);
            if (selArray.length > 0) this.selected = selArray[0];
            else {
                console.log("ERROR INVALID SELECTED HIERARCY ID SPECIFIED.", selectedHierarchyId);

                this.selected = (this.policies.length && this.policies.length > 0) ? this.policies[0] : null;
            }
        }
        else {
            this.selected = (this.policies.length && this.policies.length > 0) ? this.policies[0] : null;
        }

        this.loadPermissions(this.permissionsService, StringConstants.ObjectPolicy, this.selected.ID);

        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.selected.Name, undefined, true, 'Taxonomy', this.selected.ID, this.treeNodeArray, this.findSelectedTreeNode(selectedHierarchyId)));
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

    private showHierarchy(id: number) {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${this.policyTypeId};hierarchyId=${id}`);
    }

    private edit() {
        this.formMode = FormMode.Editing;
    }

};