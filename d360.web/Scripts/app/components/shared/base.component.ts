import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { RightSidebarService, WebAnalyticsService  } from '../../services/index';
import { Subscription }   from 'rxjs/Subscription';


export class BaseComponent {    
    protected isLoading = false;

    //sidebar
    sidebarSubscription: Subscription;

    //tabs
    isAuditVisible: boolean = false;
    isOwnershipVisible: boolean = false;
    isDashboardVisible: boolean = false;
    isLineageVisible: boolean = false;

    //filter mode
    showSimpleFilter: boolean = true;

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


    setCommonRightSideBar(hasAudit?: boolean, hasOwnership?: boolean, hasDashboard?: boolean, hasLineage?: boolean) {
        if (this.rightSidebarService) {
            if (hasAudit || hasAudit === undefined) this.rightSidebarService.showItem(new RightSidebarItem('Audit', 'audit'));
            if (hasOwnership) this.rightSidebarService.showItem(new RightSidebarItem('Ownership', 'ownership'));
            if (hasDashboard) this.rightSidebarService.showItem(new RightSidebarItem('Dashboards', 'dashboards'));
            if (hasLineage) this.rightSidebarService.showItem(new RightSidebarItem('Lineage', 'lineage'));

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
                    else
                        this.showHideBreadcrumbItem(item);
                });
        }
    }

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        console.log('show/hide :');
        console.log(activatedItem);
    }

    clearSidebar(unsubscribe?: boolean) {
        if (this.rightSidebarService) {
            this.rightSidebarService.clearItems();
            if (this.sidebarSubscription && (unsubscribe || unsubscribe == undefined)) {
                console.log("DEV INFO - UNSUBSCRIBING FROM RIGHT SIDE BAR SUBSCRIPTION");
                this.sidebarSubscription.unsubscribe();
            }
        }
    }
}