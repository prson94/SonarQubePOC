import { Component, AfterViewInit, ViewChild, ViewChildren, OnInit, ViewContainerRef, ComponentFactoryResolver, ComponentFactory, ComponentRef } from '@angular/core';
import { Router } from '@angular/router';
import { MessagesService, HeaderBreadcrumbService, HeaderActionsService, PageHeader, RightSidebarService, WebAnalyticsService, StateService  } from './services/index';
import { RightSidebarComponent } from './components/rightsidebar/right-sidebar.component';
import { DynamicTypeBuilder, IHaveDynamicData } from './services/dynamic-type-builder';
declare var $: JQueryStatic;
import 'rxjs/Rx';


@Component({
    selector: 'd3s-app',    
    template: ` <header>
                    <d3s-header></d3s-header>
                    <d3s-navbar></d3s-navbar>
                </header>
                <main>                                                                          
                    <div class="row">                         
                        <div class="col s12">            
                            <div class="maincontent">                                                                                                                                                                            
                                <router-outlet></router-outlet>                                                
                            </div>  
                        </div>                                                
                    </div>                    
                    <d3s-right-sidebar [titleHeight]="0"></d3s-right-sidebar>                        
                </main>
                <d3s-messages></d3s-messages>                
                <div #target></div>                
              `
})

export class AppComponent implements AfterViewInit, OnInit {    
    @ViewChild(RightSidebarComponent) private rightSidebarComponent: RightSidebarComponent;    
    @ViewChild('target', { read: ViewContainerRef }) protected dynamicComponentTarget: ViewContainerRef;
    protected componentRef: ComponentRef<IHaveDynamicData>;

    constructor(protected typeBuilder: DynamicTypeBuilder, private pageHeader: PageHeader, public componentFactoryResolver: ComponentFactoryResolver) {
        
    }
    
    ngOnInit() {
        
    }
    
    ngAfterViewInit() {
        this.initializeQtipTooltips();  // initialize qtips library for tooltips we use in the site it needs to be a global js function                           
    }
    
    private initializeQtipTooltips() {
        var me = this;
        $('body').on('mouseenter', '*[data-type]', function (event) {
            $(this).qtip({
                content: {
                    title: $(this).data('title'),
                    // Set the text to an image HTML string with the correct src URL to the loading image you want to use
                    text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                    ajax: {
                        url: "/resources/" + $(this).data("type") + "/" + $(this).data("id") + "/templates/tooltip/" + $(this).data("context") + "?isNg=true",
                        once: false,// ($(this).attr('data-cache') ? $(this).data('cache') : true),  // do we want to fetch the tooltip just once or recall each time?                            
                        success: function (data) {
                            if (!data || !data.length) {
                                this.destroy();
                            }
                            else {                                
                                if (me.componentRef) {
                                    me.componentRef.destroy();
                                }

                                // add router links
                                data = data.replace('href', 'routerLink');
                                // wrap with a div with id we know
                                data = `<div id='qTipContentCnt' style='display:none'>${data}</div>`;
                                
                                // here we get Factory (just compiled or from cache)
                                me.typeBuilder
                                    .createComponentFactory(data)
                                    .then((factory: ComponentFactory<IHaveDynamicData>) => {
                                        
                                        // Target will instantiate and inject component (we'll keep reference to it)                                        
                                        me.componentRef = me
                                            .dynamicComponentTarget
                                            .createComponent(factory);
                                    });
                                var qtipScope = this;
                                setTimeout(() => {                                  
                                    qtipScope.set('content.text', $('#qTipContentCnt'));
                                }, 100);                           
                            }
                        }
                    }
                },
                position: {
                    at: 'bottom center', // Position the tooltip above the link
                    my: 'top center',
                    viewport: $(window), // Keep the tooltip on-screen at all times
                    effect: false, // Disable positioning animation                  
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
                    classes: 'qtip-light qtip-shadow'
                }             
            });
        });
    }
    
}
