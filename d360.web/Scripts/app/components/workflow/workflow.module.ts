import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';

import { WorkflowDetailComponent } from './workflow-detail.component';
import { WorkflowSuggestDetailsComponent } from './workflow-suggest-details.component';
import { WorkflowCertifyDetailsComponent } from './workflow-certify-details.component';
import { WorkflowCertifyEditorComponent } from './workflow-certify-editor.component';
import { WorkflowRaiseIssueComponent } from './workflow-raise-issue.component';
import { WorkflowSuggestEditorComponent } from './workflow-suggest-editor.component';
import { WorkflowViewStatusComponent } from './workflow-view-status.component';
import { WorkflowWorkItemComponent } from './workflow-work-item.component';


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
    PaginatorModule,
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

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
        PaginatorModule,
        EditorModule,
        AutoCompleteModule,
        SharedModule,

        //d3s
        D3SSharedModule,
        CoreModule,

    ],
    declarations: [
        WorkflowCertifyDetailsComponent,
        WorkflowCertifyEditorComponent,                 
        WorkflowRaiseIssueComponent,
        WorkflowSuggestDetailsComponent,
        WorkflowSuggestEditorComponent,
        WorkflowViewStatusComponent,
        WorkflowWorkItemComponent,
        WorkflowDetailComponent,
    ],
    exports: [
        WorkflowCertifyDetailsComponent,
        WorkflowCertifyEditorComponent,             
        WorkflowRaiseIssueComponent,
        WorkflowSuggestDetailsComponent,
        WorkflowSuggestEditorComponent,
        WorkflowWorkItemComponent,
        WorkflowDetailComponent,
    ]
})
export class WorkflowModule { }