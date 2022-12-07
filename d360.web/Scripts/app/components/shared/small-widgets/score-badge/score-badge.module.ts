import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PipesModule } from '../../../../pipes/pipes.module';
import { IgBadgeModule } from '../../../shared/controls/badge/badge.module';

import { ScoreBadgeComponent } from './score-badge.component';
import { ScoreDisplayPipe } from '../../../../pipes/score-display.pipe';

@NgModule({
    imports: [CommonModule,
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