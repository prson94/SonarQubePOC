import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FusionService } from '../../services/fusion.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { FusionConfigurationDetails, FusionAttributeType, Fusion  } from '../../models/fusion.model';
import { FusionStructureTreeComponent} from './fusion-structure-tree.component';
import { FusionAttributeFilter } from '../../models/fusion-attribute.model';
import { SecondaryNavItem } from '../../models/secondaryNav.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import { AuthenticationService } from '../../services/authentication.service';
import { TreeNode } from 'primeng/api';

declare var CompanySettings;

@Component({
    selector: 'd3s-fusion-item',
    templateUrl: './fusion-item.component.html',
    providers: [FusionService, PermissionsService],
})

export class FusionItemComponent extends BaseComponent implements OnInit, OnDestroy { 
    private routeParams: any;
    private getFusionConfiguration: any;
    private fusionId: number;
    private fusion: FusionConfigurationDetails;
    treeNodeArray: TreeNode[] = [];
    treeSub: any;
    private crumbs: Breadcrumb[] = [];

    private selectedFusionAttributeTypeId: number;
    private selectedFusionAttribute: any;
    private initialFusionAttributeId: number;

    @ViewChild(FusionStructureTreeComponent, {static: false}) private fusionTreeComponent: FusionStructureTreeComponent;
    
    constructor(private headerBreadcrumbService: HeaderBreadcrumbService,
            private route: ActivatedRoute,
            private router: Router,
            private fusionService: FusionService,
            protected secondaryNavService: SecondaryNavService,
            protected titleService: Title,
            protected permissionsService: PermissionsService,
            private authenticationService: AuthenticationService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Fusion');
                
        this.routeParams = this.route.params.subscribe((params) => {
            let newFusionID = +params['fusionId'];
            this.selectedFusionAttributeTypeId = +params['fusionAttributeTypeId'];
            this.initialFusionAttributeId = +params['fusionAttributeId'];

            if (this.fusionId != newFusionID) {
                this.fusionId = newFusionID;
                this.loadPermissions(this.permissionsService, StringConstants.ObjectFusion , this.fusionId);

                this.getFusionConfiguration = this.fusionService.getFusionConfiguration(this.fusionId).subscribe(
                    (result) => {
                        this.fusion = result;
                        this.setBrowserTitle(this.titleService, `Fusion - ${this.fusion.Name}`);
                        this.setObjectInfo('Fusion', this.fusionId, undefined, this.fusion.AssetID);
                        this.treeSub = this.headerBreadcrumbService.breadcrumbTreeSource$.subscribe(
                            (id) => {
                                this.changeFusionAttributeTypeId(id);
                            }
                        );
                        this.buildBreadcrumb();
                        this.isLoading = false;
                    }
                );
            }
            else {
                this.buildBreadcrumb();
            }
        });
    }   

    ngOnDestroy() {
        if (this.routeParams) {
            this.routeParams.unsubscribe();
        }
        if (this.getFusionConfiguration) {
            this.getFusionConfiguration.unsubscribe();
        }
        if (this.treeSub) {
            this.treeSub.unsubscribe();
        }
        this.clearSidebar();
    }

    private setRightSideBar(hasDashboard: boolean, isManual: boolean) {
        this.secondaryNavService.clearItems();
        this.setCommonSecondaryNavTabs(false, true, hasDashboard);

        this.secondaryNavService.showItem(new SecondaryNavItem('History', 'fusionhistory', ['fa-archive'], `/fusion/history/${this.fusionId}`));
    }
    
    private buildBreadcrumb() {  
        this.headerBreadcrumbService.getFolderTitle('#Fusion').then((res) => {
            this.headerBreadcrumbService.clearBreadcrumbs();
            this.crumbs = [];

            let areaBreadcrumb = new Breadcrumb(res ? res : 'Fusion');
            this.headerBreadcrumbService.showBreadcrumb(areaBreadcrumb);
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.fusion.Name, SiteUrlHelpers.getObjectUrl('FUSIONTYPE', this.fusionId), undefined, 'Fusion', this.fusionId, undefined, undefined, true));

            if (this.selectedFusionAttributeTypeId && this.fusionTreeComponent.fusionAttributeTypes) {
                this.treeNodeArray = this.buildTreeNodeArray(this.fusionTreeComponent.fusionAttributeTypes);
                this.addFusionAttributeTypeBreadcrumb(this.selectedFusionAttributeTypeId);
            }
            this.headerBreadcrumbService.getFolderIcon(areaBreadcrumb.text).subscribe(icon => {
                this.setRightSideBar(this.fusion.HasDashboards, this.fusion.Manual);
                this.secondaryNavService.setLocalHomeUrl(this.router.url);
                this.secondaryNavService.setCurrentArea(areaBreadcrumb.text, icon, 'Configuration');
                this.secondaryNavService.showHeader(true);

            });
        });
    }

    private addFusionAttributeTypeBreadcrumb(id: number) {        
        var items = this.fusionTreeComponent.fusionAttributeTypes.filter(x => x.ID == id);
        
        if (items.length > 0) {
            this.checkParent(items[0]);
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(items[0].Name,
                `/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};fusionAttributeTypeId=${items[0].ID}`, 
                undefined,
                'FusionAttribute',
                items[0].ID,
                this.buildTreeNodeArray(this.fusionTreeComponent.fusionAttributeTypes, items[0].ParentID),
                this.findSelectedTreeNode(items[0].ID),false));            
        }
    }

    private checkParent(modelItem: FusionAttributeType) {
        if (modelItem.ParentID > 0 && this.fusionTreeComponent.fusionAttributeTypes) {
            let parentAr = this.fusionTreeComponent.fusionAttributeTypes.filter(x => x.ID == modelItem.ParentID);
            let parent: FusionAttributeType;
            if (parentAr.length > 0) {
                parent = parentAr[0];
                let crumb = new Breadcrumb(parent.Name,
                    SiteUrlHelpers.getObjectUrl('FUSIONTYPEWITHFUSIONATTRIBUTETYPE', parent.ID, this.fusionId),
                    true,
                    'FusionAttribute',
                    parent.ID,
                    this.buildTreeNodeArray(this.fusionTreeComponent.fusionAttributeTypes, parent.ParentID),
                    this.findSelectedTreeNode(parent.ID),
                    false)
                this.crumbs.unshift(crumb);
                this.checkParent(parent);
            }
        } else {
            this.crumbs.forEach(x => this.headerBreadcrumbService.showBreadcrumb(x));
        }
    }

    private changeFusionAttributeTypeId(event) {
        if (event == this.selectedFusionAttributeTypeId) {
            this.buildBreadcrumb();
            return;
        }
        this.selectedFusionAttribute = null;
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};fusionAttributeTypeId=${event}`);
        this.buildBreadcrumb();
    }   

    protected updateTree(tree) {
        tree.load();
    }


    private buildTreeNodeArray(models: FusionAttributeType[], Parent?: number, includeChildren?: boolean): TreeNode[] {
        //find the root items then 

        let rootNodes = models.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));

        if (rootNodes.length == 0) return null;

        let res: TreeNode[] = [];

        for (let root of rootNodes) {
            res.push({
                label: root.Name,
                expanded: true,
                data: {
                    id: root.ID
                },
                children: (includeChildren ? this.buildTreeNodeArray(models, root.ID) : null) //recursively find its children
            });
        }

        return res;
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
}
