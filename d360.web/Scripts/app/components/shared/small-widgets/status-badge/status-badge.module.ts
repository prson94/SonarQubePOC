import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IgBadgeModule } from '../../../shared/controls/badge/badge.module';

import { StatusBadgeComponent } from './status-badge.component';
import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

@NgModule({
    imports: [
        CommonModule,
        IgBadgeModule
    ],
    declarations: [
        StatusBadgeComponent
    ],
    exports: [
        StatusBadgeComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class StatusBadgeModule { }