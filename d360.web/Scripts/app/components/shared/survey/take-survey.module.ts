import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../../shared/core.module';
import { ResourceModule } from '../../resource/resource.module';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { GovernRequestInterceptor } from '../../../http-interceptors/govern-request.interceptor';
import { TakeSurveyComponent } from './take-survey.component';
import { SiteModalModule } from '../modal/gov-modal.module';
import { RadioButtonModule } from 'primeng/radiobutton';
import { CheckboxModule } from 'primeng/checkbox';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        RadioButtonModule,
        CheckboxModule,
        
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
        
    ]
})
export class TakeSurveyModule { }