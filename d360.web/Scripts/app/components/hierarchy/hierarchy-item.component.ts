import { Component, OnDestroy, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ActivatedRoute, Router } from '@angular/router';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { PermissionsService } from '../../services/permissions.service';
import { ModelsService } from '../../services/models.service';
import { PoliciesService } from '../../services/policies.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { AssetTypeClass } from '../../models/asset.model';
import { StringConstants } from '../../static/string-constants';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { TreeNode } from 'primeng/api';
import { MessageBarItem } from '../../models/message-bar-item.model';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { CompanySettingsService } from '../../services/settings.service';
import { CompanySettingEnum } from '../../models/settings.model';
import { AssetDetailClickType, LinkClickInterceptor } from '../../services/href-click-service';
import { Subscription } from 'rxjs';

@Component({
    selector: 'd3s-hierarchy-item',
    providers: [
        ModelsService,
        PoliciesService,
        PermissionsService,
        WebAnalyticsService,
    ],
    templateUrl: 'hierarchy-item.component.html'
})

export class HierarchyItemComponent extends BaseComponent implements OnInit, OnDestroy {
    treeSub: any;
    routeSub: any;
    currentAreaNameSub: any;
    currentAreaName: string;
    showSocialScoreBar: boolean;

    object: string;
    objectTypeId: number;
    assetTypeClass: AssetTypeClass;

    selected: any;
    assetType: any;
    treeNodeArray: TreeNode[] = [];
    crumbs: Breadcrumb[] = [];
    messages: MessageBarItem[] = [];

    hrefSub: Subscription;
    selectedAsset: any;
    selectedTag: any;
    selectedReferenceItem: any;

    sidePanelOpen: boolean = false;
    sidePanelStorageKey;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        secondaryNavService: SecondaryNavService,
        protected modelsService: ModelsService,
        protected policiesService: PoliciesService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected permissionsService: PermissionsService,
        protected settingsService: CompanySettingsService,
        webAnalyticsService: WebAnalyticsService,
        private linkClickInterceptor: LinkClickInterceptor,
    ) {
        super(settingsService);

        this.webAnalyticsService = webAnalyticsService;
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = headerBreadcrumbService;
    }

    ngOnInit() {
        let type = this.route.parent.snapshot.data.type;

        switch (type) {
            case SiteUrlHelpers.SITE_URL_MODEL_ROOT:
                this.assetTypeClass = AssetTypeClass.Model;
                this.objectType = StringConstants.ObjectTaxonomyType;
                this.object = StringConstants.ObjectTaxonomy;
                this.objectName = 'Model';
                break;
            case SiteUrlHelpers.SITE_URL_POLICY_ROOT:
                this.assetTypeClass = AssetTypeClass.Policy;
                this.objectType = StringConstants.ObjectPolicyType;
                this.object = StringConstants.ObjectPolicy;
                this.objectName = 'Policy';
                break;
        }

        this.treeSub = this.headerBreadcrumbService.breadcrumbTreeSource$.subscribe(
            id => {
                this.selectHierarchy(id);
                this.showHierarchy(id);
            });

        this.routeSub = this.route.params.subscribe(params => {
            let newObjectTypeId = +params['typeId'];
            let hierarchyId = +params['id'];// if hierarchyId is passed via alternative route to workaround bug with router escaping ; = and other chars.

            this.currentAreaNameSub =
                this.headerBreadcrumbService
                    .getAreaName(this.objectType, newObjectTypeId)
                    .subscribe(result => { this.currentAreaName = result; if (this.assetType) this.buildBreadcrumb(); });

            if (!hierarchyId)
                hierarchyId = params['hierarchyId'] ? +params['hierarchyId'] : 0;

            this.logAction("open", this.object, hierarchyId);
            if (this.objectTypeId != newObjectTypeId || (this.selected == undefined || this.selected.ID != hierarchyId)) {
                this.objectTypeId = newObjectTypeId;
                this.isLoading = true;
                this.load(hierarchyId);

                this.isLoading = false;
            }
        });

        this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
            this.selectedAsset = null;
            this.selectedReferenceItem = null;
            this.selectedTag = null;

            if (ev.type === AssetDetailClickType.Asset) {
                this.selectedAsset = { uid: ev.uid, type: ev.objectType };
            }

            if (ev.type === AssetDetailClickType.ReferenceItem) {
                this.selectedReferenceItem = { uid: ev.assetTypeUid, assetUid: ev.uid, type: ev.objectType };
            }

            if (ev.type === AssetDetailClickType.Tag) {
                this.selectedTag = { uid: ev.uid };
            }

            if (ev.type === AssetDetailClickType.User || ev.type === AssetDetailClickType.Group) {
                this.selectedAsset = { uid: ev.uid, type: ev.objectType };
            }
        });

        this.showSocialScoreBar = this.settingsService.getSettingById(CompanySettingEnum.ShowSocialScoreBar).BooleanSetting.Value;
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private load(hierarchyId: number) {
        switch (this.assetTypeClass) {
            case AssetTypeClass.Model:
                this.modelsService.getModel(this.objectTypeId)
                    .subscribe(result => {
                        this.assetType = result;
                        this.loadHierarchy(this.objectTypeId, hierarchyId);
                        this.buildBreadcrumb();
                    });
                break;
            case AssetTypeClass.Policy:
                this.policiesService.getPolicyType(this.objectTypeId)
                    .subscribe(result => {
                        this.assetType = result;
                        this.loadHierarchy(this.objectTypeId, hierarchyId);
                        this.buildBreadcrumb();
                    });
                break;
        }


    }

    private editComplete(e: any) {
        this.load(e.ID);
    }

    private showHierarchy(id: number) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl(this.object, id, this.objectTypeId));
        this.buildBreadcrumb();
    }

    private buildBreadcrumb() {
        if (this.selected) {
            if (this.selected.DisplayValue) {
                this.buildSecondaryNavigation(this.selected.Uid, null, this.object, null, null, null, this.assetTypeClass, this.selected.DisplayValue);
            }
            else {
                this.buildSecondaryNavigation(this.selected.Uid, null, this.object, null, null, null, this.assetTypeClass);
            }
        }
    }

    private loadHierarchy(id: number, selectedHierarchyId: number): void {

        switch (this.assetTypeClass) {
            case AssetTypeClass.Model:
                this.modelsService.getModelHierarchy(id).subscribe(result => {
                    this.preloadedTreeData = result;

                    this.treeNodeArray = this.buildTreeNodeArray(this.preloadedTreeData);

                    this.selectHierarchy(selectedHierarchyId);
                    this.messages = []; //clear any messages for this model

                    this.setBrowserTitle(this.titleService, this.assetType.Name);
                });
                break;
            case AssetTypeClass.Policy:
                this.policiesService.getPolicies(this.objectTypeId)
                    .subscribe(result => {
                        this.preloadedTreeData = result;
                        this.baseTreeNodeArray = this.buildTreeNodeArrayBase(this.preloadedTreeData);
                        this.selectHierarchy(selectedHierarchyId);
                    });
                break;
        }


    }

    private selectHierarchy(selectedHierarchyId: number): Promise<void> {
        if (selectedHierarchyId > 0) {
            let selArray = this.preloadedTreeData.filter(x => x.ID == selectedHierarchyId);
            if (selArray.length > 0) this.selected = selArray[0];
            else {
                this.selected = (this.preloadedTreeData.length && this.preloadedTreeData.length > 0) ? this.preloadedTreeData[0] : null;
            }
        } else {
            this.selected = (this.preloadedTreeData.length && this.preloadedTreeData.length > 0) ? this.preloadedTreeData[0] : null;
        }

        this.assetID = this.selected.AssetID;

        this.loadPermissions(this.permissionsService, this.object, this.selected.ID);
        this.buildBreadcrumb();

        return Promise.resolve(null);
    }

    private buildTreeNodeArray(assets: any[], Parent?: number, includeChildren?: boolean): TreeNode[] {
        //find the root items then 
        includeChildren = includeChildren == undefined ? true : false;
        let rootNodes = assets.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));

        if (rootNodes.length == 0) return null;

        let res: TreeNode[] = [];

        for (let root of rootNodes) {
            res.push({
                label: root.DisplayValue,
                expanded: true,
                data: {
                    id: root.ID, hasRelations: root.HasChildren, AssetID: root.AssetID
                },
                children: (includeChildren ? this.buildTreeNodeArray(assets, root.ID) : null) //recursively find its children
            });
        }

        return res;
    }
}