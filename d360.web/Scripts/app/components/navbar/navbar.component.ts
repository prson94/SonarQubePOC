///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { transition, style, animate, trigger, state } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { NavBarMode, NavBarItem } from '../../models/nav-bar.model';
import { SiteMenu, SiteMenuItem } from '../../models/site-menu.model';
import { SiteMenuService } from '../../services/index';
import { HeaderActionsService } from '../../services/header-actions.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { FavoritesService } from '../../services/favorites.service';
import { Favorite } from '../../models/favorite.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-navbar', 
    styles: [
        `
            .menu-flex {
                display: flex;
                flex-direction: column;
                min-height: 100vh;    
            }
        
            .navbar-menu {
                position: fixed;
                width:100%;
                bottom: -50px;
            }

            .navbar-favorite {
                padding: 7px 0 7px 0;
                font-size: 1em;
                color:white;
                cursor: pointer;
                
            }
            .navbar-favorite.active, .navbar-favorite:hover {
                color:black;
            }

            .navbar-message {
                font-size: 1em;
                color: white;
                margin: 12px;
                text-align: center;
            }
            
            .navbar-favorite:hover, .navbar-favorite.active {
                background-color: #F8F3EF;
            }

            .navbar-favorite:hover a, .navbar-favorite.active a {
                color: #444;
            }

            .navbar-favorite-toolbar {
                display: inline-block;
                cursor: pointer;
                width: 100%;
                height: 32px;                
                background-color: #1E1A15;
            }

            .navbar-favorite-toolbar-item {
                color: white;
                font-size: 1.2em;
                display: inline-block;
                width: 32px;
                height: 32px;
                padding: 7px;
                margin-left: -4px;
                text-align: center;
                transition: all 200ms ease-in-out;
            }

            .navbar-favorite-toolbar-item:hover {
                background-color: #82705C;
                opacity:1;
            }

            .navbar-button {
                cursor: pointer;
                background-color: #383127;
                color: white;
                padding:5px;   
                transition: all 200ms ease-in-out; 
            }

            .navbar-button:hover {
                background-color: #F8F3EF;
                color: black;
            }
        `
    ],
    providers: [SiteMenuService, FavoritesService],
    templateUrl: 'scripts/app/components/navbar/navbar.component.html',
})

export class NavBarComponent implements OnInit, OnDestroy { 
    private sub: any;
    private subFavorites: any;
    private subBread: any;
    private currentRoute = "";
    private currentPage = "";
    private navItems: NavBarItem[];
    private siteMenu: SiteMenu[] = [];
    private favItems: NavBarItem[] = new Array<NavBarItem>();
    private adminFavorites: Favorite[] = new Array<Favorite>();
    private mode = NavBarMode.Default;
    private firstLoad = true;
    private favIndex = 0;
    private isEditingFavorites = false;
    private isEditingAdmin = false;
    private isAdmin = false;
    private isLoading = false;


    NavBarMode = NavBarMode;

    @Input() items: NavBarItem[] = new Array<NavBarItem>();
    adminItems: NavBarItem[] = new Array<NavBarItem>();

    constructor(private router: Router, private siteMenuService: SiteMenuService, private headerActionsService: HeaderActionsService, private favoritesService: FavoritesService, private headerBreadcrumbService: HeaderBreadcrumbService) {
    }

    ngOnInit() {
        this.loadMenu();
        this.loadFavorites();

        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(s => {
            this.loadFavorites(s, false);
        });

        this.sub = this.router.events.subscribe(e => {
            if (e instanceof NavigationEnd) {
                this.currentRoute = _.trimStart(e.url, '/');

                let item = this.activateRoute(this.currentRoute);
                if (item) this.expandRoute(item); 

                this.activateRoute(this.currentRoute, this.favItems);
            }
        });

        this.subBread = this.headerBreadcrumbService.breadcrumbs$.subscribe(b => {
            this.currentPage = b.text;
        });

    }

    collapseOtherTopLevelMenus(event) {
        for (let item of this.items) {
            if (item.name != event.item.name)
                item.expanded = false;
        }
    }

    loadFavorites(favorites: Favorite[] = null, emit = false): Promise<any> {
        if (favorites) {
            this.favItems = [];
            for (let f of favorites) {
                let i = new NavBarItem();
                i.name = f.Name;
                i.route = f.Route;
                if (f.ResourceID == 0)
                    i.icon = "fa-globe";
                this.favItems.push(i);
            }
            if (emit)
                this.headerActionsService.emitFavoritesChange(favorites);
            this.activateRoute(this.currentRoute, this.favItems);
            return null;
        }
        else {
            this.isLoading = true;
            return this.favoritesService.getFavorites().then(fav => {
                this.favItems = [];
                for (let f of fav) {
                    let i = new NavBarItem();
                    i.name = f.Name;
                    i.route = f.Route;
                    if (f.ResourceID == 0)
                        i.icon = "fa-globe";
                    this.favItems.push(i);
                }
                if (this.favItems.length > 0 && this.firstLoad) {
                    this.mode = NavBarMode.Favorites;
                }

                if (this.favItems.length == 0 && this.mode == NavBarMode.EditFavorites) {
                    this.mode = NavBarMode.Favorites;
                }

                if (emit)
                    this.headerActionsService.emitFavoritesChange(fav);
                this.activateRoute(this.currentRoute, this.favItems);
                this.firstLoad = false;
            }).then(() => this.favoritesService.getFavorites(true))
                .then(f => {
                    this.adminFavorites = f;
                    this.isLoading = false;
                });
        }
    }


    loadMenu() {
        this.siteMenuService.getMenu()
            .then(result => {
                this.items = new Array<NavBarItem>();
                this.adminItems = new Array<NavBarItem>();

                this.siteMenu = result.MenuItems;
                
                this.isAdmin = result.IsAdmin;
                
                this.loadGlossaryMenu(this.siteMenu.find(i => i.MenuID == '#Glossary'));
                this.loadModelMenu(this.siteMenu.find(i => i.MenuID == '#Models'));
                this.loadPoliciesMenu(this.siteMenu.find(i => i.MenuID == '#Policy'));     
                this.loadReferenceMenu(this.siteMenu.find(i => i.MenuID == '#Domains'));           
                this.loadFusionMenu(this.siteMenu.find(i => i.MenuID == '#Fusion'));
                this.loadMonitorMenu();                
                this.loadCommunityMenu(this.siteMenu.find(i => i.MenuID == '#Community'));                                   
                this.loadAdminMenu(this.siteMenu.find(i => i.MenuID == '#Admin'));
                this.loadCustomMenu(this.siteMenu.filter(i => i.MenuID.startsWith('~')));
            });
    }

    loadCustomMenu(customMenu: SiteMenu[]) {
        customMenu.forEach(c => {
            let m = this.addNavItem(c.MenuID.substr(1), 'folder', null);
            this.renderChildItems(m, c.NavigationItems);
        });
    }

    loadFusionMenu(fusionMenu: SiteMenu) {
        if (fusionMenu == null || !fusionMenu.ShouldDisplay) return;

        let fusion = this.addNavItem('Fusion', 'database', 'a/fusion');
    }

    loadReferenceMenu(referenceMenu: SiteMenu) {
        if (referenceMenu == null ) return;

        let fusion = this.addNavItem('Reference', 'cubes', 'a/reference');
    }

    loadGlossaryMenu(glossaryMenu: SiteMenu) {
        if (glossaryMenu == null ) return;

        let glossary = this.addNavItem('Glossary', 'book', null);
        
        this.renderChildItems(glossary, glossaryMenu.NavigationItems);
    }

    loadCommunityMenu(communityMenu: SiteMenu) {
        if (communityMenu == null || !communityMenu.ShouldDisplay) return;

        let community = this.addNavItem('Community', 'group', 'a/community');
    }

    loadMonitorMenu() {        
        let monitor = this.addNavItem('Monitor', 'dashboard', 'a/monitor');
    }

    loadPoliciesMenu(policiesMenus: SiteMenu) {
        if (policiesMenus == null) return;

        let policies = this.addNavItem('Policies', 'university', null);

        this.renderChildItems(policies, policiesMenus.NavigationItems);
    }

    loadModelMenu(modelMenus: SiteMenu) {
        if (modelMenus == null ) return;

        let models = this.addNavItem('Models', 'sitemap', null);

        this.renderChildItems(models, modelMenus.NavigationItems);
    }

    loadAdminMenu(adminMenu: SiteMenu) {
        if (adminMenu == null) return;

        let admin = this.addNavItem('Administration', 'cogs', null, this.adminItems);
        admin.expanded = true;
        // these are ordered by alpha a-Z...

        let integrationModel = this.addSubItem(admin, 'Integration', null, null);
        this.addSubItem(integrationModel, 'API', null, null, '/swagger/ui/index');
        this.addSubItem(integrationModel, 'Bulk Loader', null, 'a/admin/load');
        this.addSubItem(integrationModel, 'Fusion', null, 'a/admin/fusion');
        integrationModel.expanded = true;

        // meta model sub
        let metaModel = this.addSubItem(admin, 'MetaModel', null, null);
        this.addSubItem(metaModel, 'Artifacts', null, 'a/admin/artifacts');
        this.addSubItem(metaModel, 'Attributes', null, 'a/admin/attributes');
        this.addSubItem(metaModel, 'Lookups', null, 'a/admin/lookups');
        this.addSubItem(metaModel, 'Models', null, 'a/admin/taxonomies');
        this.addSubItem(metaModel, 'Policies', null, 'a/admin/policies');
        this.addSubItem(metaModel, 'Relationships', null, 'a/admin/relationships');
        this.addSubItem(metaModel, 'Rules', null, 'a/admin/rules');
        this.addSubItem(metaModel, 'Surveys', null, 'a/admin/surveys');
        metaModel.expanded = true;

        let metricsModel = this.addSubItem(admin, 'Metrics', null, null);
        this.addSubItem(metricsModel, 'Analytics', null, 'a/admin/analytics');
        this.addSubItem(metricsModel, 'Dashboards', null, 'a/admin/dashboards');
        metricsModel.expanded = true;

        this.addSubItem(admin, 'Reference', null, 'a/admin/domain');

        //security sub menu
        let security = this.addSubItem(admin, 'Security', null, null);
        this.addSubItem(security, 'Groups', null, 'a/admin/groups');
        this.addSubItem(security, 'Responsibilities', null, 'a/admin/responsibilities');
        this.addSubItem(security, 'Users', null, 'a/admin/resources');
        security.expanded = true;

        this.addSubItem(admin, 'Settings', null, 'a/admin/settings');
        this.addSubItem(admin, 'Templates', null, 'a/admin/templates');
        this.addSubItem(admin, 'Workflow', null, 'a/admin/workflow');        
    }
       
    private renderChildItems(navBar: NavBarItem, siteMenuItems: SiteMenuItem[]) {
        //add each to the navbar
        if (siteMenuItems == null || siteMenuItems.length == 0) return;

        for (let item of siteMenuItems) {
            if (item.Items) {
                var parent = this.addSubItem(navBar, item.Name, null, item.Url ? item.Url : null); //menu doesnt yet support link / expand collapse combo

                this.renderChildItems(parent, item.Items);
            }
            else {
                this.addSubItem(navBar, item.Name, null, item.Url);
            }
        }
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.subFavorites.unsubscribe();
        this.subBread.unsubscribe();
    }

    addNavItem(name: string, icon: string, route: string, menu: NavBarItem[] = null): NavBarItem {
        if (menu == null)
            menu = this.items;
        route = _.trimStart(route, '/');
        let i = new NavBarItem();
        i.name = name;
        i.icon = icon;
        i.route = route;        
        menu.push(i);
        return i;
    }

    addSubItem(item: NavBarItem, name: string, icon: string, route: string, url?: string): NavBarItem {
        route = _.trimStart(route, '/');
        let i = new NavBarItem();
        if (!item.subItems) {
            item.subItems = new Array<NavBarItem>();
        }
        i.name = name;
        i.icon = icon;
        i.route = route;
        i.parent = item;
        if (url != null) i.url = url;
        item.subItems.push(i);
        return i;
    }
  

    findNavItem(route: string, itms: NavBarItem[] = null): NavBarItem {
        if (!itms)
            itms = this.items;
        for (var i = 0; i < itms.length; i++) {
            let r = null;
            var item = itms[i];
            if (item.route == route) {
                return item;
            }
            if (item.subItems && item.subItems.length > 0)
                r = this.findNavItem(route, item.subItems);
            if (r) return r;
        }
        return null;
    }

    expandRoute(i: NavBarItem): void {
        i.expanded = true;
        if (i.parent)
            this.expandRoute(i.parent);
    }

    activateRoute(route: string, itms: NavBarItem[] = null): NavBarItem {        
        let r = null;
        if (!itms)
            itms = this.items;        
        for(let item of itms){            
            item.expanded = false;
            item.active = false;            
            if (item.route == route) r = item;
            if (item.subItems && item.subItems.length > 0) {
                let s = this.activateRoute(route, item.subItems);
                if (s) r = s;
            }
        }
        if (r) {
            r.active = true;
        }
        return r;
    }

    favAction(action: string) {
        let item = null;
        item = this.favItems[this.favIndex];

        switch (action) {
            case 'up':
                if (item == null || this.favIndex == 0)
                    return;
                this.isEditingFavorites = true;
                this.favoritesService.moveUp(item.route)
                    .then(() => {
                        this.favIndex--;
                        this.loadFavorites().then(() => {
                            this.isEditingFavorites = false;
                        });
                    });
                break;
            case 'down':
                if (item == null || this.favIndex == (this.favItems.length - 1))
                    return;
                this.isEditingFavorites = true;
                this.favoritesService.moveDown(item.route)
                    .then(() => {
                        this.favIndex++;
                        this.loadFavorites().then(() => {
                            this.isEditingFavorites = false;
                        });
                    });
                break;
            case 'remove':
                if (item == null)
                    return;
                this.isEditingFavorites = true;
                this.favoritesService.toggleFavorite(item.name, item.route)
                    .then(() => {
                        this.loadFavorites(null, true).then(() => {
                            this.isEditingFavorites = false;
                        });
                    });
                break;
            case 'edit':
                let activeIndex = this.favItems.findIndex(f => f.active);
                if (activeIndex >= 0)
                    this.favIndex = activeIndex;
                this.mode = NavBarMode.EditFavorites;
                break;
            case 'adminedit':
                this.mode = NavBarMode.EditAdminFavorites;
                break;
            case 'admintoggle':
                this.favoritesService.toggleFavorite(this.currentPage, this.currentRoute, true)
                    .then(() => {
                        this.loadFavorites(null, true);
                    });
                break;
            case 'adminup':
                item = this.adminFavorites[this.favIndex];
                if (item == null || this.favIndex == 0)
                    return;
                this.isEditingFavorites = true;
                this.favoritesService.moveUp(item.Route, true)
                    .then(() => {
                        this.favIndex--;
                        this.loadFavorites().then(() => {
                            this.isEditingFavorites = false;
                        });
                    });
                break;
            case 'admindown':
                item = this.adminFavorites[this.favIndex];
                if (item == null || this.favIndex == (this.adminFavorites.length - 1))
                    return;
                this.isEditingFavorites = true;
                this.favoritesService.moveDown(item.Route, true)
                    .then(() => {
                        this.favIndex++;
                        this.loadFavorites().then(() => {
                            this.isEditingFavorites = false;
                        });
                    });
                break;
            case 'adminremove':
                item = this.adminFavorites[this.favIndex];
                console.log(item);
                if (item == null)
                    return;
                this.isEditingFavorites = true;
                this.favoritesService.toggleFavorite(item.Name, item.Route, true)
                    .then(() => {
                        this.loadFavorites(null, true).then(() => {
                            this.isEditingFavorites = false;
                        });
                    });
                break;
            default:
                break;

        }

        //make sure favIndex is still valid after whatever
        _.clamp(this.favIndex, 0, (this.favItems.length - 1));

    }

    currentRouteInAdminFavorites() {
        return this.adminFavorites.findIndex(f => f.Route == this.currentRoute) >= 0;
    }
}

