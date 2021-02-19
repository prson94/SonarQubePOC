import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminMetricPassTestDetailsComponent } from './admin-metric-pass-test-details.component';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
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
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        }
    ]
})

export class MetricPassTestDetailsModule { }
