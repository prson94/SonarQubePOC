///<reference path="./es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { RouteConfig, ROUTER_DIRECTIVES, AsyncRoute } from '@angular/router-deprecated';
import { HomeComponent, AdminSettingsComponent, AdminDomainComponent, AdminGroupsComponent } from './components/index'
import { PageHeader } from './page-header.service';
import 'rxjs/Rx';

@Component({
    selector: 'd3s-app',
    templateUrl: 'scripts/app/templates/app.component.html',
    directives: [ROUTER_DIRECTIVES],
    providers: [PageHeader]
})

@RouteConfig([
        { path: '/a', name: 'Home', component: HomeComponent, useAsDefault: true },
        { path: '/a/settings', name: 'Settings', component: AdminSettingsComponent },
        { path: '/a/admin/domain', name: 'Domain', component: AdminDomainComponent },
        { path: '/a/admin/groups', name: 'Groups', component: AdminGroupsComponent },
])
export class AppComponent {
    pageHeader: PageHeader;

    constructor(pageHeader: PageHeader) {
        console.clear();
        this.pageHeader = pageHeader;
    }
}

