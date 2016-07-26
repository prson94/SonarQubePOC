///<reference path="./es6-shim.d.ts"/>
import { Component, AfterViewInit, ViewChild, ViewChildren, OnInit } from '@angular/core';
import { ROUTER_DIRECTIVES } from '@angular/router';
import { HomeComponent, AdminComponent, HeaderComponent, NavBarComponent, MessagesComponent } from './components/index';
import { MessagesService, HeaderBreadcrumbService, HeaderActionsService, PageHeader, RightSidebarService  } from './services/index';
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
                    <div class="row PageHeader" #pageheader>
                        <div class="col l7 m7 s12">              
                            <div class="PageDescription maincontent">{{pageHeader.description}}</div>              
                        </div>                                    
                    </div>
                    <div class="row">
                        <div class="col s12">
                            <div class="maincontent">                                            
                                <router-outlet></router-outlet>                                                
                            </div>  
                        </div>                                                
                    </div>                    
                    <d3s-right-sidebar [(visible)]="showRightSideBar" [titleHeight]="pageheader?.nativeElement?.clientHeight"></d3s-right-sidebar>                        
                </main>
                <d3s-messages></d3s-messages>
              `,
    directives: [ROUTER_DIRECTIVES, HeaderComponent, NavBarComponent, MessagesComponent, RightSidebarComponent],
    providers: [HeaderActionsService, HeaderBreadcrumbService, MessagesService, PageHeader, RightSidebarService]
})

export class AppComponent implements AfterViewInit, OnInit {
    showRightSideBar: boolean = false;
    @ViewChild(RightSidebarComponent) private rightSidebarComponent: RightSidebarComponent;

    @ViewChild('pageheader') pageheader;
    
    constructor(private pageHeader: PageHeader) {
        
    }

    toggleRightSidebar() {
        this.showRightSideBar = !this.showRightSideBar;
    }

    ngOnInit() {
        //this.pageheaderList.changes.subscribe(changes => console.log(changes));
    }
    
    ngAfterViewInit() {
        this.initializeQtipTooltips();  // initialize qtips library for tooltips we use in the site it needs to be a global js function                     
      //  console.log(this.pageheader);
      //  this.rightSidebarComponent.setTop(this.pageheader.nativeElement.clientHeight);
    }

    private initializeQtipTooltips() {
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
                style: {
                    classes: 'qtip-youtube qtip-rounded'
                }                
            });
        });
    }
    
}

