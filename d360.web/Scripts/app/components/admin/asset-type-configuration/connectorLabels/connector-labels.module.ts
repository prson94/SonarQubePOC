import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { RouterModule } from "@angular/router";
import { AngularSplitModule } from "angular-split";
import { SharedModule } from "primeng/api";
import { AutoCompleteModule } from "primeng/autocomplete";
import { ButtonModule } from "primeng/button";
import { DropdownModule } from "primeng/dropdown";
import { EditorModule } from "primeng/editor";
import { TableModule } from "primeng/table";
import { DirectivesModule } from "../../../../directives/directives.module";
import { ConnectorLabelDefinitionModule } from "../../../connector-label/definition/connector-label-definition.module";
import { AssetDetailModule } from "../../../shared/asset-detail/asset-detail.module";
import { AssetPreviewModule } from "../../../shared/asset-preview/asset-preview.module";
import { PopupMenuModule } from "../../../shared/controls/popup-menu/popup-menu.component";
import { PropertyGroupModule } from "../../../shared/controls/property-group/property-group.component";
import { SearchFieldModule } from "../../../shared/controls/search-field/search-field.component";
import { CoreModule } from "../../../shared/core.module";
import { SharedDeleteFormModule } from "../../../shared/delete.form";
import { SharedDynamicGridEditorModule } from "../../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module";
import { SharedGridPagingInfoModule } from "../../../shared/grid-paging-info.component";
import { SiteModalModule } from "../../../shared/modal/gov-modal.module";
import { SharedObjectDetailsModule } from "../../../shared/objectdetails/shared-object-details.module";
import { SidePanelModule } from "../../../shared/sidepanel/side-panel.module";
import { TilesModule } from "../../../shared/tiles/tiles.module";
import { WhereUsedModule } from "../../../shared/where-used/where-used.module";
import { ConnectorLabelFormModule } from "../../../sidebar/connector-labels/connector-label-form.module";
import { ConnectorLabelSidePanelWrapperComponent } from "./connector-label-sidepanel-wrapper.component";
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
		PopupMenuModule,
		SearchFieldModule,
		SidePanelModule,
		AngularSplitModule,
		PropertyGroupModule,
		ConnectorLabelDefinitionModule,
		AssetDetailModule,
		AssetPreviewModule
	],
    declarations: [
		ConnectorLabelsComponent,
		ConnectorLabelSidePanelWrapperComponent
    ],
    exports: [
		ConnectorLabelsComponent
    ],
    providers: [
    ]
})
export class ConnectorLabelsModule { }
