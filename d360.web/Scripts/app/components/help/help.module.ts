import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../http-interceptors/govern-post-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';

import { HelpRoutingModule } from './help.routes';

import { HelpComponent } from './help.component';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //routing 
        HelpRoutingModule,

        //d3s        
        CoreModule,        
    ],
    declarations: [
        HelpComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class HelpModule { }