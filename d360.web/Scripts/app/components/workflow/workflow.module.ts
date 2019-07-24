import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
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


import { WorkflowRoutingModule } from './workflow.routes';

import {
    GrowlModule,
    CalendarModule,    
    InputTextModule,
    ToggleButtonModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    MultiSelectModule,
    TooltipModule,
    EditorModule,
    AutoCompleteModule,
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        WorkflowRoutingModule,
        //primeng  
        CalendarModule,
        GrowlModule,
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

        //d3s
        
        CoreModule,
        PipesModule,
        TilesModule,
        D3SSharedModule,
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