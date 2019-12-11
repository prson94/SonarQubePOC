import { NgModule, Component } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../../shared/core.module';
import { ResourceModule } from '../../resource/resource.module';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { GovernRequestInterceptor } from '../../../http-interceptors/govern-request.interceptor';
import { TakeSurveyComponent } from './take-survey.component';
import { SiteModalModule } from '../modal/gov-modal.module';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        RouterModule,
        
        //d3s        
        CoreModule,
        ResourceModule,
        SiteModalModule,
    ],
    declarations: [
        TakeSurveyComponent
    ],
    exports: [
        TakeSurveyComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class TakeSurveyModule { }