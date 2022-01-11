import { NgModule }       from "@angular/core";
import { CommonModule }       from "@angular/common";
import { FormsModule }    from "@angular/forms";
import { HTTP_INTERCEPTORS, HttpClientModule } from "@angular/common/http";


import { CoreModule } from "../../shared/core.module";
import { PipesModule } from "../../../pipes/pipes.module";
import { TilesModule  } from "../../shared/tiles/tiles.module";
import { SharedGridPagingInfoModule } from "../../shared/grid-paging-info.component";
import { SharedDeleteFormModule } from "../../shared/delete.form";
import { SharedObjectDetailsModule } from "../../shared/objectdetails/shared-object-details.module";
import { SharedFieldDefinitionModule } from "../../shared/fielddefinition/shared-field-definition.module"; 
import { SharedDynamicGridEditorModule } from "../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module";
import { SharedResponsibilitiesModule } from "../../shared/responsibilities/shared-responsibilities.module";
import { AdminModule } from "../admin.module";

import { AdminRulesComponent } from "./admin-rules.component";

import { AdminRulesRoutingModule } from "./admin-rules.routes";

import { SharedModule } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { InputTextModule } from "primeng/inputtext";
import { DropdownModule } from "primeng/dropdown";
import { TableModule } from "primeng/table";
import { SharedAssetTypeEditorModule } from "../../shared/assettypeeditor/shared-asset-type-editor.module";
import { AssetTypeDeleteModule } from "../asset-type-delete/asset-type-delete.module";

@NgModule({
    imports: [
        CommonModule,
        FormsModule,


        AdminRulesRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        TableModule,

        //d3s        
        CoreModule,
        PipesModule,
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,    
        SharedObjectDetailsModule,
        SharedFieldDefinitionModule,
        SharedDynamicGridEditorModule,
        SharedResponsibilitiesModule,    
        TilesModule,
        AdminModule,
        AssetTypeDeleteModule,
        SharedAssetTypeEditorModule,

    ],
    declarations: [
        AdminRulesComponent
    ],
    providers: [
    ]
})
export class AdminRulesModule { }