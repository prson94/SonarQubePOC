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
import { TooltipModule } from "primeng/tooltip";

@NgModule({
    imports: [
        FormsModule,
        CommonModule,
        RouterModule,

        DirectivesModule,
        PipesModule,
        CoreModule,

        CheckboxModule,
        TooltipModule
    ],
    declarations: [
        ScoreHistoryComponent
    ],
    exports: [
        ScoreHistoryComponent
    ],
    providers: [
        
        DatePipe
    ]
})
export class ScoreHistoryModule { }
