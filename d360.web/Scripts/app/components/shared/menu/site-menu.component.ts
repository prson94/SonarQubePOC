import { Input, Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, ViewChildren, QueryList, ViewEncapsulation, ViewChild, ElementRef, HostListener, AfterContentInit } from '@angular/core';
import { BaseComponent } from '../base.component';
import { StateService } from '../../../services/state.service';
import { AuthenticationService } from '../../../services/authentication.service';
import { SiteMenuService } from '../../../services/site-menu.service';
import { SiteMenu, SiteMenuItem, NavigationState } from '../../../models/site-menu.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import * as _ from 'lodash';
import { SiteMenuCategoryComponent } from './site-menu-category.component';
import { ActivatedRoute } from '@angular/router';
import { CompanySettingsService } from '../../../services/settings.service';
import { CompanySettingEnum } from '../../../models/settings.model';
import { SiteMenuFavoritesComponent } from './site-menu-favorites.component';
import { Subject } from 'rxjs';
import { FeatureFlags, FeatureFlagsService } from '../../../services/featureflags.service';

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

    private adminMenu: SiteMenu;
    private configMenu: SiteMenu;
    private subSiteNav: any;
    private subParams: any;

    protected countData: any[];

    isScrollerVisable: boolean = false;
    scrollingUp: boolean = false;
    scrollTitle: string = $localize`Scroll down`;

    @ViewChildren(SiteMenuCategoryComponent) menuRefs: QueryList<SiteMenuCategoryComponent>;
    @ViewChildren(SiteMenuFavoritesComponent) favoritesMenuRefs: QueryList<SiteMenuFavoritesComponent>;
    @ViewChild("menu", { static: false }) menu: ElementRef;
    @ViewChild("menuBottomPadding", { static: false }) menuBottomPadding: ElementRef;
    isMenuActive: boolean;

    @HostListener('document:click', ['$event'])
    documentClick(event: MouseEvent) {
        this.isMenuActive = false;
    }

    public activeMenu$ = new Subject();

    constructor(
        private authenticationService: AuthenticationService,
        protected settingsService: CompanySettingsService,
        private siteMenuService: SiteMenuService,
        private stateService: StateService,
        private ref: ChangeDetectorRef,
        private route: ActivatedRoute,
        private featureFlagService: FeatureFlagsService
    ) {
        super(settingsService);
    }

    ngAfterContentInit(): void {
        this.checkScroller();
    }

    ngOnInit() {
        this.loadMenu();


        this.subSiteNav = this.stateService.siteMenuRequiresReload$.subscribe(() => {
            this.loadMenu();
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
            this.scrollTitle = $localize`Scroll up`;
            this.ref.markForCheck();
        } else if (top <= 0) {
            this.scrollingUp = false;
            this.scrollTitle = $localize`Scroll down`;
            this.ref.markForCheck();
        }

    }

    checkScroller() {
        if (!this.menu) {
            return;
        }

        const paddingElement = this.menuBottomPadding.nativeElement as HTMLDivElement;
        const paddingHeight = paddingElement.clientHeight;

        const menu = this.menu.nativeElement as HTMLUListElement;
        const heightOfContent = menu.scrollHeight - paddingHeight;

        this.isScrollerVisable = (menu.clientHeight < heightOfContent);
        this.ref.markForCheck();
    }

    @HostListener('scroll', ['$event'])
    onElementScroll($event) {
        this.delayedCheckScrollerPos();
    }
    @HostListener('window:resize', ['$event'])
    onResize(event) {
        this.checkScroller();
    }

    loadMenu() {

        let navigationState: NavigationState[] = JSON.parse(localStorage.getItem("NavigationMenu")) ? JSON.parse(localStorage.getItem("NavigationMenu")) : [];

        this.siteMenuService.getMenu().subscribe(
            result => {

                // used to enable guard that allows access to administrative routes                                
                this.authenticationService.isAdmin = result.IsAdmin;
                this.isAdmin = result.IsAdmin;

                result.MenuItems = result.MenuItems.filter(x => (x.MenuID != '#Admin')); //remove admin menu it will get built later.
                if (!this.featureFlagService.flags[FeatureFlags.SemanticTypesUiFlag]) {
                    result.MenuItems = result.MenuItems.filter((x) => (x.MenuID !== '#SemanticTypes'));
                }
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
                        case '#SemanticTypes':
                            menu.NavigationItems = [];
                            menu.ngUrl = SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT;
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

    toggleMenu() {
        this.menuOpen = !this.menuOpen;
        this.menuChanged.emit(this.menuOpen);
        this.checkScroller();
    }

    changeActiveMenu($event) {
        this.isMenuActive = false;
        [...Array.from(this.menuRefs), ...Array.from(this.favoritesMenuRefs)].forEach((item) => {
            if ($event?.item?.title == item.title) {
                if (item.menu) {
                    item.menu.isActiveItem = true;
                    this.setIsMenuActive(item);
                }
            } else {
                if (item.menu) {
                    item.menu.isActiveItem = false;
                }
            }
        });
        this.activeMenu$.next($event?.item?.menu);
        this.ref.detectChanges();
    }

    setIsMenuActive(item) {
        if (item.menu.NavigationItems?.length) {
            this.isMenuActive = true;
        }
    }

    buildConfigMenu() {

        this.configMenu = new SiteMenu();
        this.configMenu.MenuID = '-Config';
        this.configMenu.NavigationItems = [];

        this.configMenu.NavigationItems.push({ Name: $localize`Business Assets`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_BUSINESS}`, Items: null, IsLink: false, count: null });
        this.configMenu.NavigationItems.push({ Name: $localize`Technical Assets`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET}/${SiteUrlHelpers.SITE_URL_ADMIN_ASSET_TECHNICAL}`, Items: null, IsLink: false, count: null });
        this.configMenu.NavigationItems.push({ Name: $localize`Diagram Assets`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_DIAGRAM_ASSETS}`, Items: null, IsLink: false, count: null });
        this.configMenu.NavigationItems.push({ Name: $localize`Models`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_MODELS}`, Items: null, IsLink: false, count: null });
        this.configMenu.NavigationItems.push({ Name: $localize`Policies`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_POLICIES}`, Items: null, IsLink: false, count: null });
        this.configMenu.NavigationItems.push({ Name: $localize`Rules`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RULES}`, Items: null, IsLink: false, count: null });
        this.configMenu.NavigationItems.push({ Name: $localize`Relationships`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RELATIONSHIPS}`, Items: null, IsLink: false, count: null });
        this.configMenu.NavigationItems.push({ Name: $localize`Predicates`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_PREDICATES}`, Items: null, IsLink: false, count: null });
        this.configMenu.NavigationItems.push({ Name: $localize`Workflows`, Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_WORKFLOW}`, IsLink: false, count: null });
        this.configMenu.NavigationItems.push({ Name: $localize`Workflow Actions`, Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ISSUE_TYPES}`, IsLink: false, count: null });
        this.configMenu.NavigationItems.push({ Name: $localize`Scoring Definitions`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SCORING}`, Items: null, IsLink: false, count: null });
        this.configMenu.NavigationItems.push({ Name: $localize`Surveys`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SURVEYS}`, Items: null, IsLink: false, count: null });
    }

    private buildAdminMenu() {
        this.adminMenu = new SiteMenu();
        this.adminMenu.MenuID = '-Admin';
        this.adminMenu.NavigationItems = [];

        let integrationMenu = new SiteMenuItem();
        integrationMenu.Name = $localize`Integration`;
        integrationMenu.Items = [];
        integrationMenu.Items.push({ Name: 'API', Url: '/swagger/ui/index', Items: null, IsLink: true, count: null });
        integrationMenu.Items.push({ Name: $localize`Bulk Loader`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_BULK_LOAD}`, Items: null, IsLink: false, count: null });
        if (this.getBooleanSetting(CompanySettingEnum.ShowCustomAPIAdmin)) {
            integrationMenu.Items.push({ Name: $localize`Custom API`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_CUSTOM_API}`, Items: null, IsLink: false, count: null });
        }
        this.adminMenu.NavigationItems.push(integrationMenu);

        let securityMenu = new SiteMenuItem();
        securityMenu.Name = $localize`Security`;
        securityMenu.Items = [];

        securityMenu.Items.push({ Name: $localize`Groups`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_GROUPS}`, Items: null, IsLink: false, count: null });

        if (this.getBooleanSetting(CompanySettingEnum.EnableOrganizations)) {
            securityMenu.Items.push({ Name: $localize`Organizations`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ORGANIZATIONS}`, Items: null, IsLink: false, count: null });
        }
        securityMenu.Items.push({ Name: $localize`Responsibilities`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RESPONSIBILITIES}`, Items: null, IsLink: false, count: null });
        securityMenu.Items.push({ Name: $localize`Users`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RESOURCES}`, Items: null, IsLink: false, count: null });

        this.adminMenu.NavigationItems.push(securityMenu);

        this.adminMenu.NavigationItems.push({ Name: $localize`Settings`, Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SETTINGS}`, IsLink: false, count: null });

        this.adminMenu.NavigationItems.push({ Name: $localize`Export Templates`, Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_EXPORT_TEMPLATES}`, IsLink: false, count: null });

        this.adminMenu.NavigationItems.push({ Name: $localize`Dashboards`, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_DASHBOARDS}`, Items: null, IsLink: false, count: null });

        if (this.featureFlagService.flags[FeatureFlags.BrandingThemeUiTemp]) {
            this.adminMenu.NavigationItems.push({ Name: $localize`Branding`, Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_BRANDING}`, IsLink: false, count: null });
        }
        else {
            this.adminMenu.NavigationItems.push({ Name: $localize`Branding`, Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_CUSTOMIZATIONS}`, IsLink: false, count: null });
        }
        this.adminMenu.NavigationItems.push({ Name: $localize`Tags`, Items: null, Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_TAGS}`, IsLink: false, count: null });

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
