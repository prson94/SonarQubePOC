import { NgModule }       from "@angular/core";
import { CommonModule }       from "@angular/common";
import { FormsModule }    from "@angular/forms";

import { CoreModule } from "../../shared/core.module";
import { PipesModule } from "../../../pipes/pipes.module";
import { TilesModule  } from "../../shared/tiles/tiles.module";
import { SharedGridPagingInfoModule } from "../../shared/grid-paging-info.component";
import { SharedDeleteFormModule } from "../../shared/delete.form";
import { SharedResponsibilitiesModule } from "../../shared/responsibilities/shared-responsibilities.module";
import { SharedObjectDetailsModule } from "../../shared/objectdetails/shared-object-details.module";
import { AdminModule } from "../admin.module";

import { AdminGovernanceComponent } from "./admin-governance.component";
import { ResponsibilityTypeForm } from "./responsibility-type.form";
import { ResponsibilityRulesComponent } from "./responsibility-rules.component";

import { ResponsibilityRuleForm } from "./responsibility-rule.form";

import { AdminResponsibilitiesRoutingModule } from "./admin-responsibilities.routes";

import { SimpleAccordionModule } from "../../shared/simple-accordion.part";

import { SharedModule } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { InputTextModule } from "primeng/inputtext";
import { CheckboxModule } from "primeng/checkbox";
import { DropdownModule } from "primeng/dropdown";
import { EditorModule } from "primeng/editor";
import { MultiSelectModule } from "primeng/multiselect";
import { RadioButtonModule } from 'primeng/radiobutton';
import { TooltipModule } from "primeng/tooltip";
import { TableModule } from "primeng/table";

import { DirectivesModule } from "../../../directives/directives.module";

@NgModule({
    imports: [
        CommonModule,
        FormsModule,


        AdminResponsibilitiesRoutingModule,

        //prime
        ButtonModule,
        CheckboxModule,
        DropdownModule,
        EditorModule,
        InputTextModule,
        MultiSelectModule,
        RadioButtonModule,
        SharedModule,
        TooltipModule,
        TableModule,

        //d3s        
        CoreModule,
        PipesModule,
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedObjectDetailsModule,
        SharedResponsibilitiesModule,
        DirectivesModule,
        SimpleAccordionModule,
        TilesModule,
        AdminModule,
    ],
    declarations: [
        AdminGovernanceComponent,
        ResponsibilityTypeForm,
        ResponsibilityRulesComponent,
        ResponsibilityRuleForm
    ],
    providers: [
    ]
})
export class AdminResponsibilitiesModule { }