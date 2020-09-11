import { Component, OnDestroy, AfterContentInit, ViewChild, ElementRef } from '@angular/core';
import { HeaderActionsService } from './services/header-actions.service';
import { Subscription } from 'rxjs';
import { Message } from 'primeng/api';
import { CookieService } from './services/cookie.service';
import { MessagesObservableService } from './services/messages-observable.service';
import { MessageService } from 'primeng/api';
import { ApplicationInsightsService } from './services/application-insights.service';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-app',
    host: {
        '(window:resize)': 'setMaxHeight()'
    },  
    template: ` <header #header>
                    <d3s-header></d3s-header>
                    <d3s-site-menu (menuChanged)="handleMenuChange($event)" [menuOpen]="menuOpen"></d3s-site-menu>
                </header>
                <main>
                    <d3s-right-sidebar #sidebar [menuOpen]="menuOpen" (changed)="setMaxHeight()"></d3s-right-sidebar>
                    <div class="row d3s-content-pane" [ngStyle]="{'height.px': maxContentPaneHeight}">
                        <div class="row">
                            <div [class.maincontent]="!menuOpen" [class.maincontent-open]="menuOpen">
                                <router-outlet></router-outlet>
                            </div>
                        </div>
                    </div>
                </main>
                <p-toast [baseZIndex]="20000"></p-toast>
              `,
    providers: [MessageService]
})

export class AppComponent implements AfterContentInit, OnDestroy {    
    subscription: Subscription;
    msgs: Message[];
    public menuOpen: boolean = true;
    public maxContentPaneHeight: number = 1000;
    @ViewChild('header', { static: false }) header: ElementRef;
    @ViewChild('sidebar', {static: false, read: ElementRef }) sidebar: ElementRef;
    private timer: any;

    constructor(                
        private messagesService: MessagesObservableService,        
        protected headerActionsService: HeaderActionsService,
        protected aiService: ApplicationInsightsService,
        private cookieService: CookieService,
        private toastService: MessageService) {
        this.msgs = [];
        
        this.subscription = messagesService.errorMessage$.subscribe(
            errorMsg => {
                this.toastService.add({ severity: 'error', summary: errorMsg.summary, detail: errorMsg.detail });
            });
        this.subscription = messagesService.infoMessage$.subscribe(
            infoMsg => {       
                this.toastService.add({ severity: 'info', summary: infoMsg.summary, detail: infoMsg.detail });
            });
        this.aiService.setUserId(String(CurrentResourceID));
    }

    ngAfterContentInit() {        
        this.headerActionsService.emitFavoritesChange();//on first load when a non-default home page is defined, we need to update the action icons

        let menuState = this.cookieService.get("MenuState");
        if ((menuState + "") == "") {
            this.cookieService.set("MenuState", "true");
            this.handleMenuChange(true);
        } else {
            this.handleMenuChange(menuState.toLocaleLowerCase() == "true");
        }
        this.setMaxHeight();
    }

    private handleMenuChange(v: boolean) {
        this.menuOpen = v;
        this.cookieService.set("MenuState", v + "");
    }  

    private setMaxHeight() {
        clearTimeout(this.timer);
        this.timer = window.setTimeout(() => {
            let headerHeight = 0;
            let sidebarHeight = 0;
            if (this.sidebar.nativeElement && this.sidebar.nativeElement.children[0]) 
                sidebarHeight = this.sidebar.nativeElement.children[0].getBoundingClientRect().height;
            if (this.header.nativeElement && this.header.nativeElement.children[0].children[0]) 
                headerHeight = this.header.nativeElement.getBoundingClientRect().height;

            this.maxContentPaneHeight = (window.innerHeight > 100) ? ((window.innerHeight - (headerHeight + sidebarHeight))) : 100;
        }, 200);
    }

    ngOnDestroy() {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }
}
