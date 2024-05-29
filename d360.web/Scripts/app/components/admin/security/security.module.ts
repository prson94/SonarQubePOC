import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { TableModule } from "primeng/table";
import { TooltipModule } from "primeng/tooltip";
import { PipesModule } from "../../../pipes/pipes.module";
import { CoreModule } from "../../shared/core.module";
//import { SharedDeleteFormModule } from "../../shared/delete.form";
//import { SharedDynamicGridEditorModule } from "../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module";
//import { SharedFieldDefinitionModule } from "../../shared/fielddefinition/shared-field-definition.module";
//import { SharedGridPagingInfoModule } from "../../shared/grid-paging-info.component";
//import { TilesModule } from "../../shared/tiles/tiles.module";
import { Roles } from "./roles";
import { AdminSecurityRoutingModule } from "./security.routes";
import { RolesSidePanelWrapperComponent } from "./roles-sidepanel-wrapper";
import { RoleDelete } from "./role-delete";
import { RoleEditor } from "./role-editor";
import { RoleList } from "./role-list";
import { FormFeedbackBadgesModule } from "../../shared/controls/form-feedback-badges/form-feedback-badges.component";
import { IgMessageBoxModule } from "../../shared/controls/message-box/message-box.module";
import { SiteModalModule } from "../../shared/modal/gov-modal.module";
import { RoleDetail } from "./role-detail";
import { SearchFieldModule } from "../../shared/controls/search-field/search-field.component";
import { AngularSplitModule } from "angular-split";
import { SidePanelModule } from "../../shared/sidepanel/side-panel.module";
//import { InputTextModule } from "primeng/inputtext";
//import { ButtonModule } from "primeng/button";
//import { SpinnerModule } from "primeng/spinner";
//import { SliderModule } from "primeng/slider";

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

		AdminSecurityRoutingModule,

        //prime
		//SharedModule,
//		ButtonModule,
//		SpinnerModule,
//		SliderModule,
        TooltipModule,
        TableModule,
//		InputTextModule,
		//		InputTextModule,
		AngularSplitModule,

		//d3s
        CoreModule,
		PipesModule,
		SiteModalModule,
		IgMessageBoxModule,
		SearchFieldModule,
		FormFeedbackBadgesModule,
		SidePanelModule,

//		SharedDeleteFormModule,
//		SharedDynamicGridEditorModule,
//		SharedFieldDefinitionModule,
//		SharedGridPagingInfoModule,
//		TilesModule
    ],
	declarations: [
		Roles,
		RolesSidePanelWrapperComponent,
		RoleDelete,
		RoleDetail,
		RoleEditor,
		RoleList,
    ],
    providers: [
	],
	exports: [
		Roles
	]
})
export class AdminSecurityModule { }