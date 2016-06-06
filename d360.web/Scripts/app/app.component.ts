///<reference path="./es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { RouteConfig, ROUTER_DIRECTIVES, AsyncRoute } from '@angular/router-deprecated';
import { HomeComponent, AdminComponent } from './components/index'
import 'rxjs/Rx';

@Component({
    selector: 'd3s-app',
    template: `<router-outlet></router-outlet>`,
    directives: [ROUTER_DIRECTIVES]
})

@RouteConfig([
    { path: '/a/admin/...', name: 'Admin', component: AdminComponent },
    { path: '/a', name: 'Home', component: HomeComponent, useAsDefault: true },
])
export class AppComponent { }

