import { NgModule, Component } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';

import {
    ButtonModule,
} from 'primeng/primeng';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { SurveyRoutingModule } from './survey.routes';
import { SurveyComponent } from './survey.component';
import { ResourceModule } from '../../resource/resource.module';
import { SocialModule } from '../../shared/social/social.module';
import { D3SSharedModule } from '../../shared/shared.module';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { GovernRequestInterceptor } from '../../../http-interceptors/govern-request.interceptor';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,
        RouterModule,
        SocialModule,

        //routing 
        SurveyRoutingModule,

        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        ResourceModule,
        D3SSharedModule,
    ],
    declarations: [
        SurveyComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class SurveyModule { }