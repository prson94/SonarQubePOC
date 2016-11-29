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

import { AdminWorkflowComponent } from './admin-workflow.component';
import { WorkflowItemForm } from './workflow-item.form';

import { AdminWorkflowRoutingModule } from './admin-workflow.routes';

import {
    ButtonModule,
    CalendarModule,
    EditorModule,
    GrowlModule,
    InputTextModule,
    SharedModule,
    DataTableModule
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

        //d3s                
        CoreModule,
        SharedDeleteFormModule,        
        SharedObjectDetailsModule,
        SharedGridPagingInfoModule,
        TilesModule,
    ],
    declarations: [
        AdminWorkflowComponent,        
        WorkflowItemForm,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminWorkflowModule { }