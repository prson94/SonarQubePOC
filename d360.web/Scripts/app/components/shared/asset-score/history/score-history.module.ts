import { NgModule } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { DirectivesModule } from '../../../../directives/directives.module';
import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { PipesModule } from '../../../../pipes/pipes.module';
import { ScoreHistoryComponent } from './score-history.component';
import { CoreModule } from '../../core.module';
import { CheckboxModule } from 'primeng/checkbox';

@NgModule({
    imports: [
        FormsModule,
        CommonModule,
        RouterModule,
        HttpClientModule,
        DirectivesModule,
        PipesModule,
        CoreModule,

        CheckboxModule
    ],
    declarations: [
        ScoreHistoryComponent
    ],
    exports: [
        ScoreHistoryComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
        DatePipe
    ]
})
export class ScoreHistoryModule { }
