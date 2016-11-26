import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';

import { AssignmentsTile } from './assignments-tile.component';
import { WorkflowComponent } from './workflow.component';
import { WorkflowDetailComponent } from './workflow-detail.component';
import { WorkflowSuggestDetailsComponent } from './workflow-suggest-details.component';
import { WorkflowCertifyDetailsComponent } from './workflow-certify-details.component';
import { WorkflowCertifyEditorComponent } from './workflow-certify-editor.component';
import { WorkflowRaiseIssueComponent } from './workflow-raise-issue.component';
import { WorkflowSuggestEditorComponent } from './workflow-suggest-editor.component';
import { WorkflowViewStatusComponent } from './workflow-view-status.component';
import { WorkflowWorkItemComponent } from './workflow-work-item.component';
import { WorkflowDetailedViewComponent } from './workflow-detailed-view.component';
import { WorkflowIssueDetailsComponent } from './workflow-issue-details.component';
import { WorkflowIssueEditorComponent } from './workflow-issue-editor.component';

import { WorkflowRoutingModule } from './workflow.routes';

import {
    GrowlModule,
    InputTextModule,
    InputMaskModule,
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    CalendarModule,
    MenuModule,
    MenubarModule,
    AccordionModule,
    SelectButtonModule,
    AutoCompleteModule,
    MultiSelectModule,    
    EditorModule,
    TooltipModule,        
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        WorkflowRoutingModule,
        //primeng  
        GrowlModule,
        InputTextModule,
        InputMaskModule,
        DataTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,
        MenuModule,
        MenubarModule,
        AccordionModule,
        SelectButtonModule,
        MultiSelectModule,        
        TooltipModule,        
        EditorModule,
        AutoCompleteModule,
        SharedModule,

        //d3s
        
        CoreModule,
        TilesModule,
        SharedGridPagingInfoModule,
    ],
    declarations: [
        AssignmentsTile,
        WorkflowCertifyDetailsComponent,
        WorkflowCertifyEditorComponent,                 
        WorkflowRaiseIssueComponent,
        WorkflowSuggestDetailsComponent,
        WorkflowSuggestEditorComponent,
        WorkflowViewStatusComponent,
        WorkflowWorkItemComponent,
        WorkflowDetailComponent,
        WorkflowComponent,
        WorkflowDetailedViewComponent,
        WorkflowIssueDetailsComponent,
        WorkflowIssueEditorComponent,
    ],
    exports: [
        AssignmentsTile,
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
    ]
})
export class WorkflowModule { }