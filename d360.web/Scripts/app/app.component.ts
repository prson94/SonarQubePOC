///<reference path="./es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { ROUTER_DIRECTIVES } from '@angular/router';
import { HomeComponent, AdminComponent, HeaderComponent, NavBarComponent, MessagesComponent } from './components/index';
import { MessagesService, HeaderBreadcrumbService, HeaderActionsService  } from './services/index';

import 'rxjs/Rx';

@Component({
    selector: 'd3s-app',
    template: ` <header>
                    <d3s-header></d3s-header>
                    <d3s-navbar></d3s-navbar>
                </header>
                <main>
                    <div class="maincontent">                                            
                       <router-outlet></router-outlet>                                                
                    </div>
                </main>
                <d3s-messages></d3s-messages>
              `,
    directives: [ROUTER_DIRECTIVES, HeaderComponent, NavBarComponent, MessagesComponent],
    providers: [HeaderActionsService, HeaderBreadcrumbService, MessagesService]
})

export class AppComponent { }

