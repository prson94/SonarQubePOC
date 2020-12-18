import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { DirectivesModule } from '../../../../directives/directives.module';
import { GovernRequestInterceptor } from '../../../../http-interceptors/govern-request.interceptor';
import { ScoreDefinitionComponent } from './score-definition.component';
import { CoreModule } from '../../core.module';
import { PipesModule } from '../../../../pipes/pipes.module';

@NgModule({
    imports: [
        FormsModule,
        CommonModule,
        RouterModule,
        HttpClientModule,
        DirectivesModule,
        PipesModule

    ],
    declarations: [
        ScoreDefinitionComponent
    ],
    exports: [
        ScoreDefinitionComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        }
    ]
})
export class ScoreDefinitionModule { }
