import {
    Component,
    OnInit,
    OnDestroy
} from '@angular/core';
import {Router, ActivatedRoute} from '@angular/router';
import {Title} from '@angular/platform-browser';
import {TreeNode} from 'primeng/api';

import {Breadcrumb} from '../../models/breadcrumb.model';
import {Policy, PolicyType } from '../../models/policy.model';
import {Permission} from '../../models/responsibility-type.model';

import {HeaderBreadcrumbService} from '../../services/header-breadcrumb.service';
import {PoliciesService} from '../../services/policies.service';
import {RightSidebarService} from '../../services/right-sidebar.service';
import {HeaderActionsService} from '../../services/header-actions.service';
import {PermissionsService} from '../../services/permissions.service';

import {BaseComponent} from '../shared/base.component';

import {StringConstants} from '../../static/string-constants';
import {SiteUrlHelpers} from '../../static/site-url-helpers';
import { RightSidebarItem } from '../../models/rightsidebar.model';

declare var CompanySettings;

@Component({
    selector: 'd3s-policy-item',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading"
             class="row">
            <div class="col s12">
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-object-definition-tile [nymTypes]="policyType?.NymTypes"
                                                        [objectPermissions]="permissions"
                                                        [objectType]="'Policy'"
                                                        [objectID]="selected?.ID"
                                                        [hasAttributes]="policyType.AllowAttributes"
                                                        (onEditComplete)="editComplete($event)"></d3s-object-definition-tile>
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
    private crumbs: Breadcrumb[] = [];
    routeParamsSubscription: any;
    private currentAreaNameSubscription: any;
    private currentAreaName: string;
    treeSub: any;

    private showSocialScoreBar = true;

    constructor(
        private headerActionsService: HeaderActionsService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private policiesService: PoliciesService,
        private route: ActivatedRoute,
        private router: Router,
        rightSidebarService: RightSidebarService,
        private permissionsService: PermissionsService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, '- Policy');

        this.routeParamsSubscription = this.route.params.subscribe(params => {
            const newPolicyTypeId = +params['policyTypeId'];
            // if hierarchyId is passed via alternative route to workaround
            // bug with router escaping ; = and other chars.
            let hierarchyId = +params['id'];

            if (!hierarchyId) {
                hierarchyId = +params['hierarchyId'] || 0;
            }
            this.headerBreadcrumbService.clearCurrentObjectInfo();
            if (hierarchyId != 0) {
                this.headerBreadcrumbService.setCurrentObjectInfo('Policy', hierarchyId);
            } else {
                this.headerBreadcrumbService.setCurrentObjectInfo('PolicyType', newPolicyTypeId);
            }
            this.setObjectInfo('Policy', hierarchyId);

            this.treeSub = this.headerBreadcrumbService.breadcrumbTreeSource$.subscribe(
                id => {
                    this.showHierarchy(id);
                }
            );

            if (this.policyTypeId != newPolicyTypeId) {
                this.policyTypeId = newPolicyTypeId;

                this.isLoading = true;

                this.load(hierarchyId).then(
                    () => this.isLoading = false
                );
            } else {
                this.headerBreadcrumbService.popLastBreadcrumb();

                this.selectPolicyHierarchy(hierarchyId).then(
                    p => {
                    }
                );
            }
        });

        this.showSocialScoreBar = (CompanySettings.ShowSocialScoreBar != 'false');
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.routeParamsSubscription.unsubscribe();
        this.currentAreaNameSubscription.unsubscribe();
        this.treeSub.unsubscribe();
    }

    buildBreadcrumb() {
        this.headerBreadcrumbService.getFolderTitle('#Policy').then((res) => {
            this.headerBreadcrumbService.clearBreadcrumbs();
            this.crumbs = [];
            let areaBreadcrumb = new Breadcrumb(
                this.currentAreaName ? this.currentAreaName : res, `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${SiteUrlHelpers.SITE_URL_POLICY_CLASSIFICATION}`
            );
            this.headerBreadcrumbService.showBreadcrumb(areaBreadcrumb);
            this.headerBreadcrumbService.showBreadcrumb(
                new Breadcrumb(
                    this.policyType.Name,
                    SiteUrlHelpers.getObjectUrl('PolicyType', this.policyType.ID), undefined, 'POLICYTYPE', this.policyTypeId, undefined, undefined, true
                )
            );

            if (this.selected && this.selected.ID > 0) {
                this.setObjectInfo('Policy', this.selected.ID, this.selected.DisplayValue, this.selected.AssetID, undefined, this.selected.Uid);
                this.checkParent(this.selected);
                this.headerBreadcrumbService.showBreadcrumb(
                    new Breadcrumb(
                        this.selected.DisplayValue,
                        undefined,
                        true,
                        'Policy',
                        this.selected.ID,
                        this.buildTreeNodeArray(this.policies, this.selected.ParentID),
                        this.findSelectedTreeNode(this.selected.ID)));
                this.headerBreadcrumbService.getFolderIcon(areaBreadcrumb.text).then(icon => {
                    this.setCommonRightSideBar(
                        true,
                        this.hasPermission(Permission.ReadResponsibilities),
                        false,
                        true,
                        true,
                        this.hasPermission(Permission.ReadRelationships),
                        true
                    );
                    this.rightSidebarService.showHeader(true);
                    this.rightSidebarService.setCurrentArea(this.selected.DisplayValue, icon, 'Definition');
                    this.rightSidebarService.setCurrentObject('PolicyType', this.policyType.ID, 'Policy', this.selected.ID, false, this.selected.HasWorkflow, this.selected.Uid);
                    this.rightSidebarService.showItem(new RightSidebarItem('Scoring', 'Scoring', ['fa-sitemap'], `/sidebar/score/Policy/${this.selected.Uid}`, null, 6));
                    this.rightSidebarService.showItem(new RightSidebarItem('Comments', 'Comments', ['fa-comments'], `/sidebar/comments/Policy/${this.selected.ID}`, null, 31));
                    this.rightSidebarService.showItem(new RightSidebarItem('Actions', 'Actions', null, `/sidebar/actions/Policy/${this.selected.ID}`, null, 26));
                });
            }
        });
        this.currentAreaNameSubscription =
            this.headerBreadcrumbService
                .getAreaName('PolicyType', this.policyTypeId)
                .subscribe(result => {
                    this.currentAreaName = result
                    
                });
    }

    private checkParent(policyItem: Policy) {
        if (policyItem.ParentID > 0 && this.policies) {
            let parentAr = this.policies.filter(x => x.ID == policyItem.ParentID);
            let parent: Policy;
            if (parentAr.length > 0) {
                parent = parentAr[0];
                let crumb = new Breadcrumb(parent.DisplayValue,
                    SiteUrlHelpers.getObjectUrl('POLICY', parent.ID, this.policyTypeId),
                    true,
                    'Policy',
                    parent.ID,
                    this.buildTreeNodeArray(this.policies, parent.ParentID),
                    this.findSelectedTreeNode(parent.ID), false, false)
                this.crumbs.unshift(crumb);
                this.checkParent(parent);
            }
        } else {
            this.crumbs.forEach(x => this.headerBreadcrumbService.showBreadcrumb(x));
        }
    }

    load(hierarchyId: number): Promise<any> {
        return this.policiesService.getPolicyType(this.policyTypeId).toPromise()
            .then(
                result => {
                    this.policyType = result;
                    this.buildBreadcrumb();
                    this.loadPolicyItems(this.policyTypeId, hierarchyId).then(
                        n => {
                            this.setBrowserTitle(this.titleService, this.policyType.Name);
                        }
                    );
                }
            );
    }

    loadPolicyItems(policyTypeId: number, selectedHierarchyId: number): Promise<any> {
        return this.policiesService.getPolicies(policyTypeId).toPromise().then(
            r => {
                this.policies = r;

                this.treeNodeArray = this.buildTreeNodeArray(this.policies);
                this.selectPolicyHierarchy(selectedHierarchyId);
            }
        );
    }

    private editComplete(e: any) {
        this.load(e.ID);
    }

    private buildTreeNodeArray(
        policies: Policy[],
        Parent?: number,
        includeChildren?: boolean
    ): TreeNode[] {
        // find the root items then
        let rootNodes = policies.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));

        if (rootNodes.length == 0) {
            return null;
        }

        const res: TreeNode[] = [];

        for (let root of rootNodes) {
            res.push({
                label: root.DisplayValue,
                expanded: true,
                data: {
                    id: root.ID
                },
                children: (includeChildren ? this.buildTreeNodeArray(policies, root.ID) : null) // recursively find its children
            });
        }

        return res;
    }

    private selectPolicyHierarchy(selectedHierarchyId: number): Promise<any> {
        if (selectedHierarchyId > 0) {
            const selArray = this.policies.filter(x => x.ID == selectedHierarchyId);

            if (selArray.length > 0) {
                this.selected = selArray[0];
            } else {
                console.log('ERROR INVALID SELECTED HIERARCHY ID SPECIFIED.', selectedHierarchyId);

                this.selected = (this.policies.length && this.policies.length > 0) ? this.policies[0] : null;
            }
        } else {
            this.selected = (this.policies.length && this.policies.length > 0) ? this.policies[0] : null;
        }

        this.assetID = this.selected.AssetID;

        this.loadPermissions(this.permissionsService, StringConstants.ObjectPolicy, this.selected.ID).then(
            p => {
                this.clearSidebar();
                this.setCommonRightSideBar(
                    true,
                    this.hasPermission(Permission.ReadResponsibilities),
                    false,
                    true,
                    true,
                    this.hasPermission(Permission.ReadRelationships),
                    true
                );
            }
        );

        this.buildBreadcrumb();

        return Promise.resolve(null);
    }

    private findSelectedTreeNode(id: number): TreeNode {
        const nodes: TreeNode[] = [];

        // add root nodes
        for (let rNode of this.treeNodeArray) {
            nodes.push(rNode);
        }

        // do a breadth first search for the given treenode
        if (nodes.length == 0) {
            return;
        }

        let node = nodes[0];

        while (node) {
            if (node.data.id && node.data.id == id) {
                return node;
            }

            // push children
            if (node.children) {
                for (let cNode of node.children) {
                    nodes.push(cNode);
                }
            }

            // remove this node
            nodes.splice(0, 1);

            if (nodes.length == 0) {
                return null;
            }

            node = nodes[0];
        }
    }

    private showHierarchy(id: number) {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${this.policyTypeId};hierarchyId=${id}`);
        this.buildBreadcrumb();
    }
}
