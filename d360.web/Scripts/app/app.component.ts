///<reference path="./es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { RouteConfig, ROUTER_DIRECTIVES, AsyncRoute } from '@angular/router-deprecated';
import { HomeComponent, AdminComponent, HeaderComponent, NavBarComponent } from './components/index'

import 'rxjs/Rx';

@Component({
    selector: 'd3s-app',
    template: ` <header>
                    <d3s-header></d3s-header>
                    <d3s-navbar></d3s-navbar>
                </header>
                <main>
                    <div class="container">                                            
                       <router-outlet></router-outlet>                                                
                    </div>
                </main>
              `,
    directives: [ROUTER_DIRECTIVES, HeaderComponent, NavBarComponent]
})

@RouteConfig([
    { path: '/a/admin/...', name: 'Admin', component: AdminComponent },
    { path: '/a', name: 'Home', component: HomeComponent, useAsDefault: true },
])
export class AppComponent { }

