import { Input, Component, OnInit, OnDestroy} from '@angular/core';
import { BaseComponent } from '../base.component';
import { SiteMenuService, AuthenticationService, StateService, FavoritesService, HeaderActionsService } from '../../../services/index';
import { SiteMenu, SiteMenuItem } from '../../../models/site-menu.model';
import { Favorite } from '../../../models/favorite.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-site-menu',    
    template: ` 
                <ul class="left-side-nav">
                    <d3s-site-menu-category [url]="modelRootUrl()" rootToolTip="Models" rootIconName="fa-sitemap" [menu]="modelMenu"></d3s-site-menu-category>
                    <d3s-site-menu-category [url]="glossaryRootUrl()" rootToolTip="Glossary" rootIconName="fa-book" [menu]="glossaryMenu"></d3s-site-menu-category>
                    <d3s-site-menu-category [url]="monitorUrl()" rootToolTip="Monitor" rootIconName="fa-dashboard"></d3s-site-menu-category>
                    <d3s-site-menu-category rootToolTip="Policies" rootIconName="fa-university" [menu]="policiesMenu"></d3s-site-menu-category>
                    <d3s-site-menu-category rootToolTip="Data Quality" rootIconName="fa-pie-chart" [menu]="dataQualityMenu"></d3s-site-menu-category>
                    <d3s-site-menu-category [url]="referenceUrl()" rootToolTip="Reference" rootIconName="fa-cubes"></d3s-site-menu-category>
                    <d3s-site-menu-category [url]="fusionUrl()" rootToolTip="Fusion" rootIconName="fa-database"></d3s-site-menu-category>
                    <d3s-site-menu-category [url]="communityUrl()" rootToolTip="Community" rootIconName="fa-group"></d3s-site-menu-category>
                    <d3s-site-menu-category *ngIf="isAdmin" rootToolTip="Administration" rootIconName="fa-cog" [menu]="adminMenu"></d3s-site-menu-category>
                    <d3s-site-menu-category *ngIf="favorites" [menu]="favorites" rootToolTip="Favorites" rootIconName="fa-star"></d3s-site-menu-category>
                </ul>
                `,    
    providers: [SiteMenuService, FavoritesService],
})

export class SiteMenuComponent extends BaseComponent implements OnInit, OnDestroy {
        
    private isAdmin: boolean = false;
    private siteMenu: SiteMenu[] = [];
    private favorites: SiteMenu;

    private glossaryMenu: SiteMenu;
    private modelMenu: SiteMenu;
    private policiesMenu: SiteMenu;
    private dataQualityMenu: SiteMenu;
    private adminMenu: SiteMenu;  
    private subSiteNav: any;
    private subFavorites: any;  

    constructor(private headerActionsService: HeaderActionsService, private authenticationService: AuthenticationService, private siteMenuService: SiteMenuService, private favoritesService: FavoritesService) {
        super();
    }

    ngOnInit() {
        this.loadMenu();
        this.loadFavorites();

        this.subSiteNav = this.headerActionsService.onSiteNavChanges$.subscribe(() => {
            this.loadMenu();
        });

        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(s => {
            this.loadFavorites();
        });
    }

    ngOnDestroy() {
        this.subSiteNav.unsubscribe();
        this.subFavorites.unsubscribe();
    }

    loadFavorites(){        
        this.favoritesService.getFavorites().then(favorites => {            
            this.favorites = new SiteMenu();
            this.favorites.NavigationItems = [];
            for (let favorite of favorites) {
                this.favorites.NavigationItems.push({
                    Name: favorite.Name,
                    Url: favorite.Route,
                    IsLink: false,
                    Items: null
                });
            }
        });        
    }

    loadMenu() {
        this.siteMenuService.getMenu()
            .then(result => {                
                this.siteMenu = result.MenuItems;

                this.glossaryMenu = this.siteMenu.filter(x => x.MenuID == '#Glossary')[0];                
                this.modelMenu = this.siteMenu.filter(i => i.MenuID == '#Models')[0];
                this.policiesMenu = this.siteMenu.filter(i => i.MenuID == '#Policy')[0];
                this.dataQualityMenu = this.siteMenu.filter(i => i.MenuID == '#Data Quality')[0];

                if (result.IsAdmin) this.buildAdminMenu();

                // used to enable guard that allows access to administrative routes                
                this.authenticationService.admin$.next(result.IsAdmin);
                this.authenticationService.admin$.complete();

                this.isAdmin = result.IsAdmin;
            });
    }

    private fusionUrl() {
        return SiteUrlHelpers.SITE_URL_FUSION_ROOT;
    }

    private communityUrl() {
        return SiteUrlHelpers.SITE_URL_COMMUNITY_ROOT;
    }

    private referenceUrl() {
        return SiteUrlHelpers.SITE_URL_REFERENCE_ROOT;
    }

    private monitorUrl() {
        return SiteUrlHelpers.SITE_URL_MONITOR_ROOT;
    }

    private modelRootUrl() {
        return `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION}`;
    }

    private glossaryRootUrl() {
        return SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT;
    }

    private buildAdminMenu() {
        this.adminMenu = new SiteMenu();
        this.adminMenu.NavigationItems = [];

        let metaMenu = new SiteMenuItem();
        metaMenu.Name = "MetaModel";
        metaMenu.Items = [];
        metaMenu.Items.push({ Name: 'Artifacts', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ARTIFACTS}`, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Attributes', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ATTRIBUTES}`, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Lookups', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_LOOKUPS}`, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Models', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_MODELS}`, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Policies', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_POLICIES}`, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Relationship Types', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RELATIONSHIPS}`, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Rules', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RULES}`, Items: null, IsLink: false });
        metaMenu.Items.push({ Name: 'Surveys', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SURVEYS}`, Items: null, IsLink: false });
        this.adminMenu.NavigationItems.push(metaMenu);
        
        let integrationMenu = new SiteMenuItem();
        integrationMenu.Name = "Integration";
        integrationMenu.Items = [];
        integrationMenu.Items.push({ Name: 'API', Url: '/swagger/ui/index', Items: null, IsLink: true});
        integrationMenu.Items.push({ Name: 'Bulk Loader', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_BULK_LOAD}`, Items: null, IsLink: false});
        integrationMenu.Items.push({ Name: 'Fusion', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_FUSION}`, Items: null, IsLink: false});

        this.adminMenu.NavigationItems.push(integrationMenu);
                

        let metricsMenu = new SiteMenuItem();
        metricsMenu.Name = "Metrics";
        metricsMenu.Items = [];
        metricsMenu.Items.push({ Name: 'Analytics', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ANALYTICS}`, Items: null, IsLink: false});
        metricsMenu.Items.push({ Name: 'Dashboard', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_DASHBOARDS}`, Items: null, IsLink: false});
        this.adminMenu.NavigationItems.push(metricsMenu);

        let securityMenu = new SiteMenuItem();
        securityMenu.Name = "Security";
        securityMenu.Items = [];

        securityMenu.Items.push({ Name: 'Groups', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_GROUPS}`, Items: null, IsLink: false});
        securityMenu.Items.push({ Name: 'Responsibilities', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RESPONSIBILITIES}`, Items: null, IsLink: false});
        securityMenu.Items.push({ Name: 'Users', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RESOURCES}`, Items: null, IsLink: false});

        this.adminMenu.NavigationItems.push(securityMenu);
                
        this.adminMenu.NavigationItems.push({ Name: 'Settings', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SETTINGS}`, IsLink: false });
        this.adminMenu.NavigationItems.push({ Name: 'Templates', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_TEMPLATES}`, IsLink: false });
        this.adminMenu.NavigationItems.push({ Name: 'Workflow', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_WORKFLOW}`, IsLink: false });
        
    }
};