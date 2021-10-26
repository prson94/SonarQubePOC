import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { HTTP_INTERCEPTORS, HttpClientModule } from "@angular/common/http";
import { GovernRequestInterceptor } from "../../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from "@angular/router";

import { AutoCompleteModule } from "primeng/autocomplete";
import { CalendarModule } from "primeng/calendar";
import { ButtonModule } from "primeng/button";
import { InputMaskModule } from "primeng/inputmask";
import { SharedModule } from "primeng/api";
import { EditorModule } from "primeng/editor";
import { ToggleButtonModule } from "primeng/togglebutton";
import { MultiSelectModule } from "primeng/multiselect";
import { TableModule } from "primeng/table";

import { CoreModule } from "../../core.module";
import { TilesModule } from "../../tiles/tiles.module";
import { D3SOverlayWindowModule } from "../../overlay-window.component";
import { D3SEditorHeaderModule } from "../../editor-header.component";
import { SharedGridPagingInfoModule } from "../../grid-paging-info.component";
import { PopupMenuModule } from "../../controls/popup-menu/popup-menu.component";

import { WorkflowDiagramComponent } from "./workflow-diagram.component";
import { WorkflowStepEditorComponent } from "./workflow-step-editor.component";
import { WorkflowTransitionEditorComponent } from "./workflow-transition-editor.component";
import { WorkflowConditionEditorComponent } from "./workflow-condition-editor.component";
import { WorkflowStepFormEditorComponent } from "./workflow-step-form-editor.component";
import { WorkflowConditionListComponent } from "./workflow-condition-list.component";
import { WorkflowTemplateToolComponent } from "./workflow-template-tool.component";
import { WorkflowHistoryComponent } from "./workflow-history.component";
import { WorkflowFormHistoryComponent } from "./workflow-form-history.component";
import { WorkflowStepFieldChangeComponent } from "./workflow-step-field-change.component";
import { WorkflowStepSummaryComponent } from "./workflow-step-summary.component";
import { WorkflowTransitionSummaryComponent } from "./workflow-transition-summary.component";
import { WorkflowResponsibilitySelectorComponent } from "./workflow-responsibility-selector.component";
import { WorkflowStepHttpComponent } from "./workflow-step-http.component";
import { WorkflowStepHttpResponseComponent } from "./workflow-step-http-response.component";
import { PipesModule } from "../../../../pipes/pipes.module";


import { WorkflowFieldsService } from "../../../../services/workflow-fields.service";
import { DirectivesModule } from "../../../../directives/directives.module";
import { CheckboxModule } from "primeng/checkbox";


@NgModule({
    imports: [
        CommonModule,        
        FormsModule,
        HttpClientModule,
        RouterModule,

        //d3s
        CoreModule,
        TilesModule,
        D3SOverlayWindowModule,
        D3SEditorHeaderModule,
        SharedGridPagingInfoModule,
        ToggleButtonModule,
        DirectivesModule,
        PopupMenuModule,

        //prime        
        EditorModule,
        SharedModule,
        ButtonModule,
        CalendarModule,
        InputMaskModule,
        AutoCompleteModule,
        MultiSelectModule,
        TableModule,
        PipesModule,
        CheckboxModule
    ],
    declarations: [
        WorkflowDiagramComponent,
        WorkflowStepEditorComponent,
        WorkflowTransitionEditorComponent,
        WorkflowConditionEditorComponent,
        WorkflowStepFormEditorComponent,
        WorkflowConditionListComponent,
        WorkflowTemplateToolComponent,
        WorkflowHistoryComponent,
        WorkflowFormHistoryComponent,
        WorkflowStepFieldChangeComponent,
        WorkflowStepSummaryComponent,
        WorkflowStepHttpComponent,
        WorkflowStepHttpResponseComponent,
        WorkflowTransitionSummaryComponent,
        WorkflowResponsibilitySelectorComponent,
    ],
    exports: [
        WorkflowDiagramComponent,
        WorkflowConditionEditorComponent,
        WorkflowConditionListComponent,
        WorkflowTemplateToolComponent,
        WorkflowHistoryComponent,
        WorkflowFormHistoryComponent,
        WorkflowStepFieldChangeComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
        WorkflowFieldsService
    ]
})
export class WorkflowDiagramModule { }