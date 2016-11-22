import { NgModule }       from '@angular/core';
import { BrowserModule, Title  } from '@angular/platform-browser';
import { AppComponent }   from './app.component';
import { FormsModule, ReactiveFormsModule }    from '@angular/forms';
import { AppRoutingModule }        from './app.routes';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { COMPILER_PROVIDERS } from '@angular/compiler';


import { PipesModule } from './pipes/pipes.module';
import { CoreModule } from './components/shared/core.module';
import { SearchModule } from './components/search/search.module';
import { WorkflowModule } from './components/workflow/workflow.module';
import { D3SSharedModule } from './components/shared/shared.module';
import { SocialModule } from './components/social/social.module';
import { HomeModule } from './components/home/home.module';


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
        FormsModule,
        ReactiveFormsModule,
        AppRoutingModule,
        HttpModule,
        

        //d3s modules
        PipesModule,
        SearchModule,
        WorkflowModule,
        D3SSharedModule,  
        SocialModule,                           
        CoreModule,                 
        HomeModule,                             
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







