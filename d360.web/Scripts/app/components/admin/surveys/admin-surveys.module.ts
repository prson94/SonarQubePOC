import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

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
    DataTableModule    
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,

        AdminSurveysRoutingModule,

        //prime      
        ButtonModule,
        EditorModule,
        InputTextModule,
        SharedModule,
        DataTableModule,

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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminSurveysModule { }