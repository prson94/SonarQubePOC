import { NgModule }       from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedGridSelectionInfoModule } from '../shared/grid-selection-info.component';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';

import { WorkflowComponent } from './workflow.component';
import { WorkflowIssueDetailsComponent } from './workflow-issue-details.component';
import { WorkflowFormComponent } from './workflow-form.component';
import { WorkflowBulkFormComponent } from './workflow-bulk-form.component';
import { WorkflowRaiseIssueComponent } from './workflow-raise-issue.component';
import { WorkflowViewDetailsComponent } from './workflow-view-details.component';
import { WorkflowNewDetailComponent } from './workflow-new-details.component';
import { WorkflowBulkReassignComponent } from './workflow-bulk-reassign.component';
import { WorkflowFormFieldsComponent } from './workflow-form-fields.component';


import { WorkflowRoutingModule } from './workflow.routes';

import { SharedModule } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { CalendarModule } from 'primeng/calendar';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ToggleButtonModule } from 'primeng/togglebutton';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { TooltipModule } from 'primeng/tooltip';
import { EditorModule } from 'primeng/editor';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { SharedWorkflowMonitorModule } from '../shared/workflow/shared-workflow.module';
import { ResourceMultiSelectGridModule } from '../shared/resource-multiselect-grid.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        WorkflowRoutingModule,
        //primeng  
        CalendarModule,
        ToastModule,
        ToggleButtonModule,
        InputTextModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,                        
        MultiSelectModule,        
        TooltipModule,        
        EditorModule,
        AutoCompleteModule,
        SharedModule,
        TableModule,


        SharedWorkflowMonitorModule,
        //d3s
        
        CoreModule,
        PipesModule,
        TilesModule,
        D3SSharedModule,
        ResourceMultiSelectGridModule,
        SharedGridPagingInfoModule,
        SharedGridSelectionInfoModule,
        SharedDynamicGridEditorModule,
    ],
    declarations: [                        
        WorkflowComponent,                
        WorkflowIssueDetailsComponent,
        WorkflowFormComponent,        
        WorkflowRaiseIssueComponent,
        WorkflowViewDetailsComponent,        
        WorkflowNewDetailComponent,
        WorkflowBulkFormComponent,
        WorkflowBulkReassignComponent,
        WorkflowFormFieldsComponent,
    ],
    exports: [                        
        WorkflowRaiseIssueComponent,           
        WorkflowComponent,        
        WorkflowIssueDetailsComponent,
        WorkflowNewDetailComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class WorkflowModule { }