import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { IgBadgeComponent } from './badge.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule

    ],
    declarations: [
        IgBadgeComponent,
    ],
    exports: [
        IgBadgeComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class IgBadgeModule { }