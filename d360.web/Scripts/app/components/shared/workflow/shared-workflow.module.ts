import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { HTTP_INTERCEPTORS, HttpClientModule } from "@angular/common/http";

import { RouterModule } from "@angular/router";
import { FormsModule } from "@angular/forms";


import { SharedModule } from "primeng/api";
import { ToastModule } from "primeng/toast";
import { CalendarModule } from "primeng/calendar";
import { InputTextModule } from "primeng/inputtext";
import { TableModule } from "primeng/table";
import { ButtonModule } from "primeng/button";
import { MultiSelectModule } from "primeng/multiselect";
import { DropdownModule } from "primeng/dropdown";
import { SelectButtonModule } from "primeng/selectbutton";
import { TooltipModule } from "primeng/tooltip";
import { PipesModule } from "../../../pipes/pipes.module";

import { CoreModule } from "../core.module";
import { TilesModule } from "../tiles/tiles.module";
import { SharedGridPagingInfoModule } from "../grid-paging-info.component";
import { SharedObjectDetailsModule } from "../objectdetails/shared-object-details.module";
import { D3SSharedModule } from "../shared.module";
import { SimpleAccordionModule } from "../simple-accordion.part";
import { WorkflowMonitorStepListComponent } from "./workflowmonitor-step-list.component";
import { WorkflowMonitorStepGridComponent } from "./workflowmonitor-step-grid.component";
import { WorkflowMonitorStepDetailsComponent } from "./workflowmonitor-step-details.component";
import { WorkflowMonitorStepFormDetailsComponent } from "./workflowmonitor-step-form-details.component";
import { WorkflowMonitorStepEmailDetailsComponent } from "./workflowmonitor-step-email-details.component";
import { WorkflowMonitorActionDetailsComponent } from "./workflowmonitor-action-details.component";
import { WorkflowMonitorStepFieldChangeDetailsComponent } from "./workflowmonitor-step-field-change-details.component";
import { WorkflowMonitorStepRelationshipChangeDetailsComponent } from "./workflowmonitor-step-relationship-change-details";
import { WorkflowMonitorStepHttpDetailsComponent } from "./workflowmonitor-step-http-details.component";
import { WorkflowMonitorStepHttpResponseDetailsComponent } from "./workflowmonitor-step-http-response-details.component";


@NgModule({
    imports: [
        CommonModule,

        RouterModule,
        FormsModule,
        PipesModule,

        //primeng
        ToastModule,
        SharedModule,
        MultiSelectModule,
        DropdownModule,
        SelectButtonModule,
        InputTextModule,
        TooltipModule,
        ButtonModule,
        CalendarModule,
        TableModule,

        //d3s        
        CoreModule,
        TilesModule,
        SharedGridPagingInfoModule,        
        SharedObjectDetailsModule,
        D3SSharedModule,
        SimpleAccordionModule,
    ],
    declarations: [
        WorkflowMonitorActionDetailsComponent,
        WorkflowMonitorStepGridComponent,
        WorkflowMonitorStepListComponent,
        WorkflowMonitorStepDetailsComponent,
        WorkflowMonitorStepFormDetailsComponent,
        WorkflowMonitorStepEmailDetailsComponent,
        WorkflowMonitorStepFieldChangeDetailsComponent,
        WorkflowMonitorStepRelationshipChangeDetailsComponent,
        WorkflowMonitorStepHttpDetailsComponent,
        WorkflowMonitorStepHttpResponseDetailsComponent,
      
    ],
    exports: [
        WorkflowMonitorStepGridComponent,
        WorkflowMonitorStepListComponent,
        WorkflowMonitorStepDetailsComponent,
        WorkflowMonitorActionDetailsComponent,
    ],
    providers: [

    ]
})
export class SharedWorkflowMonitorModule { }
