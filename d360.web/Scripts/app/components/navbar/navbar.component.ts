///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit, OnDestroy } from '@angular/core';
import { ROUTER_DIRECTIVES, Router, NavigationEnd } from '@angular/router';

@Component({
    selector: 'd3s-navbar',
    directives: [ROUTER_DIRECTIVES], 
    template: `
    <ul class="side-nav fixed" style="overflow: auto; transform: translateX(0px);">
        <li class="logo"></li> 
        <li><a href="/"><i class="fa fa-pencil"></i> Legacy site</a></li>
        <template ngFor let-item [ngForOf]="navItems">
                <li>
                    <a *ngIf="item.route" [routerLink]="[item.route]" (click)="toggleSubMenu(item)"><i [class]="'fa fa-' + item.icon"></i> {{item.name}}</a>
                    <a *ngIf="!item.route" href="#!" (click)="toggleSubMenu(item)"><i [class]="'fa fa-' + item.icon"></i> {{item.name}}</a>
                </li>
                <ul *ngIf="item.subItems && item.subItems.length > 0 && item.expanded" class="sub">
                    <li *ngFor="let sub of item.subItems" [class.router-link-active]="currentRoute == sub.route">
                        <a *ngIf="sub.route" [routerLink]="[sub.route]"><i class="fa fa-minus" aria-hidden="true"></i> {{sub.name}}</a>
                        <a *ngIf="!sub.route" href="#!"><i class="fa fa-minus" aria-hidden="true"></i> {{sub.name}}</a>
                    </li>
                </ul>
        </template>
    </ul>
`
    
    //`
    //            <ul class="side-nav fixed" style="overflow: auto; transform: translateX(0px);">
    //              <li class="logo"></li>    
    //              <li><a href="/"><i class="fa fa-pencil"></i> Legacy site</a></li>
    //              <li><a href="#!"><i class="fa fa-book"></i> Glossary</a></li>
    //              <li><a href="#!"><i class="fa fa-sitemap"></i> Models</a></li>
    //              <li><a href="#!"><i class="fa fa-university"></i> Policies</a></li>
    //              <li><a href="#!"><i class="fa fa-database"></i> Fusion</a></li>
    //              <li><a href="#!"><i class="fa fa-dashboard"></i> Monitor</a></li>
    //              <li><a href="#!"><i class="fa fa-group"></i> Community</a></li>
    //              <li><a (click)="showAdminLinks()"><i class="fa fa-gears"></i> Administration</a></li>
    //              <ul class="sub" *ngIf="showAdminChildLinks==true">
    //                    <li [class.router-link-active]="currentRoute == '/a/admin/settings'"><a [routerLink]="['/a/admin/settings']"><i class="fa fa-minus" aria-hidden="true"></i> Settings</a></li>
    //                    <li><a [routerLink]="['/a/admin/domain']"><i class="fa fa-minus" aria-hidden="true"></i> Reference Types</a></li>
    //                    <li><a [routerLink]="['/a/admin/workflow']"><i class="fa fa-minus" aria-hidden="true"></i> Workflow</a></li>
    //                    <li><a [routerLink]="['/a/admin/groups']"><i class="fa fa-minus" aria-hidden="true"></i> Groups</a></li>
    //                    <li><a [routerLink]="['/a/admin/responsibilities']"><i class="fa fa-minus" aria-hidden="true"></i> Responsibilities</a></li>
    //                    <li><a [routerLink]="['/a/admin/artifacts']"><i class="fa fa-minus" aria-hidden="true"></i> Artifacts</a></li>
    //                    <li><a [routerLink]="['/a/admin/templates']"><i class="fa fa-minus" aria-hidden="true"></i> Templates</a></li>
    //               </ul>                        
    //            </ul>
    //          `    
})

export class NavBarComponent implements OnInit, OnDestroy {
    //private showAdminChildLinks: boolean = false;
    private sub: any;
    private currentRoute = "";
    private navItems: NavBarItem[];

    constructor(private router: Router) {
    }

    ngOnInit() {
        this.navItems = new Array<NavBarItem>();

        this.navItems.push({ icon: 'book', name: 'Glossary', route: null, subItems: null, expanded: false });
        this.navItems.push({ icon: 'sitemap', name: 'Models', route: null, subItems: null, expanded: false });
        this.navItems.push({ icon: 'university', name: 'Policies', route: null, subItems: null, expanded: false });
        this.navItems.push({ icon: 'database', name: 'Fusion', route: null, subItems: null, expanded: false });
        this.navItems.push({ icon: 'dashboard', name: 'Monitor', route: null, subItems: null, expanded: false });
        this.navItems.push({ icon: 'group', name: 'Community', route: null, subItems: null, expanded: false });

        var adminItems = new Array<NavBarSubItem>();
        this.navItems.push({ icon: 'gears', name: 'Administration', route: null, subItems: adminItems, expanded: false });
        adminItems.push({ name: 'Settings', route: '/a/admin/settings', subItems: null, expanded: false });
        adminItems.push({ name: 'Reference Types', route: '/a/admin/domain', subItems: null, expanded: false });
        adminItems.push({ name: 'Workflow', route: '/a/admin/workflow', subItems: null, expanded: false });
        adminItems.push({ name: 'Groups', route: '/a/admin/groups', subItems: null, expanded: false });
        adminItems.push({ name: 'Responsibilities', route: '/a/admin/responsibilities', subItems: null, expanded: false });
        adminItems.push({ name: 'Artifacts', route: '/a/admin/artifacts', subItems: null, expanded: false });
        adminItems.push({ name: 'Templates', route: '/a/admin/templates', subItems: null, expanded: false });
        adminItems.push({ name: 'Models', route: '/a/admin/taxonomies', subItems: null, expanded: false });
        this.expandRoute();

        this.sub = this.router.events.subscribe(e => {
            if (e instanceof NavigationEnd) {
                this.currentRoute = e.url;
                this.expandRoute();
            }
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    toggleSubMenu(item: NavBarItem | NavBarSubItem) {
        var i = this.findCurrentItem();
        //disallow toggle if current route is in a sub menu
        if (i) {
            if (i == item)
                return false;
            if (item.subItems && item.subItems.length > 0 && item.subItems.indexOf(i) > -1)
                return false;
        }
        item.expanded = !item.expanded;
        return false;
    }

    expandRoute() {
        this.navItems.forEach(i => {
            i.expanded = false;
            if (this.currentRoute == i.route) {
                i.expanded = true;
            }
            if (i.subItems && i.subItems.length) {
                i.subItems.forEach(j => {
                    if (this.currentRoute == j.route) {
                        i.expanded = true;
                        return;
                    } 
                });
            }
        });
    }

    findCurrentItem(): NavBarItem | NavBarSubItem {
        var ret = null;
        this.navItems.forEach(i => {
            if (this.currentRoute == i.route) {
                ret = i;
            }
            if (i.subItems && i.subItems.length) {
                i.subItems.forEach(j => {
                    if (this.currentRoute == j.route) {
                        ret = j;
                        return;
                    }
                });
            }
        });
        return ret;
    }

    //showAdminLinks() {
    //    this.showAdminChildLinks = !this.showAdminChildLinks;
    //}
}

class NavBarItem {
    icon: string;
    name: string;
    route: string;
    expanded = false;
    subItems: NavBarSubItem[];
}

class NavBarSubItem {
    name: string;
    route: string;
    //TODO: recursive?
    expanded = false;
    subItems: NavBarSubItem[];
}
