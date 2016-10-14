import { Input } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { RightSidebarService, WebAnalyticsService, PermissionsService  } from '../../services/index';
import { Subscription }   from 'rxjs/Subscription';
import { Permission } from '../../models/permission.model'
import { StringConstants } from '../../static/string-constants';

export class BaseComponent {    
    protected isLoading = false;

    //sidebar
    sidebarSubscription: Subscription;

    //tabs
    isAuditVisible: boolean = false;
    isOwnershipVisible: boolean = false;
    isDashboardVisible: boolean = false;
    isLineageVisible: boolean = false;
    isImpactVisible: boolean = false;
    isRelationshipsVisible: boolean = false;
    isFollowersVisible: boolean = false;

    //filter mode
    showSimpleFilter: boolean = true;

    //permissions
    // Ideally this should be an input so we dont have to copy / past it...
    // child classes that support permissions input....
    permissions: Permission[] = [];

    constructor(protected rightSidebarService?: RightSidebarService, protected webAnalyticsService?: WebAnalyticsService) { }

    protected setBrowserTitle(tileService: Title, area: string) {
        tileService.setTitle(`D3S - ${area}`);
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

    setCommonRightSideBar(hasAudit?: boolean, hasOwnership?: boolean, hasDashboard?: boolean, hasLineage?: boolean, hasImpact?: boolean, hasRelationships?: boolean, hasFollowers?: boolean) {
        if (this.rightSidebarService) {
            if (hasAudit || hasAudit === undefined) this.rightSidebarService.showItem(new RightSidebarItem('Audit', 'audit'));
            if (hasOwnership) this.rightSidebarService.showItem(new RightSidebarItem('Ownership', 'ownership'));
            if (hasDashboard) this.rightSidebarService.showItem(new RightSidebarItem('Dashboards', 'dashboards'));
            if (hasLineage) this.rightSidebarService.showItem(new RightSidebarItem('Lineage', 'lineage'));
            if (hasImpact) this.rightSidebarService.showItem(new RightSidebarItem('Impact', 'impact'));
            if (hasRelationships) this.rightSidebarService.showItem(new RightSidebarItem('Relations', 'relationship'));
            if (hasFollowers) this.rightSidebarService.showItem(new RightSidebarItem('Followers', 'followers'));

            this.sidebarSubscription = this.rightSidebarService.rightSidebarClicked$.subscribe(
                item => {
                    if (item.tag == 'audit')
                        this.isAuditVisible = !this.isAuditVisible;
                    else if (item.tag == 'ownership')
                        this.isOwnershipVisible = !this.isOwnershipVisible;
                    else if (item.tag == 'dashboards')
                        this.isDashboardVisible = !this.isDashboardVisible;
                    else if (item.tag == 'lineage')
                        this.isLineageVisible = !this.isLineageVisible;
                    else if (item.tag == 'impact')
                        this.isImpactVisible = !this.isImpactVisible;
                    else if (item.tag == 'relationship')
                        this.isRelationshipsVisible = !this.isRelationshipsVisible;
                    else if (item.tag == 'followers')
                        this.isFollowersVisible = !this.isFollowersVisible;                            
                    else
                        this.showHideBreadcrumbItem(item);
                });
        }
    }

    hideSidebarItems() {
        this.isAuditVisible = false;
        this.isOwnershipVisible = false;
        this.isDashboardVisible = false;
        this.isFollowersVisible = false;
        this.isImpactVisible = false;
        this.isLineageVisible = false;
        this.isRelationshipsVisible = false;
    }


    //This is generally overloaded to show hide in your own class.
    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        //console.log('show/hide :');
        //console.log(activatedItem);
    }

    clearSidebar(unsubscribe?: boolean) {
        if (this.rightSidebarService) {
            this.rightSidebarService.clearItems();
            if (this.sidebarSubscription && (unsubscribe || unsubscribe == undefined)) {
                //console.log("DEV INFO - UNSUBSCRIBING FROM RIGHT SIDE BAR SUBSCRIPTION");
                this.sidebarSubscription.unsubscribe();
            }
        }
    }
    
}