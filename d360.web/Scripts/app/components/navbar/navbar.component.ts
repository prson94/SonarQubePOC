///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { ROUTER_DIRECTIVES, Router, NavigationEnd } from '@angular/router';
import {  NavBarItem, NavBarItemComponent } from '../navbar/navbar-item.component';
import { SiteMenuService } from '../../services/index';
import { SiteMenu, SiteMenuItem } from '../../models/site-menu.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-navbar', 
    directives: [ROUTER_DIRECTIVES, NavBarItemComponent], 
    providers: [SiteMenuService],
    template: `
    <ul class="side-nav fixed" style="overflow: auto; transform: translateX(0px);">
        <li class="logo"></li> 
        <li *ngFor="let item of items">
            <d3s-navbar-item [item]="item" (onExpanded)="collapseOtherTopLevelMenus($event)" class="top"></d3s-navbar-item>
        </li>
    </ul>
 
` 
})

export class NavBarComponent implements OnInit, OnDestroy { 
    private sub: any;
    private currentRoute = "";
    private navItems: NavBarItem[];
    private siteMenu : SiteMenu[] = [];

    @Input() items: NavBarItem[] = new Array<NavBarItem>();

    constructor(private router: Router, private siteMenuService: SiteMenuService) {
    }

    ngOnInit() {
        this.loadMenu();

        this.sub = this.router.events.subscribe(e => {
            if (e instanceof NavigationEnd) {
                this.currentRoute = _.trimStart(e.url, '/');
                let item = this.activateRoute(this.currentRoute);
                if(item) this.expandRoute(item);                
            }
        });
    }

    collapseOtherTopLevelMenus(event) {
        for (let item of this.items) {
            if (item.name != event.item.name)
                item.expanded = false;
        }
    }

    loadMenu() {
        this.siteMenuService.getMenu()
            .then(result => {
                this.items = new Array<NavBarItem>();
                
                this.siteMenu = result;

                console.log(result);
                                
                this.loadGlossaryMenu(this.siteMenu.find(i => i.MenuID == '#Glossary'));
                this.loadModelMenu(this.siteMenu.find(i => i.MenuID == '#Models'));
                this.loadPoliciesMenu(this.siteMenu.find(i => i.MenuID == '#Policy'));                
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

        let admin = this.addNavItem('Administration', 'book', null);

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
        for (var i = 0; i < itms.length; i++) {
            var item = itms[i];
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
