import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { Title } from '@angular/platform-browser';
import { BaseComponent } from '../shared/base.component';
import { Subscription }   from 'rxjs';
import { SecondaryNavItem } from '../../models/secondaryNav.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';
import { CompanySettingsService } from '../../services/settings.service';

export class AssetGridBaseComponent extends BaseComponent {            
    public area: string = StringConstants.AssetTypeClass_Business;
    public folderTitle: string; 
    public areaLink: string = SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT;
    //sidebar
    sidebarSubscription: Subscription;
    
    constructor(
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        secondaryNavService?: SecondaryNavService,
        webAnalyticsService?: WebAnalyticsService) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.webAnalyticsService = webAnalyticsService;
        this.breadcrumbsService = headerBreadcrumbService;
        this.secondaryNavService.showHeader(true);
    }        
    
    protected showHideBreadcrumbItem(activatedItem: SecondaryNavItem) {
        
    }    
}