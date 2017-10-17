import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../../authentication-connection-backend';

import {
    DataTableModule,
    EditorModule,
    InputSwitchModule,
    SharedModule,
    AutoCompleteModule,
    ButtonModule,
    CalendarModule,
    InputMaskModule,
    DataListModule,
    ToggleButtonModule,
} from 'primeng/primeng';

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

import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';


@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //d3s
        CoreModule,
        TilesModule,
        D3SOverlayWindowModule,
        D3SEditorHeaderModule,
        SharedGridPagingInfoModule,
        ToggleButtonModule,

        //prime        
        DataTableModule,
        EditorModule,
        SharedModule,
        ButtonModule,
        CalendarModule,
        InputMaskModule,
        DataListModule,
        AutoCompleteModule,

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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
        WorkflowFieldsService
    ]
})
export class WorkflowDiagramModule { }