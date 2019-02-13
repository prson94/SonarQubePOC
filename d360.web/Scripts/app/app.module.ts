import { NgModule, LOCALE_ID } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { BrowserModule, Title  } from '@angular/platform-browser';
import { AppComponent }   from './app.component';
import { AppRoutingModule }        from './app.routes';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import {
    GrowlModule,    
} from 'primeng/components/growl/growl';

import { RightsidebarModule } from './components/shared/rightsidebar/right-sidebar.module';
import { SiteMenuModule } from './components/shared/menu/site-menu.module';
import { HeaderModule } from './components/shared/header/header.module';

import { AdminUserGuard } from './guards/admin-user.guard';

import { AuthenticationService } from './services/authentication.service';
import { MessagesService } from './services/messages.service';
import { HeaderBreadcrumbService } from './services/header-breadcrumb.service';
import { HeaderActionsService } from './services/header-actions.service';
import { RightSidebarService } from './services/right-sidebar.service';
import { StateService } from './services/state.service';
import { WebAnalyticsService } from './services/web-analytics.service';
import { TooltipSingletonService } from './services/tooltip-singleton.service'

import { AuthenticationConnectionBackend } from './authentication-connection-backend';

export function getLocale() {
    console.log(navigator.language);
    return navigator.language;
}


@NgModule({
    declarations: [          
        AppComponent,                          
    ],
    imports: [
        CommonModule,
        // import deprecated module after
        DeprecatedI18NPipesModule,
        BrowserModule,
        HttpClientModule,
        AppRoutingModule,
        HttpModule,
        BrowserAnimationsModule,

        // prime 
        GrowlModule,

        //d3s modules                                            
        RightsidebarModule,
        SiteMenuModule,
        HeaderModule,                                 
    ],
    bootstrap: [AppComponent],
    providers: [
        AdminUserGuard,
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
        AuthenticationService,        
        Title,
        HeaderActionsService,
        HeaderBreadcrumbService,
        MessagesService,        
        RightSidebarService,
        WebAnalyticsService,
        TooltipSingletonService,
        StateService,
        {
            provide: LOCALE_ID,
            useFactory: () => { navigator.language }
        }
    ],    
})
export class AppModule { }