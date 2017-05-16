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
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { WorkflowDiagramModule } from '../../shared/diagram/workflow/workflow-diagram.module';
import { D3SEditorHeaderModule } from '../../shared/editor-header.component';


import { AdminWorkflowComponent } from './admin-workflow.component';
import { AdminWorkflowListComponent } from './admin-workflow-list.component';
import { AdminWorkflowEditorComponent } from './admin-workflow-editor.component';
import { AdminWorkflowDeleteComponent } from './admin-workflow-delete.component';

import { AdminWorkflowRoutingModule } from './admin-workflow.routes';



import {
    ButtonModule,
    CalendarModule,
    EditorModule,
    GrowlModule,
    InputTextModule,
    SharedModule,
    DataTableModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,

        AdminWorkflowRoutingModule,

        //prime  
        ButtonModule,
        CalendarModule,        
        GrowlModule,
        InputTextModule,
        SharedModule,
        DataTableModule,
        EditorModule,

        //d3s                
        CoreModule,
        SharedDeleteFormModule,        
        SharedObjectDetailsModule,
        SharedGridPagingInfoModule,
        TilesModule,
        WorkflowDiagramModule,
        D3SEditorHeaderModule,

    ],
    declarations: [        
        AdminWorkflowComponent,
        AdminWorkflowListComponent,
        AdminWorkflowEditorComponent,
        AdminWorkflowDeleteComponent,        
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminWorkflowModule { }