import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';

import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { IgBadgeComponent } from './badge.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        TooltipModule
    ],
    declarations: [
        IgBadgeComponent,
    ],
    exports: [
        IgBadgeComponent,
    ],
    providers: [
        
    ]
})
export class IgBadgeModule { }