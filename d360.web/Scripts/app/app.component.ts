import { Component, AfterViewInit, ViewChild, ViewChildren, OnInit, ViewContainerRef, ComponentFactoryResolver, ComponentFactory, ComponentRef, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { MessagesService } from './services/messages.service';
import { HeaderBreadcrumbService } from './services/header-breadcrumb.service';
import { HeaderActionsService } from './services/header-actions.service';
import { RightSidebarService } from './services/right-sidebar.service';
import { StateService } from './services/state.service';
import { WebAnalyticsService } from './services/web-analytics.service';
import { DynamicTypeBuilder, IHaveDynamicData } from './services/dynamic-type-builder';
import { SiteMessage } from './models/site-message.model';
import { Subscription }   from 'rxjs/Subscription';
import { Message } from 'primeng/primeng';
import * as $ from 'jquery';
import 'qtip2';


@Component({
    selector: 'd3s-app',    
    template: ` <header>
                    <d3s-header></d3s-header>
                    <d3s-site-menu></d3s-site-menu>
                </header>
                <main>                                                                          
                    <div class="row">                         
                        <div class="col s12">            
                            <div class="maincontent">                                                                                                                                                                            
                                <router-outlet></router-outlet>                                                
                            </div>  
                        </div>                                                
                    </div>                    
                    <d3s-right-sidebar></d3s-right-sidebar>                        
                </main>
                <p-growl [immutable] ="false" [value]="msgs"></p-growl>
                <div #target></div>                
              `
})

export class AppComponent implements AfterViewInit, OnDestroy {        
    @ViewChild('target', { read: ViewContainerRef }) protected dynamicComponentTarget: ViewContainerRef;
    protected componentRef: ComponentRef<IHaveDynamicData>;
    subscription: Subscription;
    msgs: Message[];

    constructor(protected typeBuilder: DynamicTypeBuilder, public componentFactoryResolver: ComponentFactoryResolver, private messagesService: MessagesService) {
        this.msgs = [];
        this.subscription = messagesService.errorMessage$.subscribe(
            errorMsg => {
                this.msgs.push({ severity: 'error', summary: errorMsg.summary, detail: errorMsg.detail });
            });
        this.subscription = messagesService.infoMessage$.subscribe(
            infoMsg => {
                this.msgs.push({ severity: 'info', summary: infoMsg.summary, detail: infoMsg.detail });
            });
    }
        
    ngAfterViewInit() {
        this.initializeQtipTooltips();  // initialize qtips library for tooltips we use in the site it needs to be a global js function                           
    }

    ngOnDestroy() {
        // prevent memory leak when component destroyed
        this.subscription.unsubscribe();
    }
    
    private initializeQtipTooltips() {
        var me = this;
        $('body').on('mouseenter', '*[data-type]', function (event) {          
            $(this).qtip({
                content: {
                    title: $(this).data('title'),
                    // Set the text to an image HTML string with the correct src URL to the loading image you want to use
                   // text: '<i class="fa fa-spinner fa-spin fa-4x"></i>',
                    text: function (event, api) {                        
                        //api.set('content.text','<i class="fa fa-spinner fa-spin fa-4x"></i>');
                        // This time, we return the deferred object, not a 'Loading...' message.
                        $.ajax({
                            url: "/resources/" + $(this).data("type") + "/" + $(this).data("id") + "/templates/tooltip/" + $(this).data("context") + "?isNg=true" // Use data-url attribute for the URL
                        })
                            .then(function (data) {
                                // Return the content instead of using .set(). If you're wanting to select
                                // specific elements, see ther above section and adapt the `api.set` call
                                // into a `return elements` statement!
                                //return content
                                if (!data || !data.length) {
                                    this.destroy();
                                }
                                else {
                                    if (me.componentRef) {
                                        me.componentRef.destroy();
                                    }

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
                                    setTimeout(() => {
                                        api.set('content.text', $('#qTipContentCnt'));
                                    }, 100);
                                }
                            }, function (xhr, status, error) {
                                // Errors aren't handled by the library automatically, so
                                // you'll need to call .set() upon failure, just as before.
                                api.set('content.text', status + ': ' + error);
                            });
                        return '<i class="fa fa-spinner fa-spin fa-4x"></i>';
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
                },
            });
        });
    }    
}
