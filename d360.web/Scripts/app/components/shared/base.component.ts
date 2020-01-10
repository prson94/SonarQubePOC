import { Title } from '@angular/platform-browser';
import { SecondaryNavItem, SecondaryNavCurrentObject, SecondaryNavPostModel } from '../../models/secondaryNav.model';
import { PermissionsService } from '../../services/permissions.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';

import { Subscription } from 'rxjs';
import { FormHelpers } from '../../static/form-helpers';
import { JsonResult } from '../../models/jsonresult.model';
import { ApiResult, ErrorResponse } from '../../models/apiresult.model';
import { ResponsibilityTypeRelationPermission, Permission } from '../../models/responsibility-type.model';
import { HttpErrorResponse } from '@angular/common/http';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { TreeNode } from 'primeng/api';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { AssetTypeClass } from '../../models/asset.model';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteMenuService } from '../../services/site-menu.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { OnDestroy, OnInit } from '@angular/core';
import { Policy } from '../../models/policy.model';

declare var CompanySettings;

export class BaseComponent {
    public isLoading = false;
    public gridStateStorage: string = 'session';

    // current object info
    uid: string;
    assetID: number;
    assetTypeID: number;
    objectID: number;
    objectType: string;
    objectName: string;
    public preloadedTreeData: any[] = [];
    public baseCrumbs: Breadcrumb[] = [];
    public baseTreeNodeArray: any[] = [];

    // sidebar
    sidebarSubscription: Subscription;
    isVisitingSidebar = false;

    auditSidebar: SecondaryNavItem;
    ownershipSidebar: SecondaryNavItem;
    lineageSidebar: SecondaryNavItem;
    impactSidebar: SecondaryNavItem;
    relationsSidebar: SecondaryNavItem;
    monitorSidebar: SecondaryNavItem;
    fieldNav: SecondaryNavItem;
    dashboardSidebar: SecondaryNavItem;
    followersSidebar: SecondaryNavItem;
    childSidebar: SecondaryNavItem;
    scoreSidebar: SecondaryNavItem;
    commentsSidebar: SecondaryNavItem;
    actionsSidebar: SecondaryNavItem;
    // tabs

    lineageShowUsageOnly = false;

    // filter mode
    showSimpleFilter = true;

    // permissions
    // Ideally this should be an input so we dont have to copy / past it...
    // child classes that support permissions input....
    permissions: ResponsibilityTypeRelationPermission[] = [];

    // default paging options
    defaultPagingOptions: number[] = [10, 25, 50, 100];
    defaultInitialItemsPerPage = 10;

    protected secondaryNavService: SecondaryNavService = null;
    protected webAnalyticsService: WebAnalyticsService = null;
    protected breadcrumbsService: HeaderBreadcrumbService = null;

    protected setBrowserTitle(tileService: Title, area: string) {
        tileService.setTitle(`${CompanySettings.BrowserTitlePrefix} - ${area}`);
    }

    logAction(actionName: string, objectName: string, objectId: number) {
        if (this.webAnalyticsService) {
            this.webAnalyticsService.logActivity({
                Activity: actionName,
                ObjectId: objectId,
                ObjectName: objectName
            });
        }
    }

    /*permissions functionality */

    loadPermissions(
        permissionsService: PermissionsService,
        objectType: string,
        objectID: number
    ) {

        return permissionsService.getPermissions(objectID, objectType).toPromise().then(result => {
            this.permissions = result;
        });

    }

    loadPermissionsById(
        permissionsService: PermissionsService,
        assetID: number
    ) {

        return permissionsService.getPermissionsById(assetID).toPromise().then(result => {
            this.permissions = result;
        })

    }

    hasPermission(permission: number) {
        return ResponsibilityTypeRelationPermission.hasPermission(this.permissions, permission);
    }

    hasModifyResponsibilitiesPermissions(object: string) {
        return this.hasPermission(Permission.ModifyResponsibilities);
    }

    hasDeleteResponsibilitiesPermissions(object: string) {
        return this.hasPermission(Permission.DeleteResponsibilities);
    }

    hasModifyAssetPermissions() {
        return this.hasPermission(Permission.ModifyAsset);
    }

    hasDeleteAssetPermissions() {
        return this.hasPermission(Permission.DeleteAsset);
    }

    hasModifyRelationshipsPermissions() {
        return this.hasPermission(Permission.ModifyRelationships);
    }

    hasDeleteRelationshipsPermissions() {
        return this.hasPermission(Permission.DeleteRelationships);
    }

    hasModifyAttributesPermissions() {
        return this.hasPermission(Permission.ModifyAttributes);
    }

    hasDeleteAttributesPermissions() {
        return this.hasPermission(Permission.DeleteAttributes);
    }

    /*end permissions functionality*/



    checkSecondaryNavLocalStorage(checkLocal?: boolean) {
        if (this.secondaryNavService) {
            this.buildLocalStorage();
            this.secondaryNavService.rebuildHeader$.subscribe(res => {
                if (res) {
                    window.setTimeout(() => {
                        this.buildLocalStorage();
                    }, 250);

                }
            });
        }
    }
    buildLocalStorage() {
        let currentObject = this.secondaryNavService.getLocalCurrentObject();
        let currentArea = this.secondaryNavService.getLocalCurrentArea();
        let tabs: SecondaryNavItem[] = this.secondaryNavService.getLocalCurrentTabs();
        let currentTab = this.secondaryNavService.getLocalActiveItem();
        let homeUrl = this.secondaryNavService.getLocalHomeUrl();
        let crumbs = this.breadcrumbsService.getBreadcrumbsFromStorage();
        if (currentObject && currentArea && tabs.length > 0 && currentTab && homeUrl) {
            this.secondaryNavService.clearItems();
            this.secondaryNavService.setCurrentObject(currentObject);
            this.secondaryNavService.setCurrentArea(currentArea.title, currentArea.icon, currentArea.tabTitle);
            this.secondaryNavService.setLocalHomeUrl(homeUrl);
            tabs.forEach(tab => {
                if (tab.title == currentTab.title)
                    tab.active = true;
                else
                    tab.active = false;
                this.secondaryNavService.showItem(tab);
            });
            this.secondaryNavService.showHeader(true);
        }
        if (crumbs.length > 0)
            this.breadcrumbsService.buildFromStorage();
    }

    setCommonSecondaryNavTabs(
        hasAudit?: boolean,
        hasOwnership?: boolean,
        hasDashboard?: boolean,
        hasLineage?: boolean,
        hasImpact?: boolean,
        hasRelationships?: boolean,
        hasFollowers?: boolean,
        hasMonitor?: boolean,
        hasField?: boolean,
        hasChild?: boolean
    ) {
        if (this.secondaryNavService) {
            this.clearSidebar();
            if (hasLineage && CompanySettings.ShowLineageSidebar != 'false') {

                let lineageVersion: number = 1;

                if (CompanySettings != null && CompanySettings.LineageVersion != null) {
                    lineageVersion = +CompanySettings.LineageVersion;
                }

                if (lineageVersion !== 3) {
                    const isLineageShowUsageOnly = this.lineageShowUsageOnly ? '/1' : '';
                    const urlLineage = this.objectContextUrl() + isLineageShowUsageOnly;

                    this.lineageSidebar = new SecondaryNavItem(
                        'Lineage',
                        'lineage',
                        ['fa-random'],
                        `/sidebar/visualization/lineage${urlLineage}`, null, 15
                    );
                }
                else {
                    this.lineageSidebar = new SecondaryNavItem(
                        'Visualization',
                        'lineage',
                        ['fa-random'],
                        `/sidebar/visualization/browser${this.uidContextUrl()}`, null, 15
                    );
                }
                this.secondaryNavService.showItem(this.lineageSidebar);
            }

            if (hasAudit || hasAudit === undefined) {
                this.auditSidebar = new SecondaryNavItem(
                    'Change Log',
                    'Change Log',
                    ['fa-eye'],
                    `/sidebar/audit${this.objectContextUrl()}`, null, 40
                );
                this.secondaryNavService.showItem(this.auditSidebar);
            }
            if (hasField) {
                this.fieldNav = new SecondaryNavItem(
                    'Field Definitions',
                    'fields',
                    ['fa-drivers-license-o'],
                    `/sidebar/fields/${this.objectType}/${this.objectID}`, null, 1);
                this.secondaryNavService.showItem(this.fieldNav);
            }

            if (hasOwnership && CompanySettings.ShowOwnersSidebar != 'false') {
                var urlPart = 'ownership';
                if (this.objectType == 'ReferenceItemType')
                    urlPart = 'responsibilities';

                this.ownershipSidebar = new SecondaryNavItem(
                    'Responsibilities',
                    urlPart,
                    ['fa-user'],
                    `/sidebar/${urlPart}/${this.assetID}`, null, 25
                );
                this.secondaryNavService.showItem(this.ownershipSidebar);
            }

            if (hasDashboard) {
                this.dashboardSidebar = new SecondaryNavItem(
                    'Dashboards',
                    'dashboards',
                    ['fa-tachometer'],
                    `/dashboard${this.objectContextUrl()}`, null, 5
                );

                this.secondaryNavService.showItem(this.dashboardSidebar);
            }

            if (hasImpact && CompanySettings.ShowImpactSidebar != 'false' && CompanySettings.LineageVersion != 3) {
                this.impactSidebar = new SecondaryNavItem(
                    'Impact',
                    'impact',
                    ['fa-exchange'],
                    `/sidebar/visualization/impact${this.objectContextUrl()}`, null, 10
                );
                this.secondaryNavService.showItem(this.impactSidebar);
            }

            if (hasRelationships) {
                this.relationsSidebar = new SecondaryNavItem(
                    'Related Assets',
                    'relationship',
                    ['fa-retweet'],
                    `/sidebar/relationships${this.objectContextUrl()}`, null, 20
                );
                this.secondaryNavService.showItem(this.relationsSidebar);
            }

            if (hasFollowers && CompanySettings.ShowFollowersSidebar != 'false') {
                this.followersSidebar = new SecondaryNavItem(
                    'Followers',
                    'followers',
                    ['fa-bookmark-o'],
                    `/sidebar/followers${this.objectContextUrl()}`, null, 35
                );
                this.secondaryNavService.showItem(this.followersSidebar);
            }

            if (hasMonitor) {
                this.monitorSidebar = new SecondaryNavItem(
                    'Workflow',
                    'monitor',
                    ['fa-usb'],
                    `/sidebar/workflowmonitor${this.objectContextUrl()}`, null, 30
                );
                this.secondaryNavService.showItem(this.monitorSidebar);
            }

            if (hasChild) {
                this.childSidebar = new SecondaryNavItem(
                    'Children',
                    'children',
                    ['fa-sitemap'],
                    `/sidebar/children${this.objectContextUrl()}`
                );

                this.secondaryNavService.showItem(this.childSidebar);
            }

            if (this.objectType == 'Artifact' || this.objectType == 'Policy' || this.objectType == 'Taxonomy' || this.objectType == 'Rule') {
                this.scoreSidebar = new SecondaryNavItem(
                    'Scoring',
                    'Scoring',
                    ['fa-sitemap'],
                    `/sidebar/score/${this.objectType}/${this.uid}`, null, 7
                );

                this.secondaryNavService.showItem(this.scoreSidebar);

                this.commentsSidebar = new SecondaryNavItem(
                    'Comments', 'Comments', ['fa-comments'],
                    `/sidebar/comments/${this.objectType}/${this.assetID}`, null, 33
                );

                this.secondaryNavService.showItem(this.commentsSidebar);

                this.actionsSidebar = new SecondaryNavItem(
                    'Actions', 'Actions', null,
                    `/sidebar/actions/${this.objectType}/${this.assetID}`, null, 27
                );
                this.secondaryNavService.showItem(this.actionsSidebar);
            }

            this.sidebarSubscription = this.secondaryNavService.rightSidebarClicked$.subscribe(
                item => {
                    this.isVisitingSidebar = true;
                    this.showHideBreadcrumbItem(item);
                });
        }
    }

    setObjectInfo(
        objectType: string,
        objectID: number,
        objectName?: string,
        assetID?: number,
        assetTypeID?: number,
        uid?: string
    ) {
        this.assetID = assetID;
        this.assetTypeID = assetTypeID;
        this.objectType = objectType;
        this.objectID = objectID;
        this.uid = uid;

        if (objectName != undefined) {
            this.objectName = objectName;
        }
    }

    assetContextUrl(): string {
        const url = '';

        if (!this.assetID) {
            return url;
        }

        return `/item/${this.assetID}`;
    }

    assetTypeContextUrl(): string {
        const url = '';

        if (!this.assetTypeID) {
            return url;
        }

        return `/type/${this.assetTypeID}`;
    }

    objectContextUrl(): string {
        const url = '';

        if (!this.objectType || !this.objectID) {
            return url;
        }

        return `/${this.objectType}/${this.objectID}`;
    }

    uidContextUrl(): string {
        const url = '';

        if (!this.uid) {
            return url;
        }

        return `/${this.uid}`;
    }

    private findSelectedTreeNodeBase(id: number): TreeNode {
        const nodes: TreeNode[] = [];

        // add root nodes
        for (let rNode of this.baseTreeNodeArray) {
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

    //generic method used for objectName = Policy/Model
    private checkParentBase(item: any, arr: any[], policyTypeId: number, objectName: string) {

        if (item.ParentID > 0 && arr) {
            let parentAr = arr.filter(x => x.ID == item.ParentID);
            console.log(parentAr);
            let parent: any;
            if (parentAr.length > 0) {
                parent = parentAr[0];
                let crumb = new Breadcrumb(parent.DisplayValue,
                    SiteUrlHelpers.getObjectUrl(objectName.toUpperCase(), parent.ID, policyTypeId),
                    true,
                    objectName,
                    parent.ID,
                    this.buildTreeNodeArrayBase(arr, parent.ParentID),
                    this.findSelectedTreeNodeBase(parent.ID), false, false)
                this.baseCrumbs.unshift(crumb);
                this.checkParentBase(parent, arr, policyTypeId, objectName);
            }
        } else {
            this.baseCrumbs.forEach(x => this.breadcrumbsService.showBreadcrumb(x));
        }
    }

    public buildTreeNodeArrayBase(
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
                children: (includeChildren ? this.buildTreeNodeArrayBase(policies, root.ID) : null) // recursively find its children
            });
        }

        return res;
    }

    // This is generally overloaded to show hide in your own class.
    protected showHideBreadcrumbItem(activatedItem: SecondaryNavItem) {
    }

    clearSidebar(unsubscribe?: boolean) {
        if (this.secondaryNavService) {
            if (!this.isVisitingSidebar) {
                this.secondaryNavService.clearItems();
                this.secondaryNavService.clearButtons();
            }

            if (this.sidebarSubscription && (unsubscribe || unsubscribe == undefined)) {
                this.sidebarSubscription.unsubscribe();
            }
        }
    }

    showMessageForResult(messagesService: MessagesObservableService, result: JsonResult, defaultMessage?: string) {
        if (defaultMessage == undefined) {
            defaultMessage = 'Success';
        }

        if (result.type == 'error') {
            messagesService.showError(result.title, result.message);
        } else {
            messagesService.showInfoMessage(
                result.title,
                result.message != null ? result.message : defaultMessage
            );
        }
    }

    showMessageForApiResponse(messagesService: MessagesObservableService, result: ApiResult & ErrorResponse, defaultMessage?: string) {
        if (defaultMessage == undefined) {
            defaultMessage = 'Success';
        }

        if (!result.Success) {
            messagesService.showError(result.Title == null ? 'Error' : result.Title, result.Message);
        } else {
            messagesService.showInfoMessage(
                'Success',
                result.Message != null ? result.Message : defaultMessage
            );
        }
    }

    showMessageForApiResult(messagesService: MessagesObservableService, result: ApiResult, defaultMessage?: string) {
        if (defaultMessage == undefined) {
            defaultMessage = 'Success';
        }

        if (!result.Success) {
            messagesService.showError('Error', result.Message);
        } else {
            messagesService.showInfoMessage(
                'Success',
                result.Message != null ? result.Message : defaultMessage
            );
        }
    }

    showMessageForApiResults(messagesService: MessagesObservableService, results: ApiResult[], defaultMessage: string, disableCountShow: boolean = false) {
        var succeeded = results.filter(x => x.Success == true);
        var failed = results.filter(x => x.Success != true);

        if (succeeded.length > 0) {
            let message = disableCountShow ? defaultMessage : succeeded.length + defaultMessage;
            messagesService.showInfoMessage('Success', message);
        }

        if (failed.length > 0) {
            failed.forEach(f => {
                messagesService.showError('Error', f.Message);
            });
        }
    }

    showHttpErrorMessage(messagesService: MessagesObservableService, err: HttpErrorResponse) {
        messagesService.showError('An error occurred', err.error);
    }

    public getLocaleDateString(): string {
        return FormHelpers.getLocaleDateString();
    }

    public filterTreeTable(originalArray: TreeNode[], search: string, tree: any) {
        var arrDeepCopy = originalArray.map(x => Object.assign({}, x));
        if (search.length == 0) {
            tree.value = arrDeepCopy;
            return;
        }
        else {
            let temp: TreeNode[] = [];
            arrDeepCopy.forEach(n => {
                if (this.doesNodeContainsValue(n, search)) {
                    temp.push(n);
                    this.expandTreeNode(n);
                }
            });

            tree.value = temp;
        }
    }

    expandTreeNode(node: TreeNode) {
        node.expanded = true;
        if (node.children) {
            node.children.forEach(n => this.expandTreeNode(n));
        }
    }

    doesNodeContainsValue(node: TreeNode, q: string): boolean {
        let hasValue: boolean = false;
        var nodeProps = Object.getOwnPropertyNames(node.data);

        var tempChildren = node.children;
        node.children = [];
        if (tempChildren) {
            tempChildren.forEach(n => {
                if (this.doesNodeContainsValue(n, q)) {
                    node.children.push(n);
                }
            });
        }
        if (node.children && node.children.length > 0) return true;

        nodeProps.forEach(prop => {
            if (prop.toLowerCase().indexOf("name") != -1 || prop.toLowerCase().indexOf("value") != -1 || prop.toLowerCase().indexOf("field") != -1) {
                if (node.data[prop] && node.data[prop].toString().toLowerCase().indexOf(q.toLowerCase()) != -1) hasValue = true;
            }
        });

        return hasValue;
    }

    buildSecondaryNavigationForAssetID(assetId: number, object: string) {
        this.buildSecondaryNavigation(null, null, object, assetId);
    }

    buildSecondaryNavigationForObject(objectId: number, object: string) {
        this.buildSecondaryNavigation(null, objectId, object);
    }

    buildSecondaryNavigation(assetUid: any = null, objectId: number = null, objectType: string = null, assetId: number = null, assetTypeUid: string = null) {
        console.log("Building secondary navigation!");

        var data = new SecondaryNavPostModel();
        data.PreloadData = false;
        if (assetUid)
            data.AssetUid = assetUid;

        if (objectId)
            data.ObjectId = objectId;

        if (objectType)
            data.ObjectType = objectType;

        if (assetId)
            data.AssetId = assetId;

        if (assetTypeUid)
            data.AssetTypeUid = assetTypeUid;

        if (!this.preloadedTreeData || this.preloadedTreeData.length == 0) {
            //This will have effect only on pages that need populate tree to create breadcrumbs (model, policy)
            data.PreloadData = true;
        }

        if (!assetUid && !assetId && !assetTypeUid && !objectId) {
            return;
        }



        this.secondaryNavService.getSiteMenuService().getSecondaryNav(data).subscribe(r => {
            this.clearSidebar();
            this.breadcrumbsService.clearBreadcrumbs();

            this.assetID = r.AssetId;
            this.assetTypeID = r.AssetTypeId;
            this.uid = r.Uid;
            this.objectType = r.Object;
            this.objectID = r.ObjectID;

            var areaName = r.DisplayValue;
            var mainTabTitle = r.MainTabTitle;
            if (r.PreloadData) {
                this.preloadedTreeData = r.PreloadData;
                if (r.PreloadData.Data) {
                    this.preloadedTreeData = r.PreloadData.Data;
                }
            }
            var area = "";

            area = ['Business Assets', 'Technical Assets', 'Artifacts', 'Attributes', 'Lookups', 'Models', 'Policies', 'Predicates', 'Relationships', 'Rules', 'Surveys', 'Workflow Actions', 'Workflows']
                .indexOf(areaName) !== -1 ? 'Configuration' : "Administration";

            var homeUrl = this.getUrl(r, areaName);
            console.log(this.objectType);
            this.secondaryNavService.setLocalHomeUrl(homeUrl);

            if (this.objectType.toLowerCase() == 'artifact') {
                this.setArtifactBreadcrumbs(r);
            }
            else if (this.objectType.toLowerCase() == 'policy') {
                this.setTreeBreadcrumbs(r, 'Policy');
            }
            else if (this.objectType.toLowerCase() == 'taxonomy') {
                this.setTreeBreadcrumbs(r, 'Taxonomy');
            }
            else if (this.objectType.toLowerCase() == 'rule') {
                this.setRuleBreadcrumbs(r);
            }
            else if (this.objectType.toLowerCase() == 'referenceitemtype') {
                this.breadcrumbsService.clearBreadcrumbs();

                this.breadcrumbsService.showBreadcrumb(new Breadcrumb('Reference Lists', homeUrl));
                this.setBrowserTitle(this.breadcrumbsService.getTitleService(), 'Reference Lists');
            }
            else {
                this.SetCommonBreadcrumbs(r, area, homeUrl);
            }

            this.secondaryNavService.clearItems();
            this.secondaryNavService.clearButtons();
            this.secondaryNavService.setCurrentArea(areaName, area === 'Configuration' ? 'fa-sliders' : "fa-cog", mainTabTitle);
            this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject(r.ObjectType, this.assetTypeID, this.objectType, this.objectID, false, r.Items.HasWorkflow, this.uid));
            this.secondaryNavService.showHeader(true);

            this.setCommonSecondaryNavTabs(r.Items.HasAudit, r.Items.HasOwnership, r.Items.HasDashboard, r.Items.HasLineage, r.Items.HasImpact, r.Items.HasRelationship, r.Items.HasFollowers, r.Items.HasWorkflow, r.Items.HasField, r.Items.HasChild);

            this.activateComponent();
        });
    }

    private SetCommonBreadcrumbs(data, area, url) {
        var adminHeading = '';

        if (data.DisplayValue == 'Responsibilities') {
            adminHeading = "Security";
        }

        this.breadcrumbsService.clearBreadcrumbs();
        this.breadcrumbsService.showBreadcrumb(new Breadcrumb(area));
        if (adminHeading)
            this.breadcrumbsService.showBreadcrumb(new Breadcrumb(adminHeading));

        this.breadcrumbsService.showBreadcrumb(new Breadcrumb(data.DisplayValue, url));
        this.setBrowserTitle(this.breadcrumbsService.getTitleService(), data.DisplayValue);
    }

    private getUrl(r: any, areaName: any) {
        if (this.objectType.toLowerCase() == "policy") {
            return "/" + this.objectType.toLowerCase() + "/" + r.ObjectTypeId + ";hierarchyID=" + this.objectID;
        }
        if (this.objectType.toLowerCase() == "taxonomy") {
            return "/policy/" + r.ObjectTypeId + ";hierarchyID=" + this.objectID;
        }
        if (this.objectType.toLowerCase() == "rule") {
            return "/quality/" + this.objectType.toLowerCase() + "/" + r.ObjectTypeId + "/" + this.objectID;
        }
        if (this.objectType.toLowerCase() == "referenceitemtype") {
            return "/reference;referenceListId=" + this.objectID;
        }
        if (this.objectType.toLowerCase() == "artifacttype" && areaName == 'Business Assets') {
            return "/admin/assets/BusinessAsset";
        }
        if (this.objectType.toLowerCase() == "artifacttype" && areaName == 'Technical Assets') {
            return "/admin/assets/TechnicalAsset";
        }
        if (this.objectType.toLowerCase() == "taxonomytype") {
            return "/admin/taxonomies";
        }
        if (this.objectType.toLowerCase() == "policytype") {
            return "/admin/policies";
        }
        if (this.objectType.toLowerCase() == "intersecttype") {
            return "/admin/relationships";
        }
        if (this.objectType.toLowerCase() == "issuetype") {
            return "/admin/issuetypes";
        }
        if (this.objectType.toLowerCase() == "attributetype") {
            return "/admin/attributes";
        }
        if (this.objectType.toLowerCase() == "lookuptype") {
            return "/admin/lookups";
        }
        if (this.objectType.toLowerCase() == "responsibilitytype") {
            return "/admin/responsibilities";
        }
        if (this.objectType.toLowerCase() == "report") {
            return "/admin/dashboard";
        }

        return "/" + this.objectType.toLowerCase() + "/" + r.ObjectTypeId + "/" + this.objectID;
    }

    private activateComponent() {
        var componentName = this.constructor.name;
        switch (componentName) {
            case "ScoreComponent": this.scoreSidebar.active = true;
                break;
            case "DashboardComponent": this.dashboardSidebar.active = true;
                break;
            case "BrowserComponent": this.lineageSidebar.active = true;
                break;
            case "RelationshipsComponent": this.relationsSidebar.active = true;
                break;
            case "OwnershipComponent": this.ownershipSidebar.active = true;
                break;
            case "ActionsComponent": this.actionsSidebar.active = true;
                break;
            case "MonitorWorkflowComponent": this.monitorSidebar.active = true;
                break;
            case "CommentsComponent": this.commentsSidebar.active = true;
                break;
            case "FollowersComponent": this.followersSidebar.active = true;
                break;
            case "AuditComponent": this.auditSidebar.active = true;
                break;
            case "ChildrenComponent": this.childSidebar.active = true;
                break;
            default: break;
        }
    }

    private setArtifactBreadcrumbs(data) {
        var artifact = data.Artifact;

        let folderName: string = '#Business';

        if (artifact.Class == AssetTypeClass.TechnicalAsset) {
            folderName = '#Technical';
        }
        this.breadcrumbsService.getFolderTitle(folderName).then(res => {
            this.breadcrumbsService.clearBreadcrumbs();

            var folderTitle = res;
            var area = res;

            let index = 0;
            this.breadcrumbsService
                .getAreaName('ArtifactType', data.Artifact.Breadcrumbs[0] ? this.GetIDFromUrl(data.Artifact.Breadcrumbs[0].Url) : data.Artifact.AssetTypeID)
                .subscribe(result => {
                    var currentAreaName = result;
                    let currentFolderName = currentAreaName ? currentAreaName : folderTitle;

                    this.breadcrumbsService.clearBreadcrumbs();
                    this.breadcrumbsService.getAssetFolderIcon('ArtifactType', data.Artifact.AssetTypeID, currentFolderName).subscribe(res => {
                        this.secondaryNavService.setCurrentArea(data.Artifact.DisplayValue, res, 'Definition');
                    });
                    let areaName: string = currentAreaName ? currentAreaName : folderTitle;
                    let areaLink: string = `${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${SiteUrlHelpers.SITE_URL_ASSETS_ROOT}`;
                    if (areaName == "Technical Assets") {
                        areaLink += `/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_TECHNICAL}`;
                    }
                    else {
                        areaLink += `/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_BUSINESS}`;
                    }
                    let areaBreadcrumb = new Breadcrumb(
                        areaName,
                        areaLink,
                        false
                    );
                    this.breadcrumbsService.showBreadcrumb(areaBreadcrumb);

                    for (let breadcrumb of data.Artifact.Breadcrumbs) {
                        index++;

                        if (index == data.Artifact.Breadcrumbs.length) {
                            //last item in the breadcrumb
                            this
                                .breadcrumbsService
                                .showBreadcrumb(
                                    new Breadcrumb(
                                        breadcrumb.Name,
                                        breadcrumb.Url,
                                        false,
                                        'Artifact',
                                        data.Artifact.AssetTypeID,
                                        null,
                                        null,
                                        false,
                                        breadcrumb.TypeName !== undefined,
                                        breadcrumb.TypeName,
                                        'ArtifactType',
                                        this.GetIDFromUrl(breadcrumb.TypeUrl),
                                        breadcrumb.TypeUrl
                                    )
                                )
                                ;
                        } else {
                            this
                                .breadcrumbsService
                                .showBreadcrumb(
                                    new Breadcrumb(
                                        breadcrumb.Name,
                                        breadcrumb.Url,
                                        false,
                                        'Artifact',
                                        this.GetIDFromUrl(breadcrumb.Url),
                                        null,
                                        null,
                                        false,
                                        breadcrumb.TypeName !== undefined,
                                        breadcrumb.TypeName,
                                        'ArtifactType',
                                        this.GetIDFromUrl(breadcrumb.TypeUrl),
                                        breadcrumb.TypeUrl
                                    )
                                )
                                ;
                        }
                    }
                });
        });


    }

    //used for policy/model
    private setTreeBreadcrumbs(data, objectName: string) {
        var selected = this.preloadedTreeData.find(x => x.ID == data.ObjectID);
        var objectTypeName = objectName + "Type";
        this.breadcrumbsService.clearBreadcrumbs();

        this.breadcrumbsService
            .getAreaName(objectTypeName, data.ObjectTypeId)
            .subscribe(result => {
                this.baseCrumbs = [];
                var currentAreaName = result;

                this.breadcrumbsService.getFolderTitle('#' + (objectName == 'Taxonomy' ? 'Models' : objectName)).then((res) => {

                    var folderTitle = res;
                    let currentFolderName = currentAreaName ? currentAreaName : folderTitle;

                    this.breadcrumbsService.getAssetFolderIcon(objectTypeName, data.AssetTypeID, currentFolderName).subscribe(res => {
                        this.secondaryNavService.setCurrentArea(data.DisplayValue, res, 'Definition');
                    });

                    this.breadcrumbsService.clearBreadcrumbs();
                    let areaBreadcrumb = new Breadcrumb(
                        currentAreaName ? currentAreaName : res, `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${SiteUrlHelpers.SITE_URL_POLICY_CLASSIFICATION}`
                    );
                    this.breadcrumbsService.showBreadcrumb(areaBreadcrumb);

                    this.breadcrumbsService.showBreadcrumb(
                        new Breadcrumb(
                            data.TypeName,
                            SiteUrlHelpers.getObjectUrl(objectTypeName, data.ObjectTypeId), undefined, objectTypeName.toUpperCase(), data.ObjectTypeId, undefined, undefined, true
                        )
                    );

                    this.setObjectInfo(objectName, data.ObjectId, data.DisplayValue, data.AssetID, undefined, data.Uid);

                    if (selected && selected.ID > 0) {
                        this.setObjectInfo(objectName, selected.ID, selected.DisplayValue, selected.AssetID, undefined, selected.Uid);
                        this.checkParentBase(selected, this.preloadedTreeData, data.ObjectTypeId, objectName);
                        this.breadcrumbsService.showBreadcrumb(
                            new Breadcrumb(
                                selected.DisplayValue,
                                SiteUrlHelpers.getObjectUrl(objectTypeName, selected.ID),
                                true,
                                objectName,
                                selected.ID,
                                this.buildTreeNodeArrayBase(this.preloadedTreeData, selected.ParentID),
                                this.findSelectedTreeNodeBase(selected.ID)));
                    }


                });
            });
    }


    private setRuleBreadcrumbs(data) {
        console.log(data);
        this.breadcrumbsService
            .getAreaName('RuleType', data.ObjectTypeId)
            .subscribe(result => {
                var currentAreaName = result;
                this.breadcrumbsService.getFolderTitle('#Data Quality').then((res) => {
                    this.breadcrumbsService.clearBreadcrumbs();
                    this.breadcrumbsService.showBreadcrumb(new Breadcrumb(currentAreaName ? currentAreaName : res, undefined));//SiteUrlHelpers.SITE_URL_RULE_ROOT
                    this.breadcrumbsService.showBreadcrumb(new Breadcrumb(data.TypeName, `${SiteUrlHelpers.SITE_URL_RULE_ROOT}/${data.ObjectTypeId}`,
                        undefined,
                        'RuleType',
                        data.ObjectTypeId,
                        undefined,
                        undefined,
                        true));
                    this.breadcrumbsService.showBreadcrumb(new Breadcrumb(data.DisplayValue,
                        SiteUrlHelpers.getObjectUrl('RULEIMPLEMENTATION', data.ObjectId, data.ObjectTypeId),
                        true,
                        'Rule',
                        data.ObjectId));

                    this.breadcrumbsService.getAssetFolderIcon('RuleType', data.ObjectTypeId, currentAreaName ? currentAreaName : res).subscribe(icon => {
                        this.secondaryNavService.setCurrentArea(data.DisplayValue, icon, 'Definition');

                    });

                });
            });

    }


    private GetIDFromUrl(url: string) {
        return +url.split("/")[url.split.length - 1];
    }
}
