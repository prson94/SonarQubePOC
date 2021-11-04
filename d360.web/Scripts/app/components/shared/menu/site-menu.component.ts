import { Input, Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, ViewChildren, QueryList, ViewEncapsulation, ViewChild, ElementRef, HostListener, AfterContentInit } from '@angular/core';
import { BaseComponent } from '../base.component';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { StateService } from '../../../services/state.service';
import { FavoritesService } from '../../../services/favorites.service';
import { AuthenticationService } from '../../../services/authentication.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { SiteMenu, SiteMenuItem, SiteMenuModel, NavigationState } from '../../../models/site-menu.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import * as _ from 'lodash';
import { SiteMenuCategoryComponent } from './site-menu-category.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from "../../../static/string-constants";
import { ActivatedRoute } from '@angular/router';
import { CompanySettingsService } from '../../../services/settings.service';
import { CompanySettingEnum } from '../../../models/settings.model';

@Component({
    selector: 'd3s-site-menu',
    templateUrl: './site-menu.component.html',
    providers: [SiteMenuService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['./site-menu.less'],

})

export class SiteMenuComponent extends BaseComponent implements OnInit, OnDestroy, AfterContentInit {
    @Input() menuOpen: boolean;
    @Output() menuChanged = new EventEmitter<boolean>();

    public hideNav: boolean = false;
    public isAdmin: boolean = false;
    public siteMenu: SiteMenu[] = [];
    public favorites: SiteMenu;

    private adminMenu: SiteMenu;
    private configMenu: SiteMenu;
    private subSiteNav: any;
    private subFavorites: any;
    private subParams: any;

    private subReloadCounts: any;
    protected countData: any[];

    isScrollerVisable: boolean = false;
    scrollingUp: boolean = false;
    scrollTitle: string = "Scroll down";

    @ViewChildren(SiteMenuCategoryComponent) menuRefs: QueryList<SiteMenuCategoryComponent>;
    @ViewChild("menu", { static: false }) menu: ElementRef;
    isMenuActive: boolean;

    @HostListener('document:click', ['$event'])
    documentClick(event: MouseEvent) {
        this.isMenuActive = false;
    }

    constructor(
        private authenticationService: AuthenticationService,
        private favoritesService: FavoritesService,
        private headerActionsService: HeaderActionsService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private siteMenuService: SiteMenuService,
        private stateService: StateService,
        private ref: ChangeDetectorRef,
        private route: ActivatedRoute
    ) {
        super(settingsService);
    }    

    ngAfterContentInit(): void {
        this.checkScroller();
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

        this.subParams = this.route.queryParams.subscribe((params) => {
            let markForCheck = false;

            if (params['nonavigation'] != null) {
                this.hideNav = params['nonavigation'].toLocaleLowerCase() === 'true';
                markForCheck = true;
            }

            if (markForCheck) {
                this.ref.markForCheck();
            }
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
        if (this.subSiteNav) {
            this.subSiteNav.unsubscribe();
        }
        if (this.subFavorites) {
            this.subFavorites.unsubscribe();
        }
        if (this.subParams) {
            this.subParams.unsubscribe();
        }
    }

    doScroll() {
        if (this.menu && this.isScrollerVisable) {
            let elem = this.menu.nativeElement;
            let scrollDistance = (elem.offsetHeight - 120);
            if (this.scrollingUp) {
                elem.scrollTop -= scrollDistance;
            } else {
                elem.scrollTop += scrollDistance;
            }
        }
    }

    delayedCheckScrollerPos = _.debounce(() => {
        this.checkScrollerPos();
    }, 50);

    checkScrollerPos() {
        let elem = this.menu.nativeElement;
        let top = elem.scrollTop;
        let max = this.menuOpen ? (elem.scrollHeight - elem.offsetHeight) - 5 : (elem.scrollHeight - elem.offsetHeight) - 45;
        if (this.scrollingUp == true) {
            top = Math.ceil(top);
            max = Math.ceil(max);
        } else {
            top = Math.floor(top);
            max = Math.floor(max);
        }
        if (top >= (max) && top != 0) {
            this.scrollingUp = true;
            this.scrollTitle = "Scroll up";
            this.ref.markForCheck();
        } else if (top <= 0) {
            this.scrollingUp = false;
            this.scrollTitle = "Scroll down";
            this.ref.markForCheck();
        }

    }

    checkScroller() {
        if (this.menu) {
            let elem = this.menu.nativeElement;
            this.isScrollerVisable = (elem.clientHeight < elem.scrollHeight);
        }
    }
    @HostListener('scroll', ['$event'])
    onElementScroll($event) {
        this.delayedCheckScrollerPos();
    }
    @HostListener('window:resize', ['$event'])
    onResize(event) {
        this.checkScroller();
    }

    loadFavorites() {
        if (this.getBooleanSetting(CompanySettingEnum.ShowFavorites)) {
            return;
        }

        this.favoritesService.getHomePageAndFavorites().subscribe(
            homefav => {
                this.favorites = new SiteMenu();
                this.favorites.MenuID = StringConstants.MenuId_Favorites;
                this.favorites.NavigationItems = [];

                for (let favorite of homefav.Favorites) {
                    let isHomePage = _.isEqual(favorite, homefav.Homepage);
                    this.favorites.NavigationItems.push({
                        Name: favorite.Name,
                        Url: favorite.Route,
                        IsLink: false,
                        Items: null,
                        IsHomePage: isHomePage,
                        count: null
                    });
                }

                this.ref.markForCheck();
            }
        );
    }

    loadMenu() {

        let navigationState: NavigationState[] = JSON.parse(localStorage.getItem("NavigationMenu")) ? JSON.parse(localStorage.getItem("NavigationMenu")) : [];

        this.siteMenuService.getMenu().subscribe(
            result => {

                // used to enable guard that allows access to administrative routes                                
                this.authenticationService.isAdmin = result.IsAdmin;
                this.isAdmin = result.IsAdmin;

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
                                    menu.ngUrl = `${SiteUrlHelpers.SITE_URL_MODEL_ROOT}/${SiteUrlHelpers.SITE_URL_HIERARCHY_CLASSIFICATION}`;
                                    break;
                                case '#Policy':
                                    menu.ngUrl = `${SiteUrlHelpers.SITE_URL_POLICY_ROOT}/${SiteUrlHelpers.SITE_URL_HIERARCHY_CLASSIFICATION}`;
                                    break;
                                case '#Data Quality':
                                    break;
                                case '#Monitor':
                                    menu.NavigationItems = [];
                                    menu.ngUrl = SiteUrlHelpers.SITE_URL_MONITOR_ROOT;
                                    menu.ShouldDisplay = (!this.getBooleanSetting(CompanySettingEnum.DisableIssueManagement));
                                    break;
                                case '#Reference':
                                    menu.NavigationItems = [];
                                    menu.ngUrl = SiteUrlHelpers.SITE_URL_REFERENCE_ROOT;
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

                    //add logic around the admin menus starting as expanded
                    //only add if it doesn't already exist.
                    if (!navigationState.some(x => x.SiteMenuID == this.adminMenu.MenuID)) {
                        navigationState.push(
                            {
                                SiteMenuID: this.adminMenu.MenuID,
                                DisplayElements: [
                                    { ParentUrl: null, Url: this.adminMenu.NavigationItems.find(item => item.Name == "Integration").Name },
                                    { ParentUrl: null, Url: this.adminMenu.NavigationItems.find(item => item.Name == "Security").Name }
                                ]
                            });
                    }
                }

                localStorage.setItem("NavigationMenu", JSON.stringify(navigationState));
                window.setTimeout(() => { this.checkScroller(); this.ref.markForCheck(); }, 250);
                this.ref.markForCheck();
            }).add(() => {
                //set the nav state for each of the siteMenu elements
                this.siteMenuService.getCounts().subscribe((res) => {
                    this.siteMenu.forEach(menu => {
                        this.setNavState(navigationState, menu.NavigationItems, menu.MenuID, menu.ngUrl);
                        this.loadCounts(menu, res);
                    });
                    //set the nav state for the admin menu elements
                    if (this.adminMenu)
                        this.setNavState(navigationState, this.adminMenu.NavigationItems, this.adminMenu.MenuID, this.adminMenu.ngUrl);
                    else
                        this.setNavState(navigationState, [], null, null);
                });
            });
    }

    loadCounts(menu: any, items: any[]) {
        if (menu && menu.NavigationItems && menu.NavigationItems.length > 0 && !menu.MenuID.startsWith('-')) {
            menu.NavigationItems.forEach((item) => this.getAllCounts(item, items));
        }
    }

    getAllCounts(items, arr: any[]) {
        if (_.isString(items.Name) && _.isString(items.Url) && items.Url.indexOf('/') !== -1) {
            //get count for item
            var id = _.findIndex(arr, function (o) {
                let currentURL = items.Url.toLowerCase();
                currentURL = items.Url.replace('model', 'taxonomy');
                return o.Name == items.Name
                    && _.includes(currentURL, o.Object.toLowerCase().replace('type', ''))
                    && _.includes(currentURL, '/' + o.ObjectID);
            });
            if (id !== -1) {
                items.count = arr[id].count;
            } else {
                items.count = 0;
            }
        }

        //check if sub items exist
        if (_.isArray(items.Items)) {
            //recursively check sub items
            items.Items.forEach((item) => this.getAllCounts(item, arr));
        }
    }

    protected clearFavorites() {
        this.favoritesService.deleteCurrentUsersFavoritesV2().subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.loadFavorites(); // reload favorites because the user could still have global favorites.
                this.headerActionsService.emitFavoritesChange()
            }
        );
    }

    toggleMenu() {
        this.menuOpen = !this.menuOpen;
        this.menuChanged.emit(this.menuOpen);
    }

    changeActiveMenu($event) {
        this.isMenuActive = false;

        if ($event) {
            this.menuRefs.forEach((item) => {
                if ($event.item.title != item.title) {
                    if (item.menu)
                        item.menu.isActiveItem = false;
                } else {
                    if (item.menu && item.menu.NavigationItems && item.menu.NavigationItems.length > 0) { 
                        item.menu.isActiveItem = true;
                        this.isMenuActive = true;
                    }
                }
            });
        }
        this.ref.detectChanges();
    }

    buildConfigMenu() {

        this.configMenu = new SiteMenu();
        this.configMenu.MenuID = '-Config';
        this.configMenu.NavigationItems = [];

        this.configMenu.NavigationItems.push({ Name: 'Business Assets', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_BUSINESS}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Technical Assets', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_TECHNICAL}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Diagram Assets', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_DIAGRAM_ASSETS}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Models', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_MODELS}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Policies', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_POLICIES}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Rules', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RULES}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Relationships', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RELATIONSHIPS}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Predicates', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_PREDICATES}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Workflows', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_WORKFLOW}`, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Workflow Actions', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ISSUE_TYPES}`, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Scoring Definitions', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SCORING}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        this.configMenu.NavigationItems.push({ Name: 'Surveys', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SURVEYS}`, Items: null, IsLink: false, IsHomePage: false, count: null });
    }

    private buildAdminMenu() {
        this.adminMenu = new SiteMenu();
        this.adminMenu.MenuID = '-Admin';
        this.adminMenu.NavigationItems = [];

        let integrationMenu = new SiteMenuItem();
        integrationMenu.Name = "Integration";
        integrationMenu.Items = [];
        integrationMenu.Items.push({ Name: 'API', Url: '/swagger/ui/index', Items: null, IsLink: true, IsHomePage: false, count: null });
        integrationMenu.Items.push({ Name: 'Bulk Loader', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_BULK_LOAD}`, Items: null, IsLink: false, IsHomePage: false, count: null  });
        if (this.getBooleanSetting(CompanySettingEnum.ShowCustomAPIAdmin)) {
            integrationMenu.Items.push({ Name: 'Custom API', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_CUSTOM_API}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        }
        this.adminMenu.NavigationItems.push(integrationMenu);

        let securityMenu = new SiteMenuItem();
        securityMenu.Name = "Security";
        securityMenu.Items = [];

        securityMenu.Items.push({ Name: 'Groups', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_GROUPS}`, Items: null, IsLink: false, IsHomePage: false, count: null });

        if (this.getBooleanSetting(CompanySettingEnum.EnableOrganizations)) {
            securityMenu.Items.push({ Name: 'Organizations', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ORGANIZATIONS}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        }
        securityMenu.Items.push({ Name: 'Responsibilities', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RESPONSIBILITIES}`, Items: null, IsLink: false, IsHomePage: false, count: null });
        securityMenu.Items.push({ Name: 'Users', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RESOURCES}`, Items: null, IsLink: false, IsHomePage: false, count: null });

        this.adminMenu.NavigationItems.push(securityMenu);

        this.adminMenu.NavigationItems.push({ Name: 'Settings', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SETTINGS}`, IsLink: false, IsHomePage: false, count: null });

        this.adminMenu.NavigationItems.push({ Name: 'Export Templates', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_EXPORT_TEMPLATES}`, IsLink: false, IsHomePage: false, count: null });

        this.adminMenu.NavigationItems.push({ Name: 'Dashboards', Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_DASHBOARDS}`, Items: null, IsLink: false, IsHomePage: false, count: null });

        this.adminMenu.NavigationItems.push({ Name: 'Branding', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_CUSTOMIZATIONS}`, IsLink: false, IsHomePage: false, count: null });

        this.adminMenu.NavigationItems.push({ Name: 'Tags', Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_TAGS}`, IsLink: false, IsHomePage: false, count: null });

    }

    private setNavState(currentNavState: NavigationState[], menuItems: SiteMenuItem[], siteMenuID: string, parentUrl: string) {
        menuItems.forEach(menuItem => {
            if (!menuItem.ShowChildren) {
                menuItem.ShowChildren = currentNavState.some(y => y.SiteMenuID == siteMenuID && y.DisplayElements.findIndex(element => (element.Url == menuItem.Url) || (!element.ParentUrl && element.Url == menuItem.Name)) >= 0);

                if (menuItem.Items && menuItem.Items.length > 0) {
                    this.setNavState(currentNavState, menuItem.Items, siteMenuID, menuItem.Url);
                }
            }
        });
    }
};
