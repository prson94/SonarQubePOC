///<reference path="./es6-shim.d.ts"/>
//
import { Component } from 'angular2/core';
import { RouteConfig, ROUTER_DIRECTIVES, AsyncRoute } from 'angular2/router';
import { HomeComponent } from './components/home.component';
import { AdminSettingsComponent } from './components/admin-settings.component';
import { PageHeader } from './page-header.service';
import 'rxjs/Rx';

@Component({
    selector: 'd3s-app',
    templateUrl: 'scripts/app/app.component.html',
    directives: [ROUTER_DIRECTIVES],
    providers: [PageHeader]
})

@RouteConfig([
        { path: '/a', name: 'Home', component: HomeComponent, useAsDefault: true },
        { path: '/a/settings', name: 'Settings', component: AdminSettingsComponent },
])
export class AppComponent {
    pageHeader: PageHeader;

    constructor(pageHeader: PageHeader) {
        this.pageHeader = pageHeader;
    }
}

