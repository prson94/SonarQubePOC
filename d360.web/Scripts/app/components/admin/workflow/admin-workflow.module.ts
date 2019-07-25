import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { WorkflowDiagramModule } from '../../shared/diagram/workflow/workflow-diagram.module';
import { D3SEditorHeaderModule } from '../../shared/editor-header.component';
import { D3SSortIconModule } from '../../shared/turbotable-sorticon.component';
import { D3SColumnFilterModule } from '../../shared/turbotable-column-filter.component';



import { AdminWorkflowComponent } from './admin-workflow.component';
import { AdminWorkflowListComponent } from './admin-workflow-list.component';
import { AdminWorkflowEditorComponent } from './admin-workflow-editor.component';
import { AdminWorkflowDeleteComponent } from './admin-workflow-delete.component';

import { AdminWorkflowRoutingModule } from './admin-workflow.routes';
import { DirectivesModule } from '../../../directives/directives.module';


import {
    ButtonModule,
    CalendarModule,
    EditorModule,
    GrowlModule,
    InputTextModule,
    SharedModule,
    DropdownModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        AdminWorkflowRoutingModule,

        //prime  
        ButtonModule,
        CalendarModule,        
        GrowlModule,
        InputTextModule,
        SharedModule,
        EditorModule,
        DropdownModule,
        TableModule,

        //d3s                
        CoreModule,
        SharedDeleteFormModule,        
        SharedObjectDetailsModule,
        SharedGridPagingInfoModule,
        TilesModule,
        WorkflowDiagramModule,
        D3SEditorHeaderModule,
        DirectivesModule,
        D3SSortIconModule,
        D3SColumnFilterModule,

    ],
    declarations: [        
        AdminWorkflowComponent,
        AdminWorkflowListComponent,
        AdminWorkflowEditorComponent,
        AdminWorkflowDeleteComponent,        
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class AdminWorkflowModule { }