import { AfterContentInit, Component, ElementRef, Inject, OnDestroy, Renderer2, ViewChild } from '@angular/core';
import { HeaderActionsService } from './services/header-actions.service';
import { Subscription } from 'rxjs';
import { Message, MessageService, PrimeNGConfig, Translation } from 'primeng/api';
import { CookieService } from './services/cookie.service';
import { MessagesObservableService } from './services/messages-observable.service';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { datadogRum } from '@datadog/browser-rum';
import { environment } from '../environments/environment';
import { DOCUMENT } from '@angular/common';

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
    @ViewChild('sidebar', { static: false, read: ElementRef }) sidebar: ElementRef;
    private timer: any;
    hideNav: boolean = false;
    secondNavOpen: boolean = false;

    constructor(
        private messagesService: MessagesObservableService,
        protected headerActionsService: HeaderActionsService,
        private cookieService: CookieService,
        private route: ActivatedRoute,
        private toastService: MessageService,
        private router: Router,
        @Inject(DOCUMENT) private document: Document,
        private renderer: Renderer2,
        private config: PrimeNGConfig) {
        this.msgs = [];

        this.enableDataDog();

        this.errorSub = messagesService.errorMessage$.subscribe(
            (errorMsg) => {
                this.toastService.add({ severity: 'error', summary: errorMsg.summary, detail: errorMsg.detail });
            });
        this.msgSub = messagesService.infoMessage$.subscribe(
            (infoMsg) => {
                this.toastService.add({ severity: 'info', summary: infoMsg.summary, detail: infoMsg.detail });
            });

        this.paramSub = this.route.queryParams.subscribe(() => {
            const url = new URL(window.location.href);
            const search = url.search;
            const params = new URLSearchParams(search);
            if (params.has('nonavigation')) {
                this.hideNav = params.get('nonavigation').toLowerCase() === 'true';
            }
        });

        this.router.events.subscribe((event) => {
            if (event instanceof NavigationEnd) {
                if (event.url === "/home") {
                    this.renderer.addClass(this.document.body, 'home-page');
                } else {
					this.renderer.removeClass(this.document.body, 'home-page');
                }
            }
        });

        this.config.setTranslation(this.primeNgTranslations);
    }

    ngAfterContentInit() {
        this.headerActionsService.emitFavoritesChange();//on first load when a non-default home page is defined, we need to update the action icons

        const menuState = this.cookieService.get("MenuState");
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
            console.log("Datadog Real user monitoring cannot be initialized!");
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
                {sidebarHeight = this.sidebar.nativeElement.children[0].getBoundingClientRect().height;}
            if (this.header?.nativeElement && this.header.nativeElement.children[0].children[0])
                {headerHeight = this.header.nativeElement.getBoundingClientRect().height;}

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

    private primeNgTranslations: Translation = {
        startsWith: $localize`Starts with`,
        contains: $localize`Contains`,
        notContains: $localize`Not contains`,
        endsWith: $localize`Ends with`,
        equals: $localize`Equals`,
        notEquals: $localize`Not equals`,
        noFilter: $localize`No Filter`,
        lt: $localize`Less than`,
        lte: $localize`Less than or equal to`,
        gt: $localize`Greater than`,
        gte: $localize`Greater than or equal to`,
        is: $localize`Is`,
        isNot: $localize`Is not`,
        before: $localize`Before`,
        after: $localize`After`,
        dateIs: $localize`Date is`,
        dateIsNot: $localize`Date is not`,
        dateBefore: $localize`Date is before`,
        dateAfter: $localize`Date is after`,
        clear: $localize`Clear`,
        apply: $localize`Apply`,
        matchAll: $localize`Match All`,
        matchAny: $localize`Match Any`,
        addRule: $localize`Add Rule`,
        removeRule: $localize`Remove Rule`,
        accept: $localize`Yes`,
        reject: $localize`No`,
        choose: $localize`Choose`,
        upload: $localize`Upload`,
        cancel: $localize`Cancel`,
        dayNames: [$localize`Sunday`, $localize`Monday`, $localize`Tuesday`, $localize`Wednesday`, $localize`Thursday`, $localize`Friday`, $localize`Saturday`],
        dayNamesShort: [
            $localize`:@@day_of_week.3letter.Sunday:Sun`,
            $localize`:@@day_of_week.3letter.Monday:Mon`,
            $localize`:@@day_of_week.3letter.Tuesday:Tue`,
            $localize`:@@day_of_week.3letter.Wednesday:Wed`,
            $localize`:@@day_of_week.3letter.Thursday:Thu`,
            $localize`:@@day_of_week.3letter.Friday:Fri`,
            $localize`:@@day_of_week.3letter.Saturday:Sat`],
        dayNamesMin: [
            $localize`:@@day_of_week.2letter.Sunday:Su`,
            $localize`:@@day_of_week.2letter.Monday:Mo`,
            $localize`:@@day_of_week.2letter.Tuesday:Tu`,
            $localize`:@@day_of_week.2letter.Wednesday:We`,
            $localize`:@@day_of_week.2letter.Thursday:Th`,
            $localize`:@@day_of_week.2letter.Friday:Fr`,
            $localize`:@@day_of_week.2letter.Saturday:Sa`],
        monthNames: [$localize`January`, $localize`February`, $localize`March`, $localize`April`, $localize`May`, $localize`June`, $localize`July`, $localize`August`, $localize`September`, $localize`October`, $localize`November`, $localize`December`],
        monthNamesShort: [
            $localize`:@@month_abbr.3letter.January:Jan`,
            $localize`:@@month_abbr.3letter.February:Feb`,
            $localize`:@@month_abbr.3letter.March:Mar`,
            $localize`:@@month_abbr.3letter.April:Apr`,
            $localize`:@@month_abbr.3letter.May:May`,
            $localize`:@@month_abbr.3letter.June:Jun`,
            $localize`:@@month_abbr.3letter.July:Jul`,
            $localize`:@@month_abbr.3letter.August:Aug`,
            $localize`:@@month_abbr.3letter.September:Sep`,
            $localize`:@@month_abbr.3letter.October:Oct`,
            $localize`:@@month_abbr.3letter.November:Nov`,
            $localize`:@@month_abbr.3letter.December:Dec`],
        today: $localize`Today`,
        weekHeader: $localize`Wk`,
        weak: $localize`Weak`,
        medium: $localize`Medium`,
        strong: $localize`Strong`,
        passwordPrompt: $localize`Enter a password`,
        emptyMessage: $localize`No results found`,
        emptyFilterMessage: $localize`No results found`
    };
}
