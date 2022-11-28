import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DirectivesModule } from '../../../../directives/directives.module';
import { ScoreDefinitionComponent } from './score-definition.component';
import { PipesModule } from '../../../../pipes/pipes.module';
import { MetricPassTestDetailsModule } from '../../../admin/scoring/admin-metric-pass-test-details.module';
import { MeasureConditionsDetailsComponent } from './measure-conditions-details.component';

@NgModule({
    imports: [
        FormsModule,
        CommonModule,
        RouterModule,

        DirectivesModule,
        PipesModule,

        MetricPassTestDetailsModule,

    ],
    declarations: [
        ScoreDefinitionComponent,
        MeasureConditionsDetailsComponent
    ],
    exports: [
        ScoreDefinitionComponent,
        MeasureConditionsDetailsComponent
    ],
    providers: [
        
    ]
})
export class ScoreDefinitionModule { }
