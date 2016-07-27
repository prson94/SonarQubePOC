import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService, HeaderBreadcrumbService, PageHeader, RightSidebarService  } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Subscription }   from 'rxjs/Subscription';
import { RightSidebarItem } from '../../models/rightsidebar.model';

export class AdminBaseComponent extends BaseComponent {
    public areaName: string;
    public areaLink: string = undefined;
    public areaDescription: string = "base";
    public area: string = "Administration";

    //sidebar
    sidebarSubscription: Subscription;
    
    isAuditVisible: boolean = false;
    

    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, protected pageHeader: PageHeader, protected titleService: Title, protected rightSidebarService?: RightSidebarService) {
        super();        
    }

    setCommonItems() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.area));
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.areaName, this.areaLink));
        this.pageHeader.description = this.areaDescription;
        this.setBrowserTitle(this.titleService, this.areaName);
    }

    setCommonRightSideBar(hasAudit?: boolean) {
        if (this.rightSidebarService) {
            this.rightSidebarService.showItem(new RightSidebarItem('Audit', 'audit'));


            this.sidebarSubscription = this.rightSidebarService.rightSidebarClicked$.subscribe(
                item => {
                    if (item.tag = 'audit')
                        this.isAuditVisible = !this.isAuditVisible;
                });
        }
    }

    clearSidebar() {
        if (this.rightSidebarService) {
            this.rightSidebarService.clearItems();
            this.sidebarSubscription.unsubscribe();
        }
    }
}