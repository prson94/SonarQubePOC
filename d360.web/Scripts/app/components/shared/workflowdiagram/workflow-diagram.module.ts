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

import { WorkflowDiagramComponent } from './workflow-diagram.component';


@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        //d3s
        CoreModule,
        TilesModule,

        //prime        
        DataTableModule,
        EditorModule,
        SharedModule,
        ButtonModule,

        WorkflowDiagramComponent,
    ],
    declarations: [
        WorkflowDiagramComponent,
    ],
    exports: [
        WorkflowDiagramComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class WorkflowDiagramModule { }