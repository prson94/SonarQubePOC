import {
    Component,
    OnInit,
    OnDestroy,
    ViewChild,
    OnChanges
} from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { TreeNode } from 'primeng/primeng';

import { Breadcrumb } from '../../models/breadcrumb.model';
import { Policy, PolicyType, PolicyStatus } from '../../models/policy.model';
import { GridColumn, GridField } from '../../models/grid-definition.model';

import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PoliciesService } from '../../services/policies.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { PermissionsService } from '../../services/permissions.service';
import { LevelsService } from '../../services/levels.service';
import { GridDefinitionService } from '../../services/grid-definition.service';

import { BaseComponent } from '../shared/base.component';

import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import { MessagesObservableService } from '../../services/messages-observable.service';

@Component({
    selector: 'd3s-policy-item-structure',
    templateUrl: './policy-item-structure.component.html',
    providers: [
        PoliciesService,
        GridDefinitionService,
        PermissionsService,
        LevelsService
    ]
})

export class PolicyItemStructureComponent extends BaseComponent implements OnInit, OnDestroy {
    routeParamsSubscription: any;
    private currentAreaNameSubscription: any;
    private currentAreaName: string;

    policyType: PolicyType;
    policies: Policy[] = [];
    levels: any[] = [];

    policyTypeId: number;
    treeNodeArray: TreeNode[] = [];
    selected: TreeNode;
    selectedParentID: number;
    selectedLevel: number;
    columns: GridColumn[] = [];
    fields: GridField[] = [];

    searchValue: string;

    showDelete = false;
    showEditor = false;
        
    @ViewChild("treeTable") treeTable: any;
    unfilteredTreeNode: TreeNode[] = [];


    constructor(
        protected titleService: Title,
        private headerActionsService: HeaderActionsService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        private policiesService: PoliciesService,
        private route: ActivatedRoute,
        private router: Router,
        private messagesService: MessagesObservableService,
        rightSidebarService: RightSidebarService,
        private permissionsService: PermissionsService,
        private levelsService: LevelsService,
        private gridDefinitionService: GridDefinitionService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;        
        router.events.subscribe(
            (value) => {
                this.showEditor = false;
            }
        );
    }

    filter(event) {
        this.filterTreeTable(this.unfilteredTreeNode, event.target.value, this.treeTable);
    }

    ngOnInit() {
        this.routeParamsSubscription = this.route.params.subscribe(
            params => {
                this.policyTypeId = +params['policyTypeId'];
                this.headerBreadcrumbService.setCurrentObjectInfo('PolicyType', this.policyTypeId);


                this.setObjectInfo('PolicyType', this.policyTypeId);
                this.clearSidebar();
                this.setCommonRightSideBar(true);
                this.getFieldsDefinition();
                this.loadPermissions(this.permissionsService, StringConstants.ObjectPolicyType, this.policyTypeId);

                this.isLoading = true;
                this.policiesService.getPolicyType(this.policyTypeId).subscribe(
                    result => {

                        this.policyType = result;
                        this.currentAreaNameSubscription =
                            this.headerBreadcrumbService
                                .getAreaName('PolicyType', this.policyTypeId)
                                .subscribe(result => {
                                    this.currentAreaName = result
                                    this.headerBreadcrumbService.getFolderTitle('#Policy').then((res) => {
                                        this.headerBreadcrumbService.clearBreadcrumbs();
                                        this.headerBreadcrumbService.showBreadcrumb(
                                            new Breadcrumb(
                                                this.currentAreaName ? this.currentAreaName : res,
                                                `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${SiteUrlHelpers.SITE_URL_POLICY_CLASSIFICATION}`
                                            )
                                        );
                                        this.headerBreadcrumbService.showBreadcrumb(
                                            new Breadcrumb(
                                                this.policyType.Name,
                                                SiteUrlHelpers.getObjectUrl('POLICYTYPE', this.policyTypeId),
                                                undefined, 'POLICYTYPE', this.policyTypeId, undefined, undefined, true)
                                        );
                                        this.headerBreadcrumbService.getFolderIcon(this.currentAreaName ? this.currentAreaName : res).then(icon => {
                                            this.rightSidebarService.showHeader(true);
                                            this.rightSidebarService.setCurrentArea(this.policyType.Name, icon, 'Policy');
                                            this.rightSidebarService.setCurrentObject('PolicyType', this.policyType.ID, null, null, true);
                                        });
                                    });
                                });

                        this.loadPolicyHierarchy(this.policyTypeId);
                        this.setBrowserTitle(this.titleService, this.policyType.Name);

                        this.isLoading = false;
                    }
                );
                this.levelsService.getObjectLevels(this.policyTypeId, StringConstants.ObjectPolicyType).subscribe(
                    result => {
                        this.levels = result;
                    }
                );
            }
        );
    }

    ngOnDestroy() {
        this.clearSidebar();
        this.routeParamsSubscription.unsubscribe();
        this.currentAreaNameSubscription.unsubscribe();
    }

    private loadPolicyHierarchy(policyTypeId: number) {
        this.policiesService.getPolicies(policyTypeId, true).subscribe(
            result => {

                for (let policy of result) {
                    policy.StatusName = PolicyStatus[policy.Status];
                }

                this.policies = result;
                this.treeNodeArray = this.buildTreeNodeArray(this.policies, 1);
                this.unfilteredTreeNode = JSON.parse(JSON.stringify(this.treeNodeArray));
            }
        );
    }

    private buildTreeNodeArray(models: Policy[], levelNumber: number, Parent?: number): TreeNode[] {
        // find the root items then
        const rootNodes = models.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));

        if (rootNodes.length == 0) {
            return null;
        }

        const res: TreeNode[] = [];

        for (let root of rootNodes) {
            root.Level = levelNumber;
            res.push({
                label: root.DisplayValue,
                expanded: false,
                data: root,
                // recursively find its children
                children: (this.buildTreeNodeArray(models, levelNumber + 1, root.ID))
            });
        }

        return res;
    }

    private showHierarchy(id: number) {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${this.policyTypeId};hierarchyId=${id}`);
    }

    private getFieldsDefinition() {
        this.gridDefinitionService.getGridDefinition(this.policyTypeId, StringConstants.ObjectPolicyType).subscribe(
            result => {
                this.columns = result.Columns;
                this.fields = result.Fields;
            }
        );
    }

    setTreeNodeStyles(node) {
        if (!node.data) {
            return null;
        }

        const styles = {
            'font-weight': node.data.hasRelations ? 'bold' : 'normal',
        };

        return styles;
    }

    public onDeleted() {
        this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was removed        
        this.deleteSelectedTreeNode(this.selected.data.ID);
        this.selected = null;
        this.showDelete = false;
    }

    private add() {
        this.selectedParentID = this.selected ? this.selected.data.ID : null;
        this.selectedLevel = this.selected ? this.selected.data.Level : 0;
        this.selected = null;
        this.showEditor = true;
    }

    private policyEditorTitle(): string {
        if (!this.selected) {
            let thisLevel = this.levels.filter(x => x.Level == this.selectedLevel + 1);

            if (thisLevel && thisLevel.length > 0) {
                return thisLevel[0].Name;
            } else {
                return `(Level ${this.selectedLevel + 1}) Item`;
            }
        }

        let thisLevel = this.levels.filter(x => x.Level == this.selected.data.Level);

        if (thisLevel && thisLevel.length > 0) {
            return thisLevel[0].Name;
        }

        return `(Level ${this.selected.data.Level + 1}) Item`;
    }

    private deleteSelectedTreeNode(id: number): TreeNode {
        let nodes: TreeNode[] = [];

        // add root nodes
        for (let i = 0; i < this.treeNodeArray.length; i++) {
            if (this.treeNodeArray[i].data.ID && this.treeNodeArray[i].data.ID == id) {
                this.treeNodeArray.splice(i, 1);

                return;
            }

            nodes.push(this.treeNodeArray[i]);
        }

        // do a breadth first search for the given treenode
        if (nodes.length == 0) {
            return;
        }

        let node = nodes[0];

        while (node) {
            if (node.data.ID && node.data.ID == id) {
                return node;
            }

            // push children
            if (node.children) {
                for (let i = 0; i < node.children.length; i++) {
                    if (node.children[i].data.ID && node.children[i].data.ID == id) {
                        node.children.splice(i, 1);

                        return;
                    }

                    nodes.push(node.children[i]);
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

    private savePolicy(event) {
        this.isLoading = true;

        this.policiesService.savePolicy(event.item).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.headerActionsService.emitFavoritesChange();
                this.loadPolicyHierarchy(this.policyTypeId);
                this.showEditor = false;

                this.isLoading = false;
            }
        );
    }
}
