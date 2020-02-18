import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { PipesModule } from '../../../../pipes/pipes.module';

import { ScoreBadgeComponent } from './score-badge.component';
import { DynamicPercentageModule } from '../dynamic-percentage/dynamic-percentage-module';
import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        DynamicPercentageModule,
        PipesModule
    ],
    declarations: [
        ScoreBadgeComponent
    ],
    exports: [
        ScoreBadgeComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class ScoreBadgeModule { }