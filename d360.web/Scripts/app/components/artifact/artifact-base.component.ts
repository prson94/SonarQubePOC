import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { Title } from '@angular/platform-browser';
import { BaseComponent } from '../shared/base.component';
import { Subscription }   from 'rxjs';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export class ArtifactBaseComponent extends BaseComponent {            
    public area: string = "Business";
    public folderTitle: string; 
    public areaLink: string = SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT;
    //sidebar
    sidebarSubscription: Subscription;
    
    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, rightSidebarService?: RightSidebarService, webAnalyticsService?: WebAnalyticsService) {
        super();
        this.rightSidebarService = rightSidebarService;
        this.webAnalyticsService = webAnalyticsService;
        //headerBreadcrumbService.getFolderTitle('#Business').then(res => { this.folderTitle = res; });
        this.rightSidebarService.showHeader(true);
    }        
    
    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        
    }    
}