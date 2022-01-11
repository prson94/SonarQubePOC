import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IgBadgeModule } from '../../../shared/controls/badge/badge.module';

import { SimpleBadgeComponent } from './simple-badge.component';
import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

@NgModule({
    imports: [
        CommonModule,
        IgBadgeModule
    ],
    declarations: [
        SimpleBadgeComponent
    ],
    exports: [
        SimpleBadgeComponent
    ],
    providers: [
        
    ]
})
export class SimpleBadgeModule { }