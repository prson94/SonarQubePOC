import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    DataTableModule,
    EditorModule,
    InputSwitchModule,
    SharedModule,
    AutoCompleteModule,
    ButtonModule,

} from 'primeng/primeng';

import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';
import { D3SOverlayWindowModule } from '../overlay-window.component';

import { WorkflowDiagramComponent } from './workflow-diagram.component';
import { WorkflowStepEditorComponent } from './workflow-step-editor.component';
import { WorkflowTransitionEditorComponent } from './workflow-transition-editor.component';
import { WorkflowConditionEditorComponent } from './workflow-condition-editor.component';
import { WorkflowStepFormEditorComponent } from './workflow-step-form-editor.component';


@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        //d3s
        CoreModule,
        TilesModule,
        D3SOverlayWindowModule,

        //prime        
        DataTableModule,
        EditorModule,
        SharedModule,
        ButtonModule,

    ],
    declarations: [
        WorkflowDiagramComponent,
        WorkflowStepEditorComponent,
        WorkflowTransitionEditorComponent,
        WorkflowConditionEditorComponent,
        WorkflowStepFormEditorComponent,
    ],
    exports: [
        WorkflowDiagramComponent,
        WorkflowConditionEditorComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class WorkflowDiagramModule { }