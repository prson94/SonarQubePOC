///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { ROUTER_DIRECTIVES, Router, NavigationEnd } from '@angular/router';
import {  NavBarItem, NavBarItemComponent } from '../navbar/navbar-item.component';

@Component({
    selector: 'd3s-navbar', 
    directives: [ROUTER_DIRECTIVES, NavBarItemComponent], 
    template: `
    <ul class="side-nav fixed" style="overflow: auto; transform: translateX(0px);">
        <li class="logo"></li> 
        <!-- TODO: hardcoded, remove later -->
        <li><a href="/"><i class="fa fa-pencil"></i> Legacy site</a></li>

        <li *ngFor="let item of items">
            <d3s-navbar-item [item]="item" class="top"></d3s-navbar-item>
        </li>
    </ul>
 
` 
})

export class NavBarComponent implements OnInit, OnDestroy { 
    private sub: any;
    private currentRoute = "";
    private navItems: NavBarItem[];

    @Input() items: NavBarItem[] = new Array<NavBarItem>();

    constructor(private router: Router) {
    }

    ngOnInit() {
        this.items = new Array<NavBarItem>();

        this.addNavItem('Glossary', 'book', null);
        this.addNavItem('Models', 'sitemap', null);
        this.addNavItem('Policies', 'university', null);
        this.addNavItem('Fusion', 'database', null);
        this.addNavItem('Monitor', 'dashboard', null);
        this.addNavItem('Community', 'group', null);

        let admin = this.addNavItem('Administration', 'book', null);
        this.addSubItem(admin, 'Settings', null, 'a/admin/settings');
        this.addSubItem(admin, 'Reference Types', null, 'a/admin/domain');
        this.addSubItem(admin, 'Workflow', null, 'a/admin/workflow');
        this.addSubItem(admin, 'Templates', null, 'a/admin/templates');

        let metaModel = this.addSubItem(admin, 'MetaModel', ' ', null);
        this.addSubItem(metaModel, 'Artifacts', null, 'a/admin/artifacts');
        this.addSubItem(metaModel, 'Models', null, 'a/admin/taxonomies');
        this.addSubItem(metaModel, 'Lookups', null, 'a/admin/lookups');

        let security = this.addSubItem(admin, 'Security', ' ', null);
        this.addSubItem(security, 'Groups', null, 'a/admin/groups');
        this.addSubItem(security, 'Responsibilities', null, 'a/admin/responsibilities');

        this.sub = this.router.events.subscribe(e => {
            if (e instanceof NavigationEnd) {
                this.currentRoute = e.url; 
                let i = this.activateRoute(this.currentRoute);
                //console.log(i);
            }
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    addNavItem(name: string, icon: string, route: string): NavBarItem {
        let i = new NavBarItem();
        i.name = name;
        i.icon = icon;
        i.route = route;
        this.items.push(i);
        return i;
    }

    addSubItem(item: NavBarItem, name: string, icon: string, route: string): NavBarItem {
        let i = new NavBarItem();
        if (!item.subItems) {
            item.subItems = new Array<NavBarItem>();
        }
        i.name = name;
        i.icon = icon;
        i.route = route;
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
