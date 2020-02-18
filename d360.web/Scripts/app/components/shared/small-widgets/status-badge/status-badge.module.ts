import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';

import { StatusBadgeComponent } from './status-badge.component';
import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
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