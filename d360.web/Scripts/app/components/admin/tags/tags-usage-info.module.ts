import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { TagUsageInfoBox } from './tags-usage-info.component';
import { PipesModule } from '../../../pipes/pipes.module';
import { TooltipModule } from 'primeng/tooltip';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,
        PipesModule, 

        //prime
        TooltipModule,
    ],
    declarations: [
        TagUsageInfoBox
    ],
    exports: [
        TagUsageInfoBox,        
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class TagUsageInfoModule { }