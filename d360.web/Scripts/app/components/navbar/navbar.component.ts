///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { transition, style, animate, trigger, state } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { NavBarMode, NavBarItem } from '../../models/nav-bar.model';
import { SiteMenu, SiteMenuItem } from '../../models/site-menu.model';
import { SiteMenuService } from '../../services/index';
import { HeaderActionsService } from '../../services/header-actions.service';
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

        `
        ],
    providers: [SiteMenuService, FavoritesService],
    template: `
    <ul class="side-nav fixed menu-flex" style="overflow: auto; transform: translateX(0px);">
        <li class="logo" [routerLink]="'a/home'"></li> 

        <template [ngIf]="mode == NavBarMode.Default">
            <li *ngFor="let item of items">
                <d3s-navbar-item [item]="item" (onExpanded)="collapseOtherTopLevelMenus($event)" class="top"></d3s-navbar-item>
            </li>
        </template>
        <template [ngIf]="mode == NavBarMode.Favorites && favItems.length > 0">
            <li *ngFor="let fav of favItems" class="navbar-favorite" [class.active]="fav.active">
                <a [routerLink]="[fav.route]">{{fav.name}}</a>
            </li>
        </template>
        <template [ngIf]="mode == NavBarMode.Favorites && favItems.length == 0">
            <li class="navbar-message">You don't have any favorites. Click the <i class="fa fa-star"></i> on the header of any page to add it to your favorites.</li>        
        </template>
        <template [ngIf]="mode == NavBarMode.Edit">
            <li>edit</li>
        </template>

        <li style="margin-top: auto;">
            <d3s-navbar-menu [(mode)]="mode"></d3s-navbar-menu>
        </li>
    </ul>

`
})

export class NavBarComponent implements OnInit, OnDestroy { 
    private sub: any;
    private subFavorites: any;
    private currentRoute = "";
    private navItems: NavBarItem[];
    private siteMenu: SiteMenu[] = [];
    private favItems: NavBarItem[] = new Array<NavBarItem>();
    private mode = NavBarMode.Default;
    NavBarMode = NavBarMode;

    @Input() items: NavBarItem[] = new Array<NavBarItem>();

    constructor(private router: Router, private siteMenuService: SiteMenuService, private headerActionsService: HeaderActionsService, private favoritesService: FavoritesService) {
    }

    ngOnInit() {
        this.loadMenu();
        this.loadFavorites();

        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(s => {
            this.loadFavorites(s);
        });

        this.sub = this.router.events.subscribe(e => {
            if (e instanceof NavigationEnd) {
                this.currentRoute = _.trimStart(e.url, '/');

                let item = this.activateRoute(this.currentRoute);
                if (item) this.expandRoute(item); 

                this.activateRoute(this.currentRoute, this.favItems);
            }
        });
    }

    collapseOtherTopLevelMenus(event) {
        for (let item of this.items) {
            if (item.name != event.item.name)
                item.expanded = false;
        }
    }

    loadFavorites(favorites: Favorite[] = null) {
        if (favorites) {
            this.favItems = [];
            for (let f of favorites) {
                let i = new NavBarItem();
                i.name = f.Name;
                i.route = f.Route;
                this.favItems.push(i);
            }
            this.activateRoute(this.currentRoute, this.favItems);
        }
        else {
            this.favoritesService.getFavorites().then(fav => {
                this.favItems = [];
                for (let f of fav) {
                    let i = new NavBarItem();
                    i.name = f.Name;
                    i.route = f.Route;
                    this.favItems.push(i);
                }
                this.activateRoute(this.currentRoute, this.favItems);
            });
        }

    }


    loadMenu() {
        this.siteMenuService.getMenu()
            .then(result => {
                this.items = new Array<NavBarItem>();
                
                this.siteMenu = result;
                
                this.loadGlossaryMenu(this.siteMenu.find(i => i.MenuID == '#Glossary'));
                this.loadModelMenu(this.siteMenu.find(i => i.MenuID == '#Models'));
                this.loadPoliciesMenu(this.siteMenu.find(i => i.MenuID == '#Policy'));     
                this.loadReferenceMenu(this.siteMenu.find(i => i.MenuID == '#Domains'));           
                this.loadFusionMenu(this.siteMenu.find(i => i.MenuID == '#Fusion'));
                this.loadMonitorMenu();                
                this.loadCommunityMenu(this.siteMenu.find(i => i.MenuID == '#Community'));                                   
                this.loadAdminMenu(this.siteMenu.find(i => i.MenuID == '#Admin'));
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

        let admin = this.addNavItem('Administration', 'cogs', null);

        // these are ordered by alpha a-Z...

        let integrationModel = this.addSubItem(admin, 'Integration', null, null);
        this.addSubItem(integrationModel, 'API', null, null, '/swagger/ui/index');
        this.addSubItem(integrationModel, 'Bulk Loader', null, 'a/admin/load');
        this.addSubItem(integrationModel, 'Fusion', null, 'a/admin/fusion');

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

        let metricsModel = this.addSubItem(admin, 'Metrics', null, null);
        this.addSubItem(metricsModel, 'Analytics', null, 'a/admin/analytics');
        this.addSubItem(metricsModel, 'Dashboards', null, 'a/admin/dashboards');

        this.addSubItem(admin, 'Reference', null, 'a/admin/domain');

        //security sub menu
        let security = this.addSubItem(admin, 'Security', null, null);
        this.addSubItem(security, 'Groups', null, 'a/admin/groups');
        this.addSubItem(security, 'Responsibilities', null, 'a/admin/responsibilities');
        this.addSubItem(security, 'Users', null, 'a/admin/resources');

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
    }

    addNavItem(name: string, icon: string, route: string): NavBarItem {
        route = _.trimStart(route, '/');
        let i = new NavBarItem();
        i.name = name;
        i.icon = icon;
        i.route = route;        
        this.items.push(i);
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
}

