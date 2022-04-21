import { Component, OnDestroy, AfterContentInit, ViewChild, ElementRef } from '@angular/core';
import { HeaderActionsService } from './services/header-actions.service';
import { Subscription } from 'rxjs';
import { Message } from 'primeng/api';
import { CookieService } from './services/cookie.service';
import { MessagesObservableService } from './services/messages-observable.service';
import { MessageService } from 'primeng/api';
import { ApplicationInsightsService } from './services/application-insights.service';
import { ActivatedRoute } from '@angular/router';
import { datadogRum } from '@datadog/browser-rum';
import { environment } from '../environments/environment';

declare var CurrentResourceID;
declare var VersionNumber: string;
declare var ResourceName;
declare var ResourceEmail;
declare var DataDogApplicationId;
declare var DataDogClientToken;
declare var DataDogService;

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
                            <div [ngClass]="{maincontent: !menuOpen, 'maincontent-open': menuOpen, 'maincontent-second': secondNavOpen}" [style.margin-left]="hideNav ? '0' : null">
                                <router-outlet></router-outlet>
                            </div>
                        </div>
                    </div>
                </main>
                <p-toast [baseZIndex]="200001"></p-toast>
              `,
    providers: [MessageService]
})

export class AppComponent implements AfterContentInit, OnDestroy {    
    msgSub: Subscription;
    errorSub: Subscription;
    paramSub: Subscription;
    msgs: Message[];
    public menuOpen: boolean = true;
    public maxContentPaneHeight: number = 1000;
    @ViewChild('header', { static: false }) header: ElementRef;
    @ViewChild('sidebar', {static: false, read: ElementRef }) sidebar: ElementRef;
    private timer: any;
    hideNav: boolean = false;
    secondNavOpen: boolean = false;

    constructor(                
        private messagesService: MessagesObservableService,        
        protected headerActionsService: HeaderActionsService,
        protected aiService: ApplicationInsightsService,
        private cookieService: CookieService,
        private route: ActivatedRoute,
        private toastService: MessageService) {
        this.msgs = [];

        this.enableDataDog();
        
        this.errorSub = messagesService.errorMessage$.subscribe(
            errorMsg => {
                this.toastService.add({ severity: 'error', summary: errorMsg.summary, detail: errorMsg.detail });
            });
        this.msgSub = messagesService.infoMessage$.subscribe(
            infoMsg => {       
                this.toastService.add({ severity: 'info', summary: infoMsg.summary, detail: infoMsg.detail });
            });
        this.aiService.setUserId(String(CurrentResourceID));

        this.paramSub = this.route.queryParams.subscribe((params) => {
            if (params['nonavigation'] != null) {
                this.hideNav = params['nonavigation'].toLocaleLowerCase() === 'true';
            }
        });

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

    private enableDataDog() {
        try {
        // Only turn on datadog Real user monitoring when Govern is in prod mode we dont want errors from developers building govern reported.
        if (environment.production) {
                datadogRum.init({
                    applicationId: DataDogApplicationId,
                    clientToken: DataDogClientToken,
                    site: 'datadoghq.com',
                    service: DataDogService,
                    env: location.hostname,
                    version: VersionNumber,
                    sampleRate: 100,
                    trackInteractions: true,
                    defaultPrivacyLevel: 'mask-user-input',
                    allowedTracingOrigins: [/https:\/\/.*\.data3sixty\.com/, /https:\/\/.*\.data3sixty\.local/]
                });

                datadogRum.setUser({
                    id: CurrentResourceID,
                    name: ResourceName,
                    email: ResourceEmail,
                });

                datadogRum.startSessionReplayRecording();
           }
        }
        catch {
            console.log("Datadog Real user monitoring cannot be initialized!")
        }
    }

    public handleMenuChange(v: boolean) {
        this.menuOpen = v;
        this.cookieService.set("MenuState", v + "");
    }  

    public setMaxHeight() {
        clearTimeout(this.timer);
        this.timer = window.setTimeout(() => {
            let headerHeight = 0;
            let sidebarHeight = 0;
            if (this.sidebar?.nativeElement && this.sidebar.nativeElement.children[0]) 
                sidebarHeight = this.sidebar.nativeElement.children[0].getBoundingClientRect().height;
            if (this.header?.nativeElement && this.header.nativeElement.children[0].children[0]) 
                headerHeight = this.header.nativeElement.getBoundingClientRect().height;

            this.maxContentPaneHeight = (window.innerHeight > 100) ? ((window.innerHeight - (headerHeight + sidebarHeight))) : 100;
            this.secondNavOpen = sidebarHeight > 61;
        }, 200);
    }

    ngOnDestroy() {
        if (this.errorSub) {
            this.errorSub.unsubscribe();
        }
        if (this.msgSub) {
            this.msgSub.unsubscribe();
        }
        if (this.paramSub) {
            this.paramSub.unsubscribe();
        }
    }
}
