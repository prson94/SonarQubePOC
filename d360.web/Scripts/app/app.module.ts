import { NgModule, LOCALE_ID, APP_INITIALIZER } from '@angular/core';
import { CommonModule, registerLocaleData } from '@angular/common';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { BrowserModule, Title } from '@angular/platform-browser';
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app.routes';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { ToastModule } from 'primeng/toast';

import { RightsidebarModule } from './components/shared/rightsidebar/right-sidebar.module';
import { SiteMenuModule } from './components/shared/menu/site-menu.module';
import { HeaderModule } from './components/shared/header/header.module';

import { AdminUserGuard } from './guards/admin-user.guard';
import { RedirectGuard } from './guards/redirect.guard';

import { AuthenticationService } from './services/authentication.service';
import { MessagesObservableService } from "./services/messages-observable.service";
import { HeaderBreadcrumbService } from './services/header-breadcrumb.service';
import { HeaderActionsService } from './services/header-actions.service';
import { SecondaryNavService } from './services/right-sidebar.service';
import { FavoritesService } from './services/favorites.service';
import { FollowerService } from './services/follower.service';
import { StateService } from './services/state.service';
import { WebAnalyticsService } from './services/web-analytics.service';
import { ApplicationInsightsService } from './services/application-insights.service';
import { SearchService } from './services/search.service';

import { TooltipSingletonService } from './services/tooltip-singleton.service'
import { PreviewpopupSingletonService } from './services/previewpopup-singleton.service'
import { GovernRequestInterceptor } from "./http-interceptors/govern-request.interceptor";
import { CookieService } from './services/cookie.service';
import { SiteMenuService } from './services/site-menu.service';
import { DialogModule } from 'primeng/dialog';
import { D3SModal } from './components/shared/modal/gov-modal.component';
import { AssetStyleService } from './services/asset-style.service';
import { FeatureFlagsService } from './services/featureflags.service';

export function localeIdFactory() {
    return navigator.language;
}

export function featureFlagServiceInitializer(provider: FeatureFlagsService) {
    return () => provider.initialize().subscribe((s) => {
        provider.createClientConnection();
    });
}

export function localeInitializer(localeId: string) {                  
    return (): Promise<any> => {
        if (localeId && localeId.toLowerCase() != 'en-us') {
            return new Promise((resolve, reject) => {
                import(`@angular/common/locales/${localeId}.js`)
                    .then(module => {
                        console.log(`Govern locale is set to [${localeId}]`);
                        registerLocaleData(module.default);
                        resolve('');
                    }).catch(() => {
                        if (localeId.indexOf('-') !== -1) {
                            import(`@angular/common/locales/${localeId.split('-')[0]}.js`)
                                .then(module => {
                                    console.log(`Govern locale is set to [${localeId.split('-')[0]}]`);
                                    registerLocaleData(module.default);
                                    resolve('');
                                }, reject);

                        }
                        else {
                            reject;
                        }
                    });

            });
        }
        else {
            console.log('Govern locale defaulting to [en-US]');
        }
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
        ToastModule,
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
        RedirectGuard,
        AuthenticationService,
        Title,
        HeaderActionsService,
        HeaderBreadcrumbService,
        MessagesObservableService,
        SecondaryNavService,
        FavoritesService,
        FollowerService,
        AssetStyleService,
        WebAnalyticsService,
        TooltipSingletonService,
        PreviewpopupSingletonService,
        StateService,
        CookieService,
        SiteMenuService,
        { provide: LOCALE_ID, useFactory: localeIdFactory },
        {
            provide: APP_INITIALIZER,
            multi: true,
            useFactory: localeInitializer,
            deps: [LOCALE_ID]
        },
        {
            provide: APP_INITIALIZER,
            multi: true,
            useFactory: featureFlagServiceInitializer,
            deps: [FeatureFlagsService]
        },
        ApplicationInsightsService,
        SearchService
    ],
    entryComponents: [D3SModal],

})

export class AppModule { }
