import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PipesModule } from '../../../../pipes/pipes.module';
import { IgBadgeModule } from '../../../shared/controls/badge/badge.module';

import { ScoreBadgeComponent } from './score-badge.component';
import { DynamicPercentageModule } from '../dynamic-percentage/dynamic-percentage-module';
import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { ScoreDisplayPipe } from '../../../../pipes/score-display.pipe';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

@NgModule({
    imports: [CommonModule,
        DynamicPercentageModule,
        PipesModule,
        IgBadgeModule
    ],
    declarations: [
        ScoreBadgeComponent
    ],
    exports: [
        ScoreBadgeComponent
    ],
    providers: [
        
        ScoreDisplayPipe
    ]
})
export class ScoreBadgeModule { }