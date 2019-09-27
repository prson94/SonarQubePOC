import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FusionService } from '../../services/fusion.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { FusionConfigurationDetails, FusionAttributeType, Fusion  } from '../../models/fusion.model';
import { FusionStructureTreeComponent} from './fusion-structure-tree.component';
import { FusionAttributeFilter } from '../../models/fusion-attribute.model';
import { RightSidebarItem } from '../../models/rightsidebar.model';
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

    private selectedFusionQueryAttributeTypeId: number;
    private selectedFusionQueryAttribute: any;
    private initialFusionQueryAttributeId: number;

    private isQueryConfigVisible: boolean = false;

    @ViewChild(FusionStructureTreeComponent) private fusionTreeComponent: FusionStructureTreeComponent;
    
    constructor(private headerBreadcrumbService: HeaderBreadcrumbService,
            private route: ActivatedRoute,
            private router: Router,
            private fusionService: FusionService,
            protected rightSidebarService: RightSidebarService,
            protected titleService: Title,
            protected permissionsService: PermissionsService,
            private authenticationService: AuthenticationService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Fusion');
                
        this.routeParams = this.route.params.subscribe(params => {
            let newFusionID = +params['fusionId'];
            this.selectedFusionAttributeTypeId = +params['fusionAttributeTypeId'];
            this.initialFusionAttributeId = +params['fusionAttributeId'];            
            this.selectedFusionQueryAttributeTypeId = +params['fusionQueryAttributeTypeId'];
            this.initialFusionQueryAttributeId = +params['fusionQueryAttributeId'];
            this.isQueryConfigVisible = params['showQueryConfig'] == 'true';         

            if (this.fusionId != newFusionID) {
                this.fusionId = newFusionID;
                this.loadPermissions(this.permissionsService, StringConstants.ObjectFusion , this.fusionId);

                this.getFusionConfiguration = this.fusionService.getFusionConfiguration(this.fusionId).subscribe(
                    result => {
                        this.fusion = result;
                        console.log(result);
                        this.setBrowserTitle(this.titleService, `Fusion - ${this.fusion.Name}`);
                        this.setObjectInfo('Fusion', this.fusionId, undefined, this.fusion.AssetID);
                        this.treeSub = this.headerBreadcrumbService.breadcrumbTreeSource$.subscribe(
                            id => {
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
        this.routeParams.unsubscribe();
        this.getFusionConfiguration.unsubscribe();
        this.treeSub.unsubscribe();
        this.clearSidebar();
    }

    private setRightSideBar(hasDashboard: boolean, isManual: boolean) {
        this.rightSidebarService.clearItems();
        this.setCommonRightSideBar(false, true, hasDashboard);

        this.rightSidebarService.showItem(new RightSidebarItem('History', 'fusionhistory', ['fa-archive'], `/fusion/history/${this.fusionId}`));

        if (isManual) this.rightSidebarService.showItem(new RightSidebarItem('Load Data', 'fusionload', ['fa-file-excel-o'], `/fusion/manual/load/${this.fusionId}`));           
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
            else if (this.selectedFusionQueryAttributeTypeId && this.fusionTreeComponent.fusionQueryAttributeTypes) {
                this.addFusionQueryAttributeTypeBreadcrumb(this.selectedFusionQueryAttributeTypeId);
            }
            else if (this.isQueryConfigVisible) {
                this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Fusion Query Configuration', `/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};showQueryConfig=true`));
            }
            this.headerBreadcrumbService.getFolderIcon(areaBreadcrumb.text).then(icon => {
                this.setRightSideBar(this.fusion.HasDashboards, this.fusion.Manual);
                this.rightSidebarService.setCurrentArea(areaBreadcrumb.text, icon, 'Configuration');
                this.rightSidebarService.showHeader(true);
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

    private addFusionQueryAttributeTypeBreadcrumb(id: number) {
        var items = this.fusionTreeComponent.fusionQueryAttributeTypes.filter(x => x.ID == id);

        if (items.length > 0) {
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Fusion Query Configuration', `/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};showQueryConfig=true`));
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(
                items[0].Name,
                `/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};fusionQueryAttributeTypeId=${items[0].ID}`));
        }
    }
    
    private changeFusionAttributeTypeId(event) {
        if (event == this.selectedFusionAttributeTypeId) {
            this.buildBreadcrumb();
            return;
        }
        this.selectedFusionAttribute = null;
        this.selectedFusionQueryAttribute = null;
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};fusionAttributeTypeId=${event}`);
        this.buildBreadcrumb();
    }   

    private showQueryConfig(val) {
        if(val) this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};showQueryConfig=true`);
    }

    private changeFusionQueryAttributeTypeId(event) {
        this.selectedFusionAttribute = null;
        this.selectedFusionQueryAttribute = null;
        this.router.navigateByUrl(`/${SiteUrlHelpers.SITE_URL_FUSION_ROOT}/${this.fusionId};fusionQueryAttributeTypeId=${event}`);
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
