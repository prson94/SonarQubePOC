import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";
import { RouterModule } from '@angular/router';

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';

import { AdminSurveysComponent } from './admin-surveys.component';
import { AdminSurveyQuestionEditorEditor } from './admin-survey-question-editor.component';
import { AdminSurveyQuestionsComponent } from './admin-survey-questions.component';

import { AdminSurveysRoutingModule } from './admin-surveys.routes';

import {
    ButtonModule,
    EditorModule,
    InputTextModule,
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        AdminSurveysRoutingModule,

        //prime      
        ButtonModule,
        EditorModule,
        InputTextModule,
        SharedModule,
        TableModule,

        //d3s                
        CoreModule,                
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,
        SharedObjectDetailsModule,
        SharedGridPagingInfoModule,
        TilesModule,
    ],
    declarations: [
        AdminSurveysComponent,
        AdminSurveyQuestionEditorEditor,
        AdminSurveyQuestionsComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class AdminSurveysModule { }