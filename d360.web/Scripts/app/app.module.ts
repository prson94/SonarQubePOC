import { APP_INITIALIZER, LOCALE_ID, NgModule } from '@angular/core';
import { CommonModule, registerLocaleData } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { BrowserModule, Title } from '@angular/platform-browser';
import { RouteReuseStrategy } from '@angular/router';
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app.routes';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { ToastModule } from 'primeng/toast';

import { RightsidebarModule } from './components/shared/rightsidebar/right-sidebar.module';
import { SiteMenuModule } from './components/shared/menu/site-menu.module';
import { HeaderModule } from './components/shared/header/header.module';

import { NumberOfRowsByCategoryServiceInitializer } from './services/number-of-rows-by-category.service';

import { DialogModule } from 'primeng/dialog';
import { CompanySettingsService } from './services/settings.service';
import { governHttpInterceptorProviders } from './http-interceptors';
import { ForceNoReuseStrategy } from './services/forceNoReuseStrategy';
import { AngularSplitModule } from 'angular-split';
import { FeatureFlagsInitService } from './services/feature-flags-init.service';

export function localeIdFactory() {
    return navigator.language;
}

export function featureFlagServiceInitializer(provider: FeatureFlagsInitService) {
    return () => provider.initialize();
}

export function settingsInitializer(provider: CompanySettingsService) {
    return () => provider.loadSettings().then((r) => { provider.loadApplicationSettings(); });
}

export function localeInitializer(localeId: string) {                  
    return (): Promise<any> => {
        if (localeId && localeId.toLowerCase() !== 'en-us') {
            return new Promise((resolve, reject) => {
                //Dynamic import of locales issue in Angular 13 https://github.com/angular/angular-cli/issues/22154
				import(`/node_modules/@angular/common/locales/${localeId}.mjs`)
                    .then((module) => {
                        console.log(`Govern locale is set to [${localeId}]`);
                        registerLocaleData(module.default);
                        resolve('');
                    }).catch(() => {
                        if (localeId.indexOf('-') !== -1) {
							import(`/node_modules/@angular/common/locales/${localeId.split('-')[0]}.mjs`)
                                .then((module) => {
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
        AngularSplitModule,
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
        governHttpInterceptorProviders,
        Title,
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
            deps: [FeatureFlagsInitService]
        },
        {
            provide: APP_INITIALIZER,
            multi: true,
            useFactory: settingsInitializer,
            deps: [CompanySettingsService]
        },
        NumberOfRowsByCategoryServiceInitializer,
        { provide: RouteReuseStrategy, useClass: ForceNoReuseStrategy },
    ]
})

export class AppModule { }
