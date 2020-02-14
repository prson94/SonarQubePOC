import { NgModule, LOCALE_ID, APP_INITIALIZER } from '@angular/core';
import { CommonModule, registerLocaleData } from '@angular/common';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { BrowserModule, Title } from '@angular/platform-browser';
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app.routes';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { GrowlModule } from 'primeng/components/growl/growl';

import { RightsidebarModule } from './components/shared/rightsidebar/right-sidebar.module';
import { SiteMenuModule } from './components/shared/menu/site-menu.module';
import { HeaderModule } from './components/shared/header/header.module';

import { AdminUserGuard } from './guards/admin-user.guard';

import { AuthenticationService } from './services/authentication.service';
import { MessagesObservableService } from "./services/messages-observable.service";
import { HeaderBreadcrumbService } from './services/header-breadcrumb.service';
import { HeaderActionsService } from './services/header-actions.service';
import { SecondaryNavService } from './services/right-sidebar.service';
import { StateService } from './services/state.service';
import { WebAnalyticsService } from './services/web-analytics.service';

import { TooltipSingletonService } from './services/tooltip-singleton.service'
import { GovernRequestInterceptor } from "./http-interceptors/govern-request.interceptor";
import { CookieService } from './services/cookie.service';
import { SiteMenuService } from './services/site-menu.service';
import { DialogModule } from 'primeng/dialog';
import { D3SModal } from './components/shared/modal/gov-modal.component';
import { AssetStyleService } from './services/asset-style.service';

declare var System;

export class LocaleService {
    getLocale(): string {
        if (typeof window === 'undefined' || typeof window.navigator === 'undefined') {
            return undefined;
        }

        let browserLang: any = window.navigator['languages'] ? window.navigator['languages'][0] : null;
        browserLang = browserLang || window.navigator.language || window.navigator['browserLanguage'] || window.navigator['userLanguage'];

        if (browserLang.indexOf('-') !== -1) {
            browserLang = browserLang.split('-')[0];
        }

        if (browserLang.indexOf('_') !== -1) {
            browserLang = browserLang.split('_')[0];
        }

        return browserLang;
    }
}

export function localeIdFactory(localeService: LocaleService) {
    return window.navigator.language;
}

export function localeInitializer(localeId: string) {
    return (): Promise<any> => {
        return new Promise((resolve, reject) => {
            import(/* webpackInclude: /(de|en|ca|no|fi|sv|tr|en-GB|en-US)\.js$/ */`@angular/common/locales/${localeId}.js`)
                .then(module => {
                    registerLocaleData(module.default);
                    resolve();
                }, reject);
        });
    };
}

@NgModule({
    declarations: [
        AppComponent,
    ],
    imports: [
        CommonModule,        
        BrowserModule,
        HttpClientModule,
        AppRoutingModule,
        BrowserAnimationsModule,

        // prime 
        GrowlModule,
        DialogModule,

        //d3s modules                                            
        RightsidebarModule,
        SiteMenuModule,
        HeaderModule,
    ],
    bootstrap: [AppComponent],
    providers: [
        AdminUserGuard,
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
        AuthenticationService,
        Title,
        HeaderActionsService,
        HeaderBreadcrumbService,
        MessagesObservableService,
        SecondaryNavService,
        AssetStyleService,
        WebAnalyticsService,
        TooltipSingletonService,
        StateService,
        CookieService,
        SiteMenuService,
        /*{
            provide: LOCALE_ID,
            useFactory: () => {
                navigator.language
            }*/
        //},
        LocaleService,
        { provide: LOCALE_ID, useFactory: localeIdFactory, deps: [LocaleService] },
        {
            provide: APP_INITIALIZER,
            multi: true,
            useFactory: localeInitializer,
            deps: [LOCALE_ID]
        }
    ],
    entryComponents: [D3SModal],

})

export class AppModule { }
