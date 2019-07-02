import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";

import { RightSidebarItemComponent } from './right-sidebar-item.component';
import { RightSidebarComponent } from './right-sidebar.component';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,         
    ],
    declarations: [
        RightSidebarItemComponent,
        RightSidebarComponent
    ],
    exports: [        
        RightSidebarComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class RightsidebarModule { }