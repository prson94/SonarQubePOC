import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { PermissionsService } from '../../services/permissions.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';

import { Subscription } from 'rxjs';
import { FormHelpers } from '../../static/form-helpers';
import { JsonResult } from '../../models/jsonresult.model';
import { ApiResult } from '../../models/apiresult.model';
import { ResponsibilityTypeRelationPermission, Permission } from '../../models/responsibility-type.model';
import { HttpErrorResponse } from '@angular/common/http';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { TreeNode } from 'primeng/primeng';

declare var CompanySettings;

export class BaseComponent {
    public isLoading = false;

    // current object info
    uid: string;
    assetID: number;
    assetTypeID: number;
    objectID: number;
    objectType: string;
    objectName: string;

    // sidebar
    sidebarSubscription: Subscription;
    isVisitingSidebar = false;

    auditSidebar: RightSidebarItem;
    ownershipSidebar: RightSidebarItem;
    lineageSidebar: RightSidebarItem;
    impactSidebar: RightSidebarItem;
    relationsSidebar: RightSidebarItem;
    monitorSidebar: RightSidebarItem;
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

    protected rightSidebarService: RightSidebarService = null;
    protected webAnalyticsService: WebAnalyticsService = null;

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

    setCommonRightSideBar(
        hasAudit?: boolean,
        hasOwnership?: boolean,
        hasDashboard?: boolean,
        hasLineage?: boolean,
        hasImpact?: boolean,
        hasRelationships?: boolean,
        hasFollowers?: boolean,
        hasMonitor?: boolean
    ) {
        if (this.rightSidebarService) {
            this.clearSidebar();
            if (hasLineage && CompanySettings.ShowLineageSidebar != 'false') {
                const isLineageShowUsageOnly = this.lineageShowUsageOnly ? '/1' : '';
                const urlLineage = this.objectContextUrl() + isLineageShowUsageOnly;

                this.lineageSidebar = new RightSidebarItem(
                    'Lineage',
                    'lineage',
                    ['fa-random'],
                    `/sidebar/visualization/lineage${urlLineage}`, null, 15
                );
                this.rightSidebarService.showItem(this.lineageSidebar);
            }

            if (hasAudit || hasAudit === undefined) {
                this.auditSidebar = new RightSidebarItem(
                    'Change Log',
                    'Change Log',
                    ['fa-eye'],
                    `/sidebar/audit${this.objectContextUrl()}`, null, 40
                );
                this.rightSidebarService.showItem(this.auditSidebar);
            }

            if (hasOwnership && CompanySettings.ShowOwnersSidebar != 'false') {
                this.ownershipSidebar = new RightSidebarItem(
                    'Responsibilities',
                    'ownership',
                    ['fa-user'],
                    `/sidebar/ownership/${this.assetID}`, null, 25
                );
                this.rightSidebarService.showItem(this.ownershipSidebar);
            }

            if (hasDashboard) {
                this.rightSidebarService.showItem(
                    new RightSidebarItem(
                        'Dashboards',
                        'dashboards',
                        ['fa-tachometer'],
                        `/dashboard${this.objectContextUrl()}`, null, 5
                    )
                );
            }

            if (hasImpact && CompanySettings.ShowImpactSidebar != 'false' && CompanySettings.LineageVersion != 3) {
                this.impactSidebar = new RightSidebarItem(
                    'Impact',
                    'impact',
                    ['fa-exchange'],
                    `/sidebar/visualization/impact${this.objectContextUrl()}`, null, 10
                );
                this.rightSidebarService.showItem(this.impactSidebar);
            }

            if (hasRelationships) {
                this.relationsSidebar = new RightSidebarItem(
                    'Related Assets',
                    'relationship',
                    ['fa-retweet'],
                    `/sidebar/relationships${this.objectContextUrl()}`, null, 20
                );
                this.rightSidebarService.showItem(this.relationsSidebar);
            }

            if (hasFollowers && CompanySettings.ShowFollowersSidebar != 'false') {
                this.rightSidebarService.showItem(
                    new RightSidebarItem(
                        'Followers',
                        'followers',
                        ['fa-bookmark-o'],
                        `/sidebar/followers${this.objectContextUrl()}`, null, 35
                    )
                );
            }

            if (hasMonitor) {
                this.monitorSidebar = new RightSidebarItem(
                    'Workflow',
                    'monitor',
                    ['fa-usb'],
                    `/sidebar/workflowmonitor${this.objectContextUrl()}`, null, 30
                );
                this.rightSidebarService.showItem(this.monitorSidebar);
            }

            this.sidebarSubscription = this.rightSidebarService.rightSidebarClicked$.subscribe(
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

    // This is generally overloaded to show hide in your own class.
    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
    }

    clearSidebar(unsubscribe?: boolean) {
        if (this.rightSidebarService) {
            if (!this.isVisitingSidebar) {
                this.rightSidebarService.clearItems();
                this.rightSidebarService.clearButtons();
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

    showHttpErrorMessage(messagesService: MessagesObservableService, err: HttpErrorResponse) {
        messagesService.showError('An error occurred', err.error);
    }

    public getLocaleDateString(): string {
        return FormHelpers.getLocaleDateString();
    }

    public filterTreeTable(originalArray: TreeNode[], search: string, tree: any) {
        var arrDeepCopy = JSON.parse(JSON.stringify(originalArray));
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
            if (prop.toLowerCase().indexOf("value") != -1 || prop.toLowerCase().indexOf("field") != -1) {
                if (node.data[prop] && node.data[prop].toString().toLowerCase().indexOf(q.toLowerCase()) != -1) hasValue = true;
            }
        });

        return hasValue;
    }

}
