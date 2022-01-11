import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { DynamicPercentageComponent } from './dynamic-percentage-component';
import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,        
    ],
    declarations: [
        DynamicPercentageComponent
    ],
    exports: [
        DynamicPercentageComponent
    ],
    providers: [
        
    ]
})
export class DynamicPercentageModule { }