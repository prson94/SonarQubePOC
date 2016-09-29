import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { SharedModule } from '../shared/shared.module';

import { WorkflowDetailComponent } from './workflow-detail.component';
import { WorkflowSuggestDetailsComponent } from './workflow-suggest-details.component';
import { WorkflowCertifyDetailsComponent } from './workflow-certify-details.component';
import { WorkflowCertifyEditorComponent } from './workflow-certify-editor.component';
import { WorkflowRaiseIssueComponent } from './workflow-raise-issue.component';
import { WorkflowSuggestEditorComponent } from './workflow-suggest-editor.component';

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
    SpinnerModule,
    EditorModule,
    TooltipModule,
    DragDropModule,
    PaginatorModule,
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
        SpinnerModule,
        TooltipModule,
        PaginatorModule,
        EditorModule,
        AutoCompleteModule,

        //d3s
        SharedModule,
        CoreModule,

    ],
    declarations: [
        WorkflowCertifyDetailsComponent,
        WorkflowCertifyEditorComponent,
        WorkflowDetailComponent,                
        WorkflowRaiseIssueComponent,
        WorkflowSuggestDetailsComponent,
        WorkflowSuggestEditorComponent,
    ],
    exports: [
        WorkflowCertifyDetailsComponent,
        WorkflowCertifyEditorComponent,
        WorkflowDetailComponent,                
        WorkflowRaiseIssueComponent,
        WorkflowSuggestDetailsComponent,
        WorkflowSuggestEditorComponent,
    ]
})
export class WorkflowModule { }