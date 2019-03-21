import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import * as _ from 'lodash';

import {SiteMenu, SiteMenuItem} from '../../../models/site-menu.model';

import {SiteUrlHelpers} from '../../../static/site-url-helpers';

import {MessagesService} from '../../../services/messages.service';
import {HeaderActionsService} from '../../../services/header-actions.service';
import {StateService} from '../../../services/state.service';
import {FavoritesService} from '../../../services/favorites.service';
import {AuthenticationService} from '../../../services/authentication.service';
import {SiteMenuService} from '../../../services/site-menu.service';

import {BaseComponent} from '../base.component';

declare var CompanySettings;

@Component({
    selector: 'd3s-site-menu',
    templateUrl: './site-menu.component.html',
    providers: [SiteMenuService, FavoritesService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class SiteMenuComponent extends BaseComponent implements OnInit, OnDestroy {
    public isAdmin: boolean = false;
    public siteMenu: SiteMenu[] = [];
    public favorites: SiteMenu;

    private adminMenu: SiteMenu;
    private subSiteNav: any;
    private subFavorites: any;

    constructor(
        private ref: ChangeDetectorRef,
        private messagesService: MessagesService,
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

        this.subSiteNav = this.stateService.siteMenuRequiresReload$.subscribe(() => {
            this.loadMenu();
        });

        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(() => {
            this.loadFavorites();
        });
    }

    ngOnDestroy() {
        this.subSiteNav.unsubscribe();
        this.subFavorites.unsubscribe();
    }

    loadFavorites() {
        if (CompanySettings.ShowFavorites == 'false') {
            return;
        }

        this.favoritesService.getFavorites().subscribe(
            favorites => {
                favorites = _.sortBy(favorites, 'SortOrder'); // sort the favorites
                this.favorites = new SiteMenu();
                this.favorites.NavigationItems = [];

                for (let favorite of favorites) {
                    this.favorites.NavigationItems.push({
                        Name: favorite.Name,
                        Url: favorite.Route,
                        IsLink: false,
                        Items: null,
                        IsHomePage: favorite.IsHomePage
                    });
                }

                this.ref.markForCheck();
            });
    }

    loadMenu() {
        this.siteMenuService.getMenu().then(
            result => {
                result.MenuItems = result.MenuItems.filter(x => (x.MenuID != '#Admin')); //remove admin menu it will get built later.

                // add properties we need to add to the burned in menus
                for (let menu of result.MenuItems) {
                    menu.ShouldDisplay = true;

                    switch (menu.MenuID) {
                        case '#Glossary':
                            menu.ngUrl = SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT;
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
                        default:
                            //is it a custom menu?
                            if (menu.MenuID.startsWith('~')) {
                                if (!menu.Title) menu.Title = menu.MenuID.replace('~', '');
                            }
                            break;
                    }

                    if (!menu.Icon) {
                        menu.Icon = 'fa-folder';
                    }
                }

                this.siteMenu = _.sortBy(result.MenuItems, 'SortOrder'); // sort the menu's by display order

                if (result.IsAdmin) {
                    this.buildAdminMenu();
                }

                // used to enable guard that allows access to administrative routes                                
                this.authenticationService.isAdmin = result.IsAdmin;
                this.isAdmin = result.IsAdmin;

                this.ref.markForCheck();
            });
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

    private buildAdminMenu() {
        this.adminMenu = new SiteMenu();
        this.adminMenu.NavigationItems = [];

        let metaMenu = new SiteMenuItem();
        let integrationMenu = new SiteMenuItem();
        let securityMenu = new SiteMenuItem();
        let metricsMenu = new SiteMenuItem();
        let workflowMenu = new SiteMenuItem();

        metaMenu.Name = "MetaModel";
        metaMenu.Items = [
            {
                Name: 'Artifacts',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ARTIFACTS}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            },
            {
                Name: 'Attributes',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ATTRIBUTES}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            },
            {
                Name: 'Lookups',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_LOOKUPS}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            },
            {
                Name: 'Models',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_MODELS}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            },
            {
                Name: 'Policies',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_POLICIES}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            },
            {
                Name: 'Predicates',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_PREDICATES}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            },
            {
                Name: 'Relationships',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RELATIONSHIPS}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            },
            {
                Name: 'Rules',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RULES}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            },
            {
                Name: 'Surveys',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SURVEYS}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            }
        ];

        integrationMenu.Name = "Integration";
        integrationMenu.Items = [
            {
                Name: 'API',
                Url: '/swagger/ui/index',
                Items: null,
                IsLink: true,
                IsHomePage: false
            },
            {
                Name: 'Bulk Loader',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_BULK_LOAD}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            }
        ];
        if (CompanySettings.ShowCustomAPIAdmin != 'false') {
            integrationMenu.Items.push({
                Name: 'Custom API',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_CUSTOM_API}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            });
        }
        integrationMenu.Items.push(
            {
                Name: 'Fusion',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_FUSION}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            }
        );

        securityMenu.Name = "Security";
        securityMenu.Items = [
            {
                Name: 'Groups',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_GROUPS}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            }
        ];
        if (CompanySettings.EnableOrganizations) {
            securityMenu.Items.push({
                Name: 'Organizations',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ORGANIZATIONS}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            });
        }
        securityMenu.Items.push(
            {
                Name: 'Responsibilities',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RESPONSIBILITIES}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            },
            {
                Name: 'Users',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_RESOURCES}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            }
        );

        metricsMenu.Name = "Metrics";
        metricsMenu.Items = [
            {
                Name: 'Analytics',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ANALYTICS}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            },
            {
                Name: 'Dashboard',
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_DASHBOARDS}`,
                Items: null,
                IsLink: false,
                IsHomePage: false
            }
        ];

        workflowMenu.Name = "Workflow";
        workflowMenu.Items = [
            {
                Name: 'Workflow',
                Items: null,
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_WORKFLOW}`,
                IsLink: false,
                IsHomePage: false
            },
            {
                Name: 'Action Types',
                Items: null,
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_ISSUE_TYPES}`,
                IsLink: false,
                IsHomePage: false
            }
        ];

        this.adminMenu.NavigationItems.push(
            metaMenu,
            integrationMenu,
            securityMenu,
            {
                Name: 'Settings',
                Items: null,
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SETTINGS}`,
                IsLink: false,
                IsHomePage: false
            },
            {
                Name: 'Export Templates',
                Items: null,
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_EXPORT_TEMPLATES}`,
                IsLink: false,
                IsHomePage: false
            },
            metricsMenu,
            workflowMenu,
            {
                Name: 'Style Customizations',
                Items: null,
                Url: `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_CUSTOMIZATIONS}`,
                IsLink: false,
                IsHomePage: false
            }
        );
    }
}
