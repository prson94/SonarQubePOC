import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { WorkflowDiagramModule } from '../../shared/diagram/workflow/workflow-diagram.module';
import { D3SEditorHeaderModule } from '../../shared/editor-header.component';
import { D3SSortIconModule } from '../../shared/turbotable-sorticon.component';
import { D3SColumnFilterModule } from '../../shared/turbotable-column-filter.component';
import { DayOfWeekInputModule } from '../../shared/small-widgets/dayofweek-input/dayofweek-input.component';


import { AdminWorkflowComponent } from './admin-workflow.component';
import { AdminWorkflowListComponent } from './admin-workflow-list.component';
import { AdminWorkflowEditorComponent } from './admin-workflow-editor.component';
import { AdminWorkflowDeleteComponent } from './admin-workflow-delete.component';

import { AdminWorkflowRoutingModule } from './admin-workflow.routes';
import { DirectivesModule } from '../../../directives/directives.module';

import { SharedModule } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { EditorModule } from 'primeng/editor';
import { CalendarModule } from 'primeng/calendar';
import { ToastModule } from 'primeng/toast';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';
import { CheckboxModule } from 'primeng/checkbox';
import { CheckboxDirective } from '../../../directives/ig-checkbox-directive';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,

        AdminWorkflowRoutingModule,

        //prime  
        ButtonModule,
        CalendarModule,        
        ToastModule,
        InputTextModule,
        SharedModule,
        EditorModule,
        DropdownModule,
        TableModule,
        CheckboxModule,
 
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
        DayOfWeekInputModule
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