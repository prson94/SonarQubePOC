import { Input } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { PermissionsService } from '../../services/permissions.service';
import { MessagesService } from '../../services/messages.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';

import { Subscription }   from 'rxjs/Subscription';
import { Permission } from '../../models/permission.model'
import { StringConstants } from '../../static/string-constants';
import { JsonResult } from '../../models/jsonresult.model';

declare var CompanySettings;

export class BaseComponent {    
    protected isLoading = false;

    //current object info
    objectID: number;
    objectType: string;
    objectName: string;

    //sidebar
    sidebarSubscription: Subscription;
    isVisitingSidebar: boolean = false;

    auditSidebar: RightSidebarItem;
    ownershipSidebar: RightSidebarItem;
    lineageSidebar: RightSidebarItem;
    impactSidebar: RightSidebarItem;
    relationsSidebar: RightSidebarItem;
    monitorSidebar: RightSidebarItem;
    //tabs

    lineageShowUsageOnly: boolean = false;
    
    //filter mode
    showSimpleFilter: boolean = true;

    //permissions
    // Ideally this should be an input so we dont have to copy / past it...
    // child classes that support permissions input....
    permissions: Permission[] = [];

    //default paging options
    defaultPagingOptions: number[] = [10, 25, 50, 100];
    defaultInitialItemsPerPage: number = 10;

    protected rightSidebarService: RightSidebarService = null;
    protected webAnalyticsService: WebAnalyticsService = null;

    //constructor(protected rightSidebarService?: RightSidebarService, protected webAnalyticsService?: WebAnalyticsService) { }

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

    loadPermissions(permissionsService: PermissionsService, objectType: string, objectID: number) {
        permissionsService.getPermissions(objectID, objectType)
            .then(result => {
                this.permissions = result;
            });
    }

    hasPermission(object: string, claim: string) {return Permission.hasPermission(this.permissions, object, claim);}

    hasCreatePermissions(object: string) {return this.hasPermission(object, StringConstants.ClaimCreate);}
    hasDeletePermissions(object: string) {return this.hasPermission(object, StringConstants.ClaimDelete);}
    hasUpdatePermissions(object: string) {return this.hasPermission(object, StringConstants.ClaimUpdate);}
    hasReadPermissions(object: string) {return this.hasPermission(object, StringConstants.ClaimRead);}

    hasRootCreatePermissions() {return this.hasPermission(StringConstants.ObjectRoot, StringConstants.ClaimCreate);}
    hasRootDeletePermissions() {return this.hasPermission(StringConstants.ObjectRoot, StringConstants.ClaimDelete);}
    hasRootUpdatePermissions() {return this.hasPermission(StringConstants.ObjectRoot, StringConstants.ClaimUpdate);}
    hasRootReadPermissions() { return this.hasPermission(StringConstants.ObjectRoot, StringConstants.ClaimRead); }

    hasRelationshipCreatePermissions() {return this.hasPermission(StringConstants.ObjectRelationship, StringConstants.ClaimCreate);}
    hasRelationshipDeletePermissions() {return this.hasPermission(StringConstants.ObjectRelationship, StringConstants.ClaimDelete);}
    hasRelationshipUpdatePermissions() {return this.hasPermission(StringConstants.ObjectRelationship, StringConstants.ClaimUpdate);}
    hasRelationshipReadPermissions() {return this.hasPermission(StringConstants.ObjectRelationship, StringConstants.ClaimRead);}

    hasAttributeCreatePermissions() { return this.hasPermission(StringConstants.ObjectAttribute, StringConstants.ClaimCreate); }
    hasAttributeDeletePermissions() { return this.hasPermission(StringConstants.ObjectAttribute, StringConstants.ClaimDelete); }
    hasAttributeUpdatePermissions() { return this.hasPermission(StringConstants.ObjectAttribute, StringConstants.ClaimUpdate); }
    hasAttributeReadPermissions() { return this.hasPermission(StringConstants.ObjectAttribute, StringConstants.ClaimRead); }


    /*end permissions functionality*/

    setCommonRightSideBar(hasAudit?: boolean, hasOwnership?: boolean, hasDashboard?: boolean, hasLineage?: boolean, hasImpact?: boolean, hasRelationships?: boolean, hasFollowers?: boolean, hasMonitor?: boolean) {
        if (this.rightSidebarService) {
            this.clearSidebar();
            if (hasLineage && CompanySettings.ShowLineageSidebar != 'false') {
                this.lineageSidebar = new RightSidebarItem('Lineage', 'lineage', ['fa-random'], `/sidebar/visualization/lineage${this.objectContextUrl()}${this.lineageShowUsageOnly ? '/1' : ''}`);
                this.rightSidebarService.showItem(this.lineageSidebar);
            }
            if (hasAudit || hasAudit === undefined) {
                this.auditSidebar = new RightSidebarItem('Audit', 'audit', ['fa-eye'], `/sidebar/audit${this.objectContextUrl()}`);
                this.rightSidebarService.showItem(this.auditSidebar);
            }
            if (hasOwnership && CompanySettings.ShowOwnersSidebar != 'false') {
                this.ownershipSidebar = new RightSidebarItem('Ownership', 'ownership', ['fa-user'], `/sidebar/ownership${this.objectContextUrl()}`)
                this.rightSidebarService.showItem(this.ownershipSidebar);
            }
            if (hasDashboard) this.rightSidebarService.showItem(new RightSidebarItem('Dashboards', 'dashboards', ['fa-tachometer'], `/sidebar/dashboard${this.objectContextUrl()}`));
            if (hasImpact && CompanySettings.ShowImpactSidebar != 'false') {
                this.impactSidebar = new RightSidebarItem('Impact', 'impact', ['fa-exchange'], `/sidebar/visualization/impact${this.objectContextUrl()}`);
                this.rightSidebarService.showItem(this.impactSidebar);
            }
            if (hasRelationships) {
                this.relationsSidebar = new RightSidebarItem('Relations', 'relationship', ['fa-retweet'], `/sidebar/relationships${this.objectContextUrl()}`)
                this.rightSidebarService.showItem(this.relationsSidebar);
            }
            if (hasFollowers && CompanySettings.ShowFollowersSidebar != 'false') this.rightSidebarService.showItem(new RightSidebarItem('Followers', 'followers', ['fa-bookmark-o'], `/sidebar/followers${this.objectContextUrl()}`));

            if (hasMonitor) {
                this.monitorSidebar = new RightSidebarItem('Workflow Monitor', 'monitor', ['fa-television'], `/sidebar/workflowmonitor${this.objectContextUrl()}`);
                this.rightSidebarService.showItem(this.monitorSidebar);
            }

            this.sidebarSubscription = this.rightSidebarService.rightSidebarClicked$.subscribe(
                item => {
                    this.isVisitingSidebar = true;              
                    this.showHideBreadcrumbItem(item);
                });
        }
    }

    setObjectInfo(objectType: string, objectID: number, objectName?: string) {
        this.objectType = objectType;
        this.objectID = objectID;
        if (objectName != undefined) this.objectName = objectName;
    }

    objectContextUrl(): string {
        let url = '';
        if (!this.objectType || !this.objectID) return url;
        return `/${this.objectType}/${this.objectID}`;
    }
        
    //This is generally overloaded to show hide in your own class.
    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        //console.log('show/hide :');
        //console.log(activatedItem);
    }

    clearSidebar(unsubscribe?: boolean) {
        if (this.rightSidebarService) {
            if (!this.isVisitingSidebar) this.rightSidebarService.clearItems();
            if (this.sidebarSubscription && (unsubscribe || unsubscribe == undefined)) {                
                this.sidebarSubscription.unsubscribe();
            }
        }
    }

    showMessageForResult(messagesService: MessagesService, result: JsonResult) {
        if (result.type == 'error') messagesService.showError(result.title, result.message);
        else messagesService.showInfoMessage(result.title, result.message);
    }
    
}