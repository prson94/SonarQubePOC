import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { AutoCompleteModule } from 'primeng/autocomplete';
import { CalendarModule } from 'primeng/calendar';
import { ButtonModule } from 'primeng/button';
import { InputMaskModule } from 'primeng/inputmask';
import { SharedModule } from 'primeng/shared';
import { DataListModule } from 'primeng/datalist';
import { EditorModule } from 'primeng/editor';
import { ToggleButtonModule } from 'primeng/togglebutton';
import { MultiSelectModule } from 'primeng/multiselect';
import { TableModule } from 'primeng/table';

import { CoreModule } from '../../core.module';
import { TilesModule } from '../../tiles/tiles.module';
import { D3SOverlayWindowModule } from '../../overlay-window.component';
import { D3SEditorHeaderModule } from '../../editor-header.component';
import { SharedGridPagingInfoModule } from '../../grid-paging-info.component';

import { WorkflowDiagramComponent } from './workflow-diagram.component';
import { WorkflowStepEditorComponent } from './workflow-step-editor.component';
import { WorkflowTransitionEditorComponent } from './workflow-transition-editor.component';
import { WorkflowConditionEditorComponent } from './workflow-condition-editor.component';
import { WorkflowStepFormEditorComponent } from './workflow-step-form-editor.component';
import { WorkflowConditionListComponent } from './workflow-condition-list.component';
import { WorkflowTemplateToolComponent } from './workflow-template-tool.component';
import { WorkflowHistoryComponent } from './workflow-history.component';
import { WorkflowFormHistoryComponent } from './workflow-form-history.component';
import { WorkflowStepFieldChangeComponent } from './workflow-step-field-change.component';
import { WorkflowStepSummaryComponent } from './workflow-step-summary.component'
import { WorkflowTransitionSummaryComponent } from './workflow-transition-summary.component';
import { WorkflowResponsibilitySelectorComponent } from './workflow-responsibility-selector.component';
import { PipesModule } from '../../../../pipes/pipes.module';


import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //d3s
        CoreModule,
        TilesModule,
        D3SOverlayWindowModule,
        D3SEditorHeaderModule,
        SharedGridPagingInfoModule,
        ToggleButtonModule,

        //prime        
        EditorModule,
        SharedModule,
        ButtonModule,
        CalendarModule,
        InputMaskModule,
        DataListModule,
        AutoCompleteModule,
        MultiSelectModule,
        TableModule,
        PipesModule
    ],
    declarations: [
        WorkflowDiagramComponent,
        WorkflowStepEditorComponent,
        WorkflowTransitionEditorComponent,
        WorkflowConditionEditorComponent,
        WorkflowStepFormEditorComponent,
        WorkflowConditionListComponent,
        WorkflowTemplateToolComponent,
        WorkflowHistoryComponent,
        WorkflowFormHistoryComponent,
        WorkflowStepFieldChangeComponent,
        WorkflowStepSummaryComponent,
        WorkflowTransitionSummaryComponent,
        WorkflowResponsibilitySelectorComponent,
    ],
    exports: [
        WorkflowDiagramComponent,
        WorkflowConditionEditorComponent,
        WorkflowConditionListComponent,
        WorkflowTemplateToolComponent,
        WorkflowHistoryComponent,
        WorkflowFormHistoryComponent,
        WorkflowStepFieldChangeComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
        WorkflowFieldsService
    ]
})
export class WorkflowDiagramModule { }