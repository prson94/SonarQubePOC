import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService, HeaderBreadcrumbService, RightSidebarService, WebAnalyticsService  } from '../../services/index';
import { Title } from '@angular/platform-browser';
import { BaseComponent } from '../shared/base.component';
import { Subscription }   from 'rxjs/Subscription';
import { RightSidebarItem } from '../../models/rightsidebar.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export class ArtifactBaseComponent extends BaseComponent {            
    public area: string = "Glossary";
    public areaLink: string = SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT;

    protected isLoading = false;

    //sidebar
    sidebarSubscription: Subscription;

    isAuditVisible: boolean = false;

    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, rightSidebarService?: RightSidebarService, webAnalyticsService?: WebAnalyticsService) {
        super(rightSidebarService, webAnalyticsService);
        
    }        

    setBrowserTitle(tileService: Title, area: string) {
        tileService.setTitle(`D3S - ${area}`);
    }

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        console.log('show/hide ' + activatedItem);
    }    
}