import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { ChipsFilterComponent } from './chips-filter-component';
import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { DirectivesModule } from '../../../../directives/directives.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        DirectivesModule,
    ],
    declarations: [
        ChipsFilterComponent
    ],
    exports: [
        ChipsFilterComponent
    ],
    providers: [
        
    ]
})
export class ChipsFilterModule { }