import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedGridSelectionInfoModule } from '../shared/grid-selection-info.component';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { PipesModule } from '../../pipes/pipes.module';

import { WorkflowComponent } from './workflow.component';
import { WorkflowIssueDetailsComponent } from './workflow-issue-details.component';
import { WorkflowIssueEditorComponent } from './workflow-issue-editor.component';
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
        HttpModule,
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
        SharedGridPagingInfoModule,
        SharedGridSelectionInfoModule,
        SharedDynamicGridEditorModule,
    ],
    declarations: [                        
        WorkflowComponent,                
        WorkflowIssueDetailsComponent,
        WorkflowIssueEditorComponent,
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
        WorkflowIssueEditorComponent,
        WorkflowNewDetailComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class WorkflowModule { }