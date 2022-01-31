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
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { ScoreType, ScoreTypeAllocation, ScoreTypeInfo } from '../../models/metrics.model';
import { StringConstants } from '../../static/string-constants';
import { CompanySettingsService } from '../../services/settings.service';
import { CompanySettingEnum } from '../../models/settings.model';

export class BaseComponent {
    public isLoading = false;
    public gridStateStorage: string = 'session';
    public maxExportRows = 0;

    readonly resourceTypeUid = '00000001-0000-0000-0000-A00000000011';
    readonly groupTypeUid = '00000001-0000-0000-0000-B00000000012';
    readonly metricAllocationUid = '00000001-0000-0000-0000-B00000000013';
    readonly predicateUid = '00000001-0000-0000-0000-B00000000014';

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

    public simpleSearchTooltipHTML: string = `<p>Type to provide a search term. Matches will be found where the value of any field starts with the term or terms provided.</p><p>You can also use wildcards for more control over how the term is matched.
*term* : Match on values which contain 'term'</p><p>All matches are case insensitive.</p>`;

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
    ruleResultSidebar: SecondaryNavItem;

    governanceRolesSidebar: SecondaryNavItem;
    connectorLabels: SecondaryNavItem;

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

    private getSecondaryNavigationSub: Subscription;

    constructor(
        protected settingsService: CompanySettingsService
    ) {
        this.maxExportRows = this.getNumberSetting(CompanySettingEnum.MaxExcelExportRows);
    }

    //#region Settings methods

    getBooleanSetting(id: CompanySettingEnum): boolean {
        return this.settingsService.getSettingById(id).BooleanSetting.Value;
    }
    getGuidSetting(id: CompanySettingEnum): string {
        return this.settingsService.getSettingById(id).GuidSetting.Value;
    }
    getNumberSetting(id: CompanySettingEnum): number {
        let setting = this.settingsService.getSettingById(id);
        if (setting && setting.NumberSetting) {
            return setting.NumberSetting.Value;
        }
        else {
            return null;
        }
    }
    getStringSetting(id: CompanySettingEnum): string {
        return this.settingsService.getSettingById(id).StringSetting.Value;
    }

    //#endregion

    protected setBrowserTitle(tileService: Title, area: string) {
        tileService.setTitle(`${this.getStringSetting(CompanySettingEnum.BrowserTitlePrefix)} - ${area}`);
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

    //#region permissions functionality

    loadPermissions(
        permissionsService: PermissionsService,
        objectType: string,
        objectID: number
    ) {

        return permissionsService.getPermissions(objectID, objectType).toPromise().then((result) => {
            this.permissions = result;
        });

    }

    loadPermissionsById(
        permissionsService: PermissionsService,
        assetID: number
    ) {

        return permissionsService.getPermissionsById(assetID).toPromise().then((result) => {
            this.permissions = result;
        })

    }

    hasPermission(permission: number) {
        return ResponsibilityTypeRelationPermission.hasPermission(this.permissions, permission);
    }

    hasAddResponsibilitiesPermissions(object: string) {
        return this.hasPermission(Permission.AddResponsibilities);
    }

    hasModifyResponsibilitiesPermissions(object: string) {
        return this.hasPermission(Permission.EditResponsibilities);
    }

    hasDeleteResponsibilitiesPermissions(object: string) {
        return this.hasPermission(Permission.DeleteResponsibilities);
    }

    hasAddAssetPermissions() {
        return this.hasPermission(Permission.AddAsset);
    }

    hasModifyAssetPermissions() {
        return this.hasPermission(Permission.EditAsset);
    }

    hasDeleteAssetPermissions() {
        return this.hasPermission(Permission.DeleteAsset);
    }

    hasAddRelationshipsPermissions() {
        return this.hasPermission(Permission.AddRelationships);
    }

    hasModifyRelationshipsPermissions() {
        return this.hasPermission(Permission.EditRelationships);
    }

    hasDeleteRelationshipsPermissions() {
        return this.hasPermission(Permission.DeleteRelationships);
    }

    //#endregion permissions functionality

    checkSecondaryNavLocalStorage(checkLocal?: boolean) {
        if (this.secondaryNavService) {
            this.buildLocalStorage();
            this.secondaryNavService.rebuildHeader$.subscribe((res) => {
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

        let isValidNav: boolean = tabs.some(x => x.url.toLowerCase() == this.secondaryNavService.getCurrentUrl().toLowerCase());

        if (isValidNav && currentArea && tabs.length > 0 && currentTab && homeUrl) {
            this.secondaryNavService.clearItems();
            if (currentObject)
                this.secondaryNavService.setCurrentObject(currentObject);
            this.secondaryNavService.setCurrentArea(currentArea.title, currentArea.icon, currentArea.tabTitle);
            this.secondaryNavService.setLocalHomeUrl(homeUrl);

            tabs.forEach((tab) => {
                if (tab.title == currentTab.title) {
                    tab.active = true;
                    this.secondaryNavService.setLocalActiveItem(tab);
                }
                else
                    tab.active = false;
                this.secondaryNavService.showItem(tab);
            });
            this.secondaryNavService.showHeader(true);
        }
        if (isValidNav && crumbs.length > 0)
            this.breadcrumbsService.buildFromStorage();
    }

    setScoringSecondaryNavTabs(assetTypeUid: string, selectedAllocationUid: string, allocations: ScoreTypeAllocation[]) {
        var baseUrl = `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SCORING}/${assetTypeUid}/`;

        if (this.secondaryNavService) {
            this.clearSidebar();

            let priority = 10;

            allocations.forEach((allocation) => {
                const navItem = new SecondaryNavItem(
                    ScoreTypeInfo.get(allocation.scoreType.toString()),
                    ScoreTypeInfo.get(allocation.scoreType.toString()),
                    [allocation.icon],
                    `/${baseUrl}${allocation.uid}`, null, priority
                );
                navItem.active = (selectedAllocationUid === allocation.uid);
                this.secondaryNavService.showItem(navItem);
                priority += 10;
            });
        }
    }


    setCommonSecondaryNavTabs(opts: {
        hasAudit?: boolean,
        hasOwnership?: boolean,
        hasDashboard?: boolean,
        hasLineage?: boolean,
        hasImpact?: boolean,
        hasRelationships?: boolean,
        hasFollowers?: boolean,
        hasMonitor?: boolean,
        hasField?: boolean,
        hasChild?: boolean,
        hasRuleResult?: boolean,
        hasGovernanceRoleSet?: boolean,
        hasProcessDiagram?: boolean,
        hasGroups?: boolean,
        hasFollowing?: boolean,
        hasItemOwn?: boolean
    }) {
        if (this.secondaryNavService && this.objectType) {
            this.clearSidebar();
            var isCommonAsset: boolean = this.objectType == 'Artifact' || this.objectType == 'Policy' || this.objectType == 'Taxonomy' || this.objectType == 'Rule';

            let showLineage = opts.hasLineage && this.getBooleanSetting(CompanySettingEnum.ShowLineageSidebar);
            let showImpact = opts.hasImpact && this.getBooleanSetting(CompanySettingEnum.ShowImpactSidebar);

            if (showLineage || showImpact || opts.hasProcessDiagram) {
                this.lineageSidebar = new SecondaryNavItem(
                    'Diagrams',
                    'lineage',
                    ['fa-random'],
                    `/sidebar/visualization/browser${this.uidContextUrl()}`, null, 15
                );

                this.lineageSidebar.subTabsUrl.push(`/sidebar/visualization/browser${this.uidContextUrl()}/Lineage`);
                this.lineageSidebar.subTabsUrl.push(`/sidebar/visualization/browser${this.uidContextUrl()}/Impact`);
                this.lineageSidebar.subTabsUrl.push(`/sidebar/visualization/browser${this.uidContextUrl()}/Process`);

                this.secondaryNavService.showItem(this.lineageSidebar);
            }

            if ((opts.hasAudit || opts.hasAudit === undefined) && this.getBooleanSetting(CompanySettingEnum.ShowChangeLogTab)) {
                this.auditSidebar = new SecondaryNavItem(
                    'Change Log',
                    'Change Log',
                    ['fa-eye'],
                    `/sidebar/audit${this.auditContextUrl()}`, null, 40
                );
                this.secondaryNavService.showItem(this.auditSidebar);
            }

            if (opts.hasField) {
                this.fieldNav = new SecondaryNavItem(
                    'Field Definitions',
                    'fields',
                    ['fa-drivers-license-o'],
                    `/sidebar/fields/${this.objectType}/${this.objectID}`, null, 1);
                this.secondaryNavService.showItem(this.fieldNav);
            }

            if (opts.hasOwnership && this.getBooleanSetting(CompanySettingEnum.ShowOwnersSidebar)) {
                if (this.objectType == 'ReferenceItemType') {
                    this.ownershipSidebar = new SecondaryNavItem(
                        'Responsibilities',
                        'responsibilities',
                        ['fa-user'],
                        `/sidebar/responsibilities${this.auditContextUrl()}`, null, 25
                    );
                }
                else {
                    this.ownershipSidebar = new SecondaryNavItem(
                        'Responsibilities',
                        'ownership',
                        ['fa-user'],
                        `/sidebar/ownership/${this.assetID}`, null, 25
                    );
                }
                this.secondaryNavService.showItem(this.ownershipSidebar);
            }

            if (opts.hasDashboard) {
                this.dashboardSidebar = new SecondaryNavItem(
                    'Dashboards',
                    'dashboards',
                    ['fa-tachometer'],
                    `/dashboard${this.objectContextUrl()}`, null, 5
                );

                this.secondaryNavService.showItem(this.dashboardSidebar);
            }

            if (opts.hasRelationships) {
                this.relationsSidebar = new SecondaryNavItem(
                    'Relationships',
                    'relationship',
                    ['fa-retweet'],
                    `/sidebar/relationships${this.objectContextUrl()}`, null, 20
                );
                this.secondaryNavService.showItem(this.relationsSidebar);
            }

            if (opts.hasFollowers && this.getBooleanSetting(CompanySettingEnum.ShowFollowersSidebar)) {
                this.followersSidebar = new SecondaryNavItem(
                    'Followers',
                    'followers',
                    ['fa-bookmark-o'],
                    `/sidebar/followers${this.objectContextUrl()}`, null, 35
                );
                this.secondaryNavService.showItem(this.followersSidebar);
            }

            if (opts.hasMonitor) {
                this.monitorSidebar = new SecondaryNavItem(
                    'Workflow',
                    'monitor',
                    ['fa-usb'],
                    `/sidebar/workflowmonitor${this.objectContextUrl()}`, null, 30
                );
                this.secondaryNavService.showItem(this.monitorSidebar);
            }

            if (opts.hasChild) {
                this.childSidebar = new SecondaryNavItem(
                    'Children',
                    'children',
                    ['fa-sitemap'],
                    `/sidebar/children${this.objectContextUrl()}`
                );

                this.secondaryNavService.showItem(this.childSidebar);
            }

            if (opts.hasRuleResult) {
                this.ruleResultSidebar = new SecondaryNavItem(
                    'Rule Results',
                    'Rule Results',
                    ['fa-sitemap'],
                    `/sidebar/ruleResults/${this.objectID}/${this.uid}`
                    , null, 1);
                this.secondaryNavService.showItem(this.ruleResultSidebar);
            }

            if (isCommonAsset) {
                this.scoreSidebar = new SecondaryNavItem(
                    'Scoring',
                    'Scoring',
                    ['fa-sitemap'],
                    `/sidebar/score/${this.uid}`, null, 7
                );

                this.secondaryNavService.showItem(this.scoreSidebar);

                if (this.getBooleanSetting(CompanySettingEnum.ShowCommentsTab)) {
                    this.commentsSidebar = new SecondaryNavItem(
                        'Comments', 'Comments', ['fa-comments'],
                        `/sidebar/comments/${this.uid}`, null, 33
                    );

                    this.secondaryNavService.showItem(this.commentsSidebar);
                }

                if (!this.getBooleanSetting(CompanySettingEnum.DisableIssueManagement)) {
                    this.actionsSidebar = new SecondaryNavItem(
                        'Actions', 'Actions', null,
                        `/sidebar/actions/${this.objectType}/${this.objectID}`, null, 27
                    );
                    this.secondaryNavService.showItem(this.actionsSidebar);
                }
            }

            if (this.objectType == 'TaskType') {
                this.governanceRolesSidebar = new SecondaryNavItem(
                    'Governance Roles', 'GovernanceRoles', null,
                    '/sidebar/governanceRoles', null, 3);
                if (!opts.hasGovernanceRoleSet) {
                    this.governanceRolesSidebar.warningMessage = 'GovRoleWarning';
                }
                this.secondaryNavService.showItem(this.governanceRolesSidebar);

                this.connectorLabels = new SecondaryNavItem(
                    'Connector Labels', 'ConnectorLabels', null,
                    '/sidebar/connectorLabels', null, 4);
                this.secondaryNavService.showItem(this.connectorLabels);
            }

            this.sidebarSubscription = this.secondaryNavService.rightSidebarClicked$.subscribe(
                (item) => {
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

    assetTypeContextUrl(): string {
        const url = '';

        if (!this.assetTypeID) {
            return url;
        }

        return `/type/${this.assetTypeID}`;
    }

    objectContextUrl(): string {
        const url = '';

        if (this.objectType == 'Tag') {
            if (this.uid && this.uid != '00000000-0000-0000-0000-000000000000') {
                return `/${this.objectType}/${this.uid}`;
            }
            else if (!this.objectID) {
                return `/${this.objectType}/0`;
            }
        }

        if (!this.objectType || !this.objectID) {
            return url;
        }

        return `/${this.objectType}/${this.objectID}`;
    }

    auditContextUrl(): string {
        const blankUid = '00000000-0000-0000-0000-000000000000';
        let uid = this.uid;

        if (this.objectType === "ResourceType") {
            return `/${this.objectType}/${this.resourceTypeUid}`;
        }

        if (this.objectType === "GroupType") {
            return `/${this.objectType}/${this.groupTypeUid}`;
        }

        if (this.objectType === "MetricAllocation") {
            return `/${this.objectType}/${this.metricAllocationUid}`;
        }

        if (this.objectType === "Predicate") {
            return `/${this.objectType}/${this.predicateUid}`;
        }

        //Tag needs to be part of the URL for the header to behave
        if (this.objectType == 'Tag') {
            if (this.uid && this.uid != blankUid) {
                return `/${this.objectType}/${this.uid}`;
            }
        }

        /**
         * Extract UID value for AssetTypes, IssueTypes, Referrence lists etc to use with audit component
         * Minimizer obfuscates this.constructor.name, so we'll have to look for specifi properties
         */
        if (this.uid == undefined || this.uid == blankUid) {
            if (this['selectedRow']) {
                if (this['selectedRow']['id']) { //AdminArtifactsComponent
                    uid = this['selectedRow']['id'];
                } else if (this['selectedRow']['uid']) { //AdminDiagramAssetComponent
                    uid = this['selectedRow']['uid'];
                }
            } else if (this['selectedReferenceListUid']) { //ReferenceListComponent
                uid = this['selectedReferenceListUid'];
            } else if (this['assetType'] && this['assetType']['AssetTypeUID']) { //HierarchyItemStructureComponent
                uid = this['assetType']['AssetTypeUID'];
            } else if (this['assetType'] && this['assetType']['uid']) { //HierarchyItemStructureComponent
                uid = this['assetType']['uid'];
            } else if (this['selected']) {
                if (this['selected']['Uid']) { //AdminIssueTypesComponent, AdminRelationshipsComponent
                    uid = this['selected']['Uid'];
                } else if (this['selected']['uid']) { //AdminHierarchiesComponent
                    uid = this['selected']['uid']
                }
            }
        }

        if (uid && uid != blankUid) {
            return `/${uid}`;
        }
        return '';
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
    private checkParentBase(item: any, arr: any[], typeId: number, objectName: string) {
        if (item.ParentID > 0 && arr) {
            let parentAr = arr.filter(x => x.ID == item.ParentID);
            let parent: any;
            if (parentAr.length > 0) {
                parent = parentAr[0];
                let crumb = new Breadcrumb(parent.DisplayValue,
                    SiteUrlHelpers.getObjectUrl(objectName.toUpperCase(), parent.ID, typeId),
                    true,
                    objectName,
                    parent.ID,
                    this.buildTreeNodeArrayBase(arr, parent.ParentID),
                    this.findSelectedTreeNodeBase(parent.ID), false, false)
                this.baseCrumbs.unshift(crumb);
                this.checkParentBase(parent, arr, typeId, objectName);
            }
        } else {
            this.baseCrumbs.forEach(x => this.breadcrumbsService.showBreadcrumb(x));
        }
    }

    public buildTreeNodeArrayBase(
        inputArr: any[],
        Parent?: number,
        includeChildren?: boolean
    ): TreeNode[] {
        // find the root items then
        let rootNodes = inputArr.filter(x => (Parent != undefined ? x.ParentID == Parent : !x.ParentID));

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
                children: (includeChildren ? this.buildTreeNodeArrayBase(inputArr, root.ID) : null) // recursively find its children
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
            failed.forEach((f) => {
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

    expandTreeNode(node: TreeNode) {
        node.expanded = true;
        if (node.children) {
            node.children.forEach(n => this.expandTreeNode(n));
        }
    }

    buildSecondaryNavigationForAssetID(assetId: number, object: string, buildBreadcrumbOverride: Function = null) {
        this.buildSecondaryNavigation(null, null, object, assetId, null, buildBreadcrumbOverride);
    }

    buildSecondaryNavigationForObject(objectId: number, object: string, buildBreadcrumbOverride: Function = null, assetClass: AssetTypeClass = null) {
        this.buildSecondaryNavigation(null, objectId, object, null, null, buildBreadcrumbOverride, assetClass);
    }

    private isSidebarLoadedForCurrentObject(loadData: SecondaryNavPostModel): boolean {
        //this is fullpage refresh, invalidate key to recreate navigation
        var checkdisplayvalue = false;
        if (!this.secondaryNavService["isSidebarCreated"]) {
            this.secondaryNavService.invalidateKey();
            return false;
        }


        var currentData = JSON.parse(this.secondaryNavService.getLoadedKey());

        if (loadData.DisplayValue != null) {
            checkdisplayvalue = true
        }

        if (loadData.ObjectType == currentData.Object && loadData.ObjectId == currentData.ObjectId)
            return true;

        if (checkdisplayvalue) {
            if (loadData.AssetUid == currentData.Uid && currentData.DisplayValue == loadData.DisplayValue)
                return true;
        }
        else {
            if (loadData.AssetUid == currentData.Uid)
                return true;
        }

        if (loadData.AssetId == currentData.AssetId)
            return true;

        return false;
    }

    refreshObjectStats() {
        this.secondaryNavService.refreshStats();
    }

    buildSecondaryNavigation(assetUid: any = null, objectId: number = null, objectType: string = null, assetId: number = null, assetTypeUid: string = null, buildBreadcrumbOverride: Function = null, assetClass: AssetTypeClass = null, DisplayValue: string = null) {
        var data = new SecondaryNavPostModel();
        data.PreloadData = false;
        data.Class = assetClass;
        if (assetUid != null)
            data.AssetUid = assetUid.toString().toLowerCase();

        if (DisplayValue != null)
            data.DisplayValue = DisplayValue;

        if (objectId) {
            data.ObjectId = objectId;
            if (objectId.toString().length == 36) {
                data.AssetUid = objectId.toString();
            }
        }

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

        if (assetUid == null && !assetId && !assetTypeUid && !(objectId != null && objectId != undefined)) {
            return;
        }
        if (this.isSidebarLoadedForCurrentObject(data)) {
            this.refreshObjectStats();
            return;
        }

        this.secondaryNavService.getSiteMenuService().getSecondaryNav(data).subscribe((r) => {
            this.assetID = r.AssetId;
            this.assetTypeID = r.AssetTypeId;
            this.uid = r.Uid;
            this.objectType = r.Object;
            this.objectID = r.ObjectID;

            var _key = JSON.stringify({ AssetId: r.AssetId, AssetTypeIdb: r.AssetTypeId, Uid: r.Uid, Object: r.Object, ObjectId: r.ObjectID, DisplayValue: r.DisplayValue });
            this.secondaryNavService.setLoadedKey(_key);

            this.clearSidebar();
            this.breadcrumbsService.clearBreadcrumbs();

            var areaName = r.DisplayValue;
            var mainTabTitle = r.MainTabTitle;
            if (r.PreloadData) {
                this.preloadedTreeData = r.PreloadData;
                if (r.PreloadData.Data) {
                    this.preloadedTreeData = r.PreloadData.Data;
                }
            }
            let area = this.determineAreaForAdminPage(areaName);

            var homeUrl = SiteUrlHelpers.getUrl(r.Object, r.ObjectID, r.ObjectTypeId, areaName, this.uid);
            this.secondaryNavService.setLocalHomeUrl(homeUrl);
            this.breadcrumbsService.setCurrentObjectInfo(r.Object, r.ObjectID);
            if (buildBreadcrumbOverride == null) {
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
            }
            else {
                buildBreadcrumbOverride();
            }

            this.secondaryNavService.clearItems();
            this.secondaryNavService.clearButtons();

            var areaIcon = area === 'Configuration' ? 'fa-sliders' : "fa-cog";
            if (r.Object == 'Tag')
                areaIcon = 'fa-tag';
            this.secondaryNavService.setCurrentArea(areaName, areaIcon, mainTabTitle);

            this.setCommonSecondaryNavTabs({
                hasAudit: r.Items.HasAudit,
                hasOwnership: r.Items.HasOwnership,
                hasDashboard: r.Items.HasDashboard,
                hasLineage: r.Items.HasLineage,
                hasImpact: r.Items.HasImpact, 
                hasRelationships: r.Items.HasRelationship, 
                hasFollowers: r.Items.HasFollowers, 
                hasMonitor: r.Items.HasWorkflow,
                hasField: r.Items.HasField, 
                hasChild: r.Items.HasChild,
                hasRuleResult: this.objectType == 'Rule',
                hasGovernanceRoleSet: r.Items.HasGovernanceRoleUidSet,
                hasProcessDiagram: r.Items.HasProcessDiagram
            });

            var isType = this.IsType(r.Object);
            this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject(r.ObjectType, r.ObjectTypeId, this.objectType, this.objectID, isType, r.Items.HasWorkflow, this.uid, r.Items.HasRequestCertificationWorkflow));
            this.secondaryNavService.showHeader(true);

            this.activateComponent();
        })
    }

    protected determineAreaForAdminPage(areaName: string): string {
        let area = "";

        area = [
            StringConstants.Section_BusinessAssets,
            StringConstants.Section_TechnicalAssets,
            StringConstants.Section_Artifacts,
            StringConstants.Section_Models,
            StringConstants.Section_Policies,
            StringConstants.Section_Predicates,
            StringConstants.Section_Relationships,
            StringConstants.Section_Rules,
            StringConstants.Section_Scoring,
            StringConstants.Section_Surveys,
            StringConstants.Section_Actions,
            StringConstants.Section_Workflows]
            .indexOf(areaName) !== -1 ? StringConstants.Area_Configuration : StringConstants.Area_Administration;

        if (this.objectType == 'Tag' && this.uid && this.uid != '00000000-0000-0000-0000-000000000000') {
            area = 'Tags';
        }

        return area;
    }

    private IsType(objectName: string): boolean {
        if (objectName == 'Tag')
            return true;

        if (objectName == 'MetricAllocation' || objectName == 'Predicate') {
            return true;
        }

        if (objectName.length <= 4)
            return false;
        if (objectName.substr(objectName.length - 4).toLowerCase() == "type") {
            return true;
        }
        return false;
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

    private activateComponent() {
        var currentComponentUrl = '';
        if (this.breadcrumbsService) {
            currentComponentUrl = this.breadcrumbsService.getCurrentUrl();
        }

        var components: SecondaryNavItem[] = [];
        components.push(this.scoreSidebar);
        components.push(this.dashboardSidebar);
        components.push(this.lineageSidebar);
        components.push(this.relationsSidebar);
        components.push(this.ownershipSidebar);
        components.push(this.actionsSidebar);
        components.push(this.monitorSidebar);
        components.push(this.commentsSidebar);
        components.push(this.followersSidebar);
        components.push(this.auditSidebar);
        components.push(this.childSidebar);
        components.push(this.fieldNav);
        components.push(this.ruleResultSidebar);
        components.push(this.governanceRolesSidebar);
        components.push(this.connectorLabels);

        components.forEach((cmp) => {
            if (cmp && currentComponentUrl.startsWith(cmp.url)) {
                cmp.active = true;
            }

            if (cmp && cmp.subTabsUrl.some(x => x == currentComponentUrl || currentComponentUrl.indexOf(x) == 0)) {
                cmp.active = true;
            }
        });
    }

    private setArtifactBreadcrumbs(data) {
        var artifact = data.Artifact;

        let folderName: string = '#Business';

        if (artifact.Class == AssetTypeClass.TechnicalAsset) {
            folderName = '#Technical';
        }
        this.breadcrumbsService.getFolderTitle(folderName).then((res) => {
            this.breadcrumbsService.clearBreadcrumbs();

            var folderTitle = res;
            var area = res;

            let index = 0;
            this.breadcrumbsService
                .getAreaName('ArtifactType', data.Artifact.Breadcrumbs[0] ? this.GetIDFromUrl(data.Artifact.Breadcrumbs[0].Url) : data.Artifact.AssetTypeID)
                .subscribe((result) => {
                    var currentAreaName = result;
                    let currentFolderName = currentAreaName ? currentAreaName : folderTitle;

                    this.breadcrumbsService.clearBreadcrumbs();
                    this.breadcrumbsService.getAssetFolderIcon('ArtifactType', data.ObjectTypeId, currentFolderName).subscribe((res) => {
                        this.secondaryNavService.setCurrentArea(data.Artifact.DisplayValue, res, 'Definition');
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
                                            data.ObjectTypeId,
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

                            this.checkIfWorkflowActionIsSelected();

                        }
                    });
                });
        });


    }

    //used for policy/model
    private setTreeBreadcrumbs(data, objectName: string) {
        var selected = this.preloadedTreeData.find(x => x.ID == data.ObjectID);
        var objectTypeName = objectName + "Type";
        this.breadcrumbsService.clearBreadcrumbs();

        this.breadcrumbsService.breadcrumbTreeSource$.subscribe(
            (id) => {
                if (objectName.toLowerCase() == 'policy') {
                    this.breadcrumbsService.reRouteFromBreadcrumbs(`/${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${data.ObjectTypeId};hierarchyId=${id}`);
                }
                if (objectName.toLowerCase() == 'taxonomy') {
                    this.breadcrumbsService.reRouteFromBreadcrumbs(`/${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${data.ObjectTypeId};hierarchyId=${id}`);
                }
            }
        );

        this.breadcrumbsService
            .getAreaName(objectTypeName, data.ObjectTypeId)
            .subscribe((result) => {
                this.baseCrumbs = [];
                var currentAreaName = result;

                this.breadcrumbsService.getFolderTitle('#' + (objectName == 'Taxonomy' ? 'Models' : objectName)).then((res) => {
                    this.breadcrumbsService.clearBreadcrumbs();

                    var folderTitle = res;
                    let currentFolderName = currentAreaName ? currentAreaName : folderTitle;

                    this.breadcrumbsService.getAssetFolderIcon(objectTypeName, data.ObjectTypeId, currentFolderName).subscribe((res) => {
                        this.secondaryNavService.setCurrentArea(data.DisplayValue, res, 'Definition');
                    });

                    let areaRootUriSegment: string = (objectName.toLowerCase() == 'policy') ? SiteUrlHelpers.SITE_URL_POLICY_ROOT : SiteUrlHelpers.SITE_URL_MODEL_ROOT;
                    let areaBreadcrumb = new Breadcrumb(
                        currentAreaName ? currentAreaName : res, `${areaRootUriSegment}/${SiteUrlHelpers.SITE_URL_HIERARCHY_CLASSIFICATION}`
                    );
                    this.breadcrumbsService.showBreadcrumb(areaBreadcrumb);

                    this.breadcrumbsService.showBreadcrumb(
                        new Breadcrumb(
                            data.TypeName,
                            SiteUrlHelpers.getObjectUrl(objectTypeName, data.ObjectTypeId), undefined, objectTypeName.toUpperCase(), data.ObjectTypeId, undefined, undefined, true
                        )
                    );

                    this.setObjectInfo(objectName, data.ObjectId, data.DisplayValue, data.AssetID, undefined, data.Uid);

                    if (selected && selected.ID > 0 && data && data.Uid) {
                        this.setObjectInfo(objectName, selected.ID, selected.DisplayValue, selected.AssetID, undefined, selected.Uid);
                        this.baseCrumbs = [];
                        this.checkParentBase(selected, this.preloadedTreeData, data.ObjectTypeId, objectName);
                        this.breadcrumbsService.showBreadcrumb(
                            new Breadcrumb(
                                selected.DisplayValue,
                                SiteUrlHelpers.getAssetUrl(data.Uid),
                                true,
                                objectName,
                                selected.ID,
                                this.buildTreeNodeArrayBase(this.preloadedTreeData, selected.ParentID),
                                this.findSelectedTreeNodeBase(selected.ID)));
                    }
                    this.checkIfWorkflowActionIsSelected();

                });
            });
    }

    private setRuleBreadcrumbs(data) {
        this.breadcrumbsService
            .getAreaName('RuleType', data.ObjectTypeId)
            .subscribe((result) => {

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

                    this.breadcrumbsService.getAssetFolderIcon('RuleType', data.ObjectTypeId, currentAreaName ? currentAreaName : res).subscribe((icon) => {
                        this.secondaryNavService.setCurrentArea(data.DisplayValue, icon, 'Definition');

                    });

                    this.breadcrumbsService.showBreadcrumb(new Breadcrumb(data.DisplayValue, null,
                        true,
                        'Rule',
                        data.ObjectId));
                    this.checkIfWorkflowActionIsSelected();

                });
            });

    }

    private checkIfWorkflowActionIsSelected() {
        if (this.breadcrumbsService.getCurrentUrl().toLowerCase().indexOf('workflow/details') != -1) {
            this.actionsSidebar.active = true;
        }
    }

    private GetIDFromUrl(url: string) {
        return +url.split("/")[url.split.length - 1];
    }

    public getAsPrecentage(val: number): string {
        if (val == undefined || val == null)
            return 'undefined';

        if (val == 0)
            return '0%';
        if (!val)
            return;
        if (val >= 1)
            return '100%'

        if (val > 1) {
            var integerPart = Math.floor(val);
            var fraction = val - integerPart;
            var res = this.getAsPrecentage(fraction);
            if (res.length == 2) {
                return integerPart + '0' + res;
            }
            return integerPart + res;
        }

        let s = (val * 100).toFixed(2).replace(/0+$/g, "").replace(/(\.[0]*?)0*$/g, "") + "%";

        return s;
    }

    public isReferenceListType(value: string): boolean {
        return value.toLowerCase() === this.referenceListUid.toLowerCase();
    }
    public get referenceListUid(): string {
        return "0000000a-0000-0000-0000-000000000009";
    }
}
