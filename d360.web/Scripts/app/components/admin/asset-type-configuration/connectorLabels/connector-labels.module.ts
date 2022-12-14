import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { RouterModule } from "@angular/router";
import { SharedModule } from "primeng/api";
import { AutoCompleteModule } from "primeng/autocomplete";
import { ButtonModule } from "primeng/button";
import { DropdownModule } from "primeng/dropdown";
import { EditorModule } from "primeng/editor";
import { TableModule } from "primeng/table";
import { DirectivesModule } from "../../../../directives/directives.module";
import { PopupMenuModule } from "../../../shared/controls/popup-menu/popup-menu.component";
import { CoreModule } from "../../../shared/core.module";
import { SharedDeleteFormModule } from "../../../shared/delete.form";
import { SharedDynamicGridEditorModule } from "../../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module";
import { SharedGridPagingInfoModule } from "../../../shared/grid-paging-info.component";
import { SiteModalModule } from "../../../shared/modal/gov-modal.module";
import { SharedObjectDetailsModule } from "../../../shared/objectdetails/shared-object-details.module";
import { TilesModule } from "../../../shared/tiles/tiles.module";
import { WhereUsedModule } from "../../../shared/where-used/where-used.module";
import { ConnectorLabelFormModule } from "../../../sidebar/connector-labels/connector-label-form.module";
import { ConnectorLabelsComponent } from "./connector-labels.component";

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,

        RouterModule,

        //d3s        
        CoreModule,
        SharedGridPagingInfoModule,
        TilesModule,
        DirectivesModule,

        //prime     
        EditorModule,
        DropdownModule,
        ButtonModule,
        SharedModule,
        TableModule,
        CoreModule,
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,
        SharedObjectDetailsModule,
        SharedGridPagingInfoModule,
        TilesModule,
        SiteModalModule,
        WhereUsedModule,
        AutoCompleteModule,

        ConnectorLabelFormModule,
        PopupMenuModule
    ],
    declarations: [
        ConnectorLabelsComponent
    ],
    exports: [
        ConnectorLabelsComponent
    ],
    providers: [
    ]
})
export class ConnectorLabelsModule { }
