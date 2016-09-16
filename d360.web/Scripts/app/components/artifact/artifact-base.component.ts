import { Breadcrumb } from '../../models/breadcrumb.model';
import { MessagesService, HeaderBreadcrumbService, PageHeader, RightSidebarService, WebAnalyticsService  } from '../../services/index';
import { Title } from '@angular/platform-browser';
import { BaseComponent } from '../shared/base.component';
import { Subscription }   from 'rxjs/Subscription';
import { RightSidebarItem } from '../../models/rightsidebar.model';

export class ArtifactBaseComponent extends BaseComponent {        
    public areaDescription: string = "base";
    public area: string = "Glossary";
    public areaLink: string = "/a/artifact";

    protected isLoading = false;

    //sidebar
    sidebarSubscription: Subscription;

    isAuditVisible: boolean = false;

    constructor(protected headerBreadcrumbService: HeaderBreadcrumbService, protected pageHeader: PageHeader, rightSidebarService?: RightSidebarService, webAnalyticsService?: WebAnalyticsService) {
        super(rightSidebarService, webAnalyticsService);
        pageHeader.description = "";
    }        

    setBrowserTitle(tileService: Title, area: string) {
        tileService.setTitle(`D3S - ${area}`);
    }

    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        console.log('show/hide ' + activatedItem);
    }    
}