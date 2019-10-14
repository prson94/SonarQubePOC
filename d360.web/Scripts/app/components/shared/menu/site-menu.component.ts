import { Input, Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, AfterViewInit, ViewChildren, ElementRef, ContentChildren, ViewChild, QueryList} from '@angular/core';
import { BaseComponent } from '../base.component';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { StateService } from '../../../services/state.service';
import { FavoritesService } from '../../../services/favorites.service';
import { AuthenticationService } from '../../../services/authentication.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { SiteMenu, SiteMenuItem } from '../../../models/site-menu.model';
import { Favorite } from '../../../models/favorite.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import * as _ from 'lodash';
import { isString, isArray } from 'util';
import { Element } from '@angular/compiler';
import { SiteMenuCategoryComponent } from './site-menu-category.component';
import { forEach } from '@angular/router/src/utils/collection';
import { MessagesObservableService } from '../../../services/messages-observable.service';

declare var CompanySettings;

@Component({
    selector: 'd3s-site-menu',
    templateUrl: './site-menu.component.html',
    providers: [SiteMenuService, FavoritesService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class SiteMenuComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() menuOpen: boolean;
    @Output() menuChanged = new EventEmitter<boolean>();

    public isAdmin: boolean = false;
    public siteMenu: SiteMenu[] = [];
    public favorites: SiteMenu;
    
    private adminMenu: SiteMenu;
    private configMenu: SiteMenu;
    private subSiteNav: any;
    private subFavorites: any;

    private subReloadCounts: any;
    private countData: any[];

    @ViewChildren(SiteMenuCategoryComponent) menuRefs: QueryList<SiteMenuCategoryComponent>;

    constructor(
        private ref: ChangeDetectorRef,
        private messagesService: MessagesObservableService,
        private stateService: StateService,
        private headerActionsService: HeaderActionsService,
        private authenticationService: AuthenticationService,
        private siteMenuService: SiteMenuService,
        private favoritesService: FavoritesService
    ) {
        super();
    }

    ngOnInit() {
        this.loadMenu();
        this.loadFavorites();

        this.subReloadCounts = this.headerActionsService.onSiteCountsChange.subscribe(() => {
            this.rebuildCounts();
        });

        this.subSiteNav = this.stateService.siteMenuRequiresReload$.subscribe(() => {
            this.loadMenu();
        });

        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(() => {
            this.loadFavorites();
        });
    }

    private rebuildCounts() {
        this.siteMenuService.getCounts().subscribe((res) => {
            this.countData = res;
            this.siteMenu.forEach(menu => {
                this.loadCounts(menu, res);
            });
        });
    }

    ngOnDestroy() {
        this.subSiteNav.unsubscribe();
        this.subFavorites.unsubscribe();
    }

    clearSearches($event) {

        this.menuRefs.forEach((item) =>
        {
            if ($event.item.title != item.title) {
                if (item.menu)
                    item.menu.isActiveItem = false;
                item.clearInput();
            }
        });
    }

    loadFavorites() {
        if (CompanySettings.ShowFavorites == 'false') {
            return;
        }

        this.favoritesService.getFavorites().subscribe(
            favorites => {
            favorites = _.sortBy(favorites, 'SortOrder'); // sort the favorites
            this.favorites = new SiteMenu();
            this.favorites.MenuID = '*Favourites';
            this.favorites.NavigationItems = [];

            for (let favorite of favorites) {
                this.favorites.NavigationItems.push({
                    Name: favorite.Name,
                    Url: favorite.Route,
                    IsLink: false,
                    Items: null,
                    IsHomePage: favorite.IsHomePage,
                    count: null
                });
            }

            this.ref.markForCheck();
        }
        );
    }

    loadMenu() {
        this.siteMenuService.getMenu().subscribe(
            result => {
                result.MenuItems = result.MenuItems.filter(x => (x.MenuID != '#Admin')); //remove admin menu it will get built later.

                // add properties we need to add to the burned in menus
                for (let menu of result.MenuItems) {
                    menu.ShouldDisplay = true;

                    switch (menu.MenuID) {
                        case '#Business':
                            menu.ngUrl = `${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/assets/BusinessAsset`;
                            break;
                        case '#Technical':
                            menu.ngUrl = `${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/assets/TechnicalAsset`;
                            break;
                        case '#Models':
                            menu.ngUrl = `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${SiteUrlHelpers.SITE_URL_MODEL_CLASSIFICATION}`;
                            break;
                        case '#Policy':
                            menu.ngUrl = `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${SiteUrlHelpers.SITE_URL_POLICY_CLASSIFICATION}`;
                            break;
                        case '#Data Quality':
                            break;
                        case '#Monitor':
                            menu.NavigationItems = [];
                            menu.ngUrl = SiteUrlHelpers.SITE_URL_MONITOR_ROOT;
                            menu.ShouldDisplay = (CompanySettings.DisableIssueManagement != 'true');
                            break;
                        case '#Reference':
                            menu.NavigationItems = [];
                            menu.ngUrl = SiteUrlHelpers.SITE_URL_REFERENCE_ROOT;
                            break;
                        case '#Fusion':
                            menu.NavigationItems = [];
                            menu.ngUrl = SiteUrlHelpers.SITE_URL_FUSION_ROOT;
                            break;
                        case '#Community':
                            menu.NavigationItems = [];
                            menu.ngUrl = SiteUrlHelpers.SITE_URL_COMMUNITY_ROOT;
                            break;
                        case '#Dashboards':
                            menu.ngUrl = SiteUrlHelpers.SITE_URL_DASHBOARD_ROOT;
                            break;
                        default:
                            //is it a custom menu?
                            if (menu.MenuID.startsWith('~')) {
                                if (!menu.Title) menu.Title = menu.MenuID.replace('~', '');
                            }
                            break;
                    }
                    if (!menu.Icon && !menu.FullURL) {
                        menu.Icon = 'fa-folder';
                    }

                }
               
                this.siteMenu = _.sortBy(result.MenuItems, 'SortOrder'); // sort the menu's by display order

                if (result.IsAdmin) {
                    this.buildConfigMenu();
                    this.buildAdminMenu();
                }

                // used to enable guard that allows access to administrative routes                                
                this.authenticationService.isAdmin = result.IsAdmin;
                this.isAdmin = result.IsAdmin;

                this.ref.markForCheck();
            }).add(() => {
                this.siteMenuService.getCounts().subscribe((res) => {
                    this.siteMenu.forEach(menu => {
                        this.loadCounts(menu, res);
                    });
                });
            });
    }


    loadCounts(menu: any, items: any[]) {
        if (menu && menu.NavigationItems && menu.NavigationItems.length > 0 && !menu.MenuID.startsWith('-')) {
                menu.NavigationItems.forEach((item) => this.getAllCounts(item, items));
        }
    }

    getAllCounts(items, arr:any[]) {
        if (isString(items.Name) && isString(items.Url) && items.Url.indexOf('/') != -1) {
            //get count for item
            var id = _.findIndex(arr, function (o) {
                let currentURL = items.Url.toLowerCase();
                currentURL = items.Url.replace('model', 'taxonomy');
                return o.Name == items.Name
                    && _.includes(currentURL, o.Object.toLowerCase().replace('type', ''))
                    && _.includes(currentURL, o.ObjectID);
            });
            if (id !== -1) {
                items.count = arr[id].count;
            } else {
                items.count = 0;
            }
        }

        //check if sub items exist
        if (isArray(items.Items)) {
            //recursively check sub items
            items.Items.forEach((item) => this.getAllCounts(item, arr));
        }
    }

    private clearFavorites() {
        this.favoritesService.deleteCurrentUsersFavorites().subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.loadFavorites(); // reload favorites because the user could still have global favorites.
                this.headerActionsService.emitFavoritesChange()
            }
        );
    }

    private toggleMenu() {
        this.menuOpen = !this.menuOpen;
        this.menuChanged.emit(this.menuOpen);
    }

    buildConfigMenu() {

        this.configMenu = new SiteMenu();
        this.configMenu.MenuID = '-Config';
        this.configMenu.NavigationItems = [];

        this.configMenu.NavigationItems.push({ Name: 'Business Assets', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_BUSINESS}`, Items: null, IsLink: false, IsHomePage: false, count: null });

        if (+CompanySettings.LineageVersion == 3 && CompanySettings.FusionEnabled != 'true') {
            this.configMenu.NavigationItems.push({ Name: 'Technical Assets', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_TECHNICAL}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        }

        this.configMenu.NavigationItems.push({ Name: 'Models', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_MODELS}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Policies', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_POLICIES}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Rules', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RULES}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Relationships', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RELATIONSHIPS}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Predicates', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_PREDICATES}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Workflows', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_WORKFLOW}`, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Workflow Actions', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ISSUE_TYPES}`, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Attributes', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ATTRIBUTES}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Surveys', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SURVEYS}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Lookups', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_LOOKUPS}`, Items: null, IsLink: false, IsHomePage: false, count: null });
    }

    private buildAdminMenu() {
        this.adminMenu = new SiteMenu();
        this.adminMenu.MenuID = '-Admin';
        this.adminMenu.NavigationItems = [];

        let integrationMenu = new SiteMenuItem();
        integrationMenu.Name = "Integration";
        integrationMenu.Items = [];
        integrationMenu.Items.push({ Name: 'API', Url: '/swagger/ui/index', Items: null, IsLink: true, IsHomePage: false, count: null });
        integrationMenu.Items.push({ Name: 'Bulk Loader', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_BULK_LOAD}`, Items: null, IsLink: false, IsHomePage: false, count:null  });
        if (CompanySettings.ShowCustomAPIAdmin != 'false') integrationMenu.Items.push({ Name: 'Custom API', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_CUSTOM_API}`, Items: null, IsLink: false, IsHomePage: false, count:null });

        if (CompanySettings.FusionEnabled != 'false')
            integrationMenu.Items.push({ Name: 'Fusion', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_FUSION}`, Items: null, IsLink: false, IsHomePage: false, count: null });

        this.adminMenu.NavigationItems.push(integrationMenu);

        let securityMenu = new SiteMenuItem();
        securityMenu.Name = "Security";
        securityMenu.Items = [];

        securityMenu.Items.push({ Name: 'Groups', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_GROUPS}`, Items: null, IsLink: false, IsHomePage: false, count:null });

        if (CompanySettings.EnableOrganizations)
            securityMenu.Items.push({ Name: 'Organizations', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ORGANIZATIONS}`, Items: null, IsLink: false, IsHomePage: false, count:null });

        securityMenu.Items.push({ Name: 'Responsibilities', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RESPONSIBILITIES}`, Items: null, IsLink: false, IsHomePage: false, count:null });
        securityMenu.Items.push({ Name: 'Users', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RESOURCES}`, Items: null, IsLink: false, IsHomePage: false, count:null });

        this.adminMenu.NavigationItems.push(securityMenu);

        this.adminMenu.NavigationItems.push({ Name: 'Settings', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SETTINGS}`, IsLink: false, IsHomePage: false, count:null });

        this.adminMenu.NavigationItems.push({ Name: 'Export Templates', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_EXPORT_TEMPLATES}`, IsLink: false, IsHomePage: false, count:null });

        this.adminMenu.NavigationItems.push({ Name: 'Scoring', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ANALYTICS}`, Items: null, IsLink: false, IsHomePage: false, count:null });

        this.adminMenu.NavigationItems.push({ Name: 'Dashboards', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_DASHBOARDS}`, Items: null, IsLink: false, IsHomePage: false, count: null });
               
        this.adminMenu.NavigationItems.push({ Name: 'Branding', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_CUSTOMIZATIONS}`, IsLink: false, IsHomePage: false, count:null });

        this.adminMenu.NavigationItems.push({ Name: 'Tags', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_TAGS}`, IsLink: false, IsHomePage: false, count: null });

    }
};
