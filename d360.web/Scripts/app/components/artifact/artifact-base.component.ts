import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService } from '../../services/messages.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { Title } from '@angular/platform-browser';
import { BaseComponent } from '../shared/base.component';
import { Subscription }   from 'rxjs';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export class ArtifactBaseComponent extends BaseComponent {            
    public area: string = "Glossary";
    public folderTitle: string; 
    public areaLink: string = SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT;
    //sidebar
    sidebarSubscription: Subscription;
    
    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, rightSidebarService?: RightSidebarService, webAnalyticsService?: WebAnalyticsService) {
        super();
        this.rightSidebarService = rightSidebarService;
        this.webAnalyticsService = webAnalyticsService;
        headerBreadcrumbService.getFolderTitle('#Glossary').then(res => { this.folderTitle = res });
    }        
    
    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        
    }    
}