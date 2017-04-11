import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';

import { WorkflowCertifyDetailsComponent } from './workflow-certify-details.component';
import { WorkflowCertifyEditorComponent } from './workflow-certify-editor.component';
import { WorkflowComponent } from './workflow.component';
import { WorkflowDetailComponent } from './workflow-detail.component';
import { WorkflowDetailedViewComponent } from './workflow-detailed-view.component';
import { WorkflowIssueDetailsComponent } from './workflow-issue-details.component';
import { WorkflowIssueEditorComponent } from './workflow-issue-editor.component';
import { WorkflowFormComponent } from './workflow-form.component';
import { WorkflowRaiseIssueComponent } from './workflow-raise-issue.component';
import { WorkflowSuggestDetailsComponent } from './workflow-suggest-details.component';
import { WorkflowSuggestEditorComponent } from './workflow-suggest-editor.component';
import { WorkflowViewStatusComponent } from './workflow-view-status.component';
import { WorkflowWorkItemComponent } from './workflow-work-item.component';
import { WorkflowViewDetailsComponent } from './workflow-view-details.component';
import { WorkflowNewDetailComponent } from './workflow-new-details.component';

import { WorkflowRoutingModule } from './workflow.routes';

import {
    GrowlModule,
    CalendarModule,
    InputTextModule,
    DataTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    MultiSelectModule,
    TooltipModule,
    EditorModule,
    AutoCompleteModule,
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        WorkflowRoutingModule,
        //primeng  
        CalendarModule,
        GrowlModule,
        InputTextModule,        
        DataTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,                        
        MultiSelectModule,        
        TooltipModule,        
        EditorModule,
        AutoCompleteModule,
        SharedModule,

        //d3s
        
        CoreModule,
        TilesModule,
        SharedGridPagingInfoModule,
        SharedDynamicGridEditorModule,
    ],
    declarations: [        
        WorkflowCertifyDetailsComponent,
        WorkflowCertifyEditorComponent,
        WorkflowComponent,
        WorkflowDetailComponent,
        WorkflowDetailedViewComponent,        
        WorkflowIssueDetailsComponent,
        WorkflowIssueEditorComponent,
        WorkflowFormComponent,        
        WorkflowRaiseIssueComponent,
        WorkflowSuggestDetailsComponent,
        WorkflowSuggestEditorComponent,
        WorkflowViewDetailsComponent,
        WorkflowViewStatusComponent,
        WorkflowWorkItemComponent,        
        WorkflowNewDetailComponent,
    ],
    exports: [        
        WorkflowCertifyDetailsComponent,
        WorkflowCertifyEditorComponent,         
        WorkflowRaiseIssueComponent,
        WorkflowSuggestDetailsComponent,
        WorkflowSuggestEditorComponent,
        WorkflowWorkItemComponent,
        WorkflowDetailComponent,
        WorkflowComponent,
        WorkflowDetailedViewComponent,
        WorkflowIssueDetailsComponent,
        WorkflowIssueEditorComponent,
        WorkflowNewDetailComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class WorkflowModule { }