import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminMetricPassTestDetailsComponent } from './admin-metric-pass-test-details.component';

import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { DirectivesModule } from '../../shared/directives/directives.module';
import { PipesModule } from '../../../pipes/pipes.module';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        DirectivesModule,
        PipesModule
    ],
    declarations: [
        AdminMetricPassTestDetailsComponent,
    ],
    exports: [
        AdminMetricPassTestDetailsComponent
    ],
    providers: [
    ]
})

export class MetricPassTestDetailsModule { }
