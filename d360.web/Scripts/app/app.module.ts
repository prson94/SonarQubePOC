import { NgModule }       from '@angular/core';
import { BrowserModule, Title  } from '@angular/platform-browser';
import { AppComponent }   from './app.component';
import { AppRoutingModule }        from './app.routes';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { COMPILER_PROVIDERS } from '@angular/compiler';

import {
    GrowlModule,    
} from 'primeng/primeng';

import { RightsidebarModule } from './components/shared/rightsidebar/right-sidebar.module';
import { SiteMenuModule } from './components/shared/menu/site-menu.module';
import { HeaderModule } from './components/shared/header/header.module';

import { AdminUserGuard } from './guards/admin-user.guard';

import { AuthenticationService } from './services/authentication.service';
import { MessagesService, HeaderBreadcrumbService, HeaderActionsService, RightSidebarService, WebAnalyticsService, StateService  } from './services/index';
import { DynamicTypeBuilder }     from './services/dynamic-type-builder';

import { AuthenticationConnectionBackend } from './authentication-connection-backend';


@NgModule({
    declarations: [          
        AppComponent,                          
    ],
    imports: [
        BrowserModule,        
        AppRoutingModule,
        HttpModule,

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
        COMPILER_PROVIDERS,
        DynamicTypeBuilder,
        Title,
        HeaderActionsService,
        HeaderBreadcrumbService,
        MessagesService,        
        RightSidebarService,
        WebAnalyticsService,
        StateService
    ],    
})
export class AppModule { }







