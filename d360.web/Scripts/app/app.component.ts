///<reference path="./es6-shim.d.ts"/>
import { Component, AfterViewInit } from '@angular/core';
import { ROUTER_DIRECTIVES } from '@angular/router';
import { HomeComponent, AdminComponent, HeaderComponent, NavBarComponent, MessagesComponent } from './components/index';
import { MessagesService, HeaderBreadcrumbService, HeaderActionsService, PageHeader, RightSidebarService  } from './services/index';
import { PageLinksComponent } from './components/shared/page-links.component';
import { RightSidebarComponent } from './components/rightsidebar/right-sidebar.component';
declare var $: JQueryStatic;
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
                        <d3s-right-sidebar [(visible)]="showRightSideBar"></d3s-right-sidebar>                        
                    </div>                    
                </main>
                <d3s-messages></d3s-messages>
              `,
    directives: [ROUTER_DIRECTIVES, HeaderComponent, NavBarComponent, MessagesComponent, PageLinksComponent, RightSidebarComponent],
    providers: [HeaderActionsService, HeaderBreadcrumbService, MessagesService, PageHeader, RightSidebarService]
})

export class AppComponent implements AfterViewInit {
    showRightSideBar: boolean = false;
    constructor(private pageHeader: PageHeader) { }

    toggleRightSidebar() {
        this.showRightSideBar = !this.showRightSideBar;
    }

    ngAfterViewInit() {
        $('body').on('mouseenter', '*[data-type]', function (event) {
            $(this).qtip({
                content: {
                    title: $(this).data('title'),
                    // Set the text to an image HTML string with the correct src URL to the loading image you want to use
                    text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                    ajax: {
                        url: "/resources/" + $(this).data("type") + "/" + $(this).data("id") + "/templates/tooltip/" + $(this).data("context"),
                        once: ($(this).attr('data-cache') ? $(this).data('cache') : true),  // do we want to fetch the tooltip just once or recall each time?                            
                        success: function (data) {
                            if (!data || !data.length) {
                                this.destroy();
                            }
                            else {
                                this.set('content.text', data);
                            }
                        }
                    }
                },
                position: {
                    at: 'bottom center', // Position the tooltip above the link
                    my: 'top center',
                    viewport: $(window), // Keep the tooltip on-screen at all times
                    effect: false // Disable positioning animation
                },
                overwrite: false,
                show: {
                    event: event.type,  // show using same event as above.
                    solo: false,         // Only show one tooltip at a time
                    ready: true
                },
                hide: {
                    fixed: true,
                    delay: 250,
                },
                //hide: 'mouseout',
                style: {
                    classes: 'qtip-youtube qtip-rounded'
                }
                //addTooltip(this);
            });
        });
    }
    
}

