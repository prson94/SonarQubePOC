///<reference path="./es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { ROUTER_DIRECTIVES } from '@angular/router';
import { HomeComponent, AdminComponent, HeaderComponent, NavBarComponent, MessagesComponent } from './components/index';
import { MessagesService, HeaderBreadcrumbService, HeaderActionsService, PageHeader  } from './services/index';
import { PageLinksComponent } from './components/shared/page-links.component';

import 'rxjs/Rx';

@Component({
    selector: 'd3s-app',
    template: ` <header>
                    <d3s-header></d3s-header>
                    <d3s-navbar></d3s-navbar>
                </header>
                <main>
                    <div class="row PageHeader">
                        <div class="col l7 m7 s12">              
                            <div class="PageDescription maincontent">{{pageHeader.description}}</div>              
                        </div>            
                        <d3s-page-links (onSideBarActivated)="toggleRightSidebar()"></d3s-page-links>
                    </div>
                    <div class="row">
                        <div class="col" [ngClass]="{'s12 m10 l11':showRightSideBar}" [ngClass]="{'s12':!showRightSideBar}">
                            <div class="maincontent">                                            
                                <router-outlet></router-outlet>                                                
                            </div>  
                        </div>
                        <div *ngIf="showRightSideBar" class="col hide-on-small-only m2 l1">
                            <d3s-right-sidebar></d3s-right-sidebar>
                        </div>
                    </div>                    
                </main>
                <d3s-messages></d3s-messages>
              `,
    directives: [ROUTER_DIRECTIVES, HeaderComponent, NavBarComponent, MessagesComponent, PageLinksComponent],
    providers: [HeaderActionsService, HeaderBreadcrumbService, MessagesService, PageHeader]
})

export class AppComponent {
    showRightSideBar: boolean = false;
    constructor(private pageHeader: PageHeader) { }

    toggleRightSidebar() {
        this.showRightSideBar = !this.showRightSideBar;
    }
}

