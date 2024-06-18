import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { TableModule } from "primeng/table";
import { TooltipModule } from "primeng/tooltip";
import { CoreModule } from "../../shared/core.module";

import { Policies } from "./policies";
import { Roles } from "./roles";

import { PolicyDelete } from "./policies/policy-delete";
import { PolicyDetail } from "./policies/policy-detail";
import { PolicyEditor } from "./policies/policy-editor";
import { PolicyList } from "./policies/policy-list";
import { PoliciesSidePanelWrapperComponent } from "./policies/policies-sidepanel-wrapper";

import { RoleDelete } from "./roles/role-delete";
import { RoleDetail } from "./roles/role-detail";
import { RoleEditor } from "./roles/role-editor";
import { RoleList } from "./roles/role-list";
import { RolesSidePanelWrapperComponent } from "./roles/roles-sidepanel-wrapper";

import { AngularSplitModule } from "angular-split";
import { SharedModule } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CheckboxModule } from "primeng/checkbox";
import { DropdownModule } from "primeng/dropdown";
import { InputTextModule } from "primeng/inputtext";
import { FormFeedbackBadgesModule } from "../../shared/controls/form-feedback-badges/form-feedback-badges.component";
import { IgMessageBoxModule } from "../../shared/controls/message-box/message-box.module";
import { PopupMenuModule } from "../../shared/controls/popup-menu/popup-menu.component";
import { PropertyGroupModule } from "../../shared/controls/property-group/property-group.component";
import { SearchFieldModule } from "../../shared/controls/search-field/search-field.component";
import { SharedDynamicGridEditorModule } from "../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module";
import { SharedGridPagingInfoModule } from "../../shared/grid-paging-info.component";
import { SiteModalModule } from "../../shared/modal/gov-modal.module";
import { SidePanelModule } from "../../shared/sidepanel/side-panel.module";
import { TilesModule } from "../../shared/tiles/tiles.module";
import { AdminSecurityRoutingModule } from "./security.routes";
import { RadioButtonModule } from "primeng/radiobutton";

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

		// prime
		ButtonModule,
		CheckboxModule,
		DropdownModule,
		InputTextModule,
		RadioButtonModule,
		SharedModule,
        TableModule,
		TooltipModule,

		CoreModule,
		SharedDynamicGridEditorModule,
		SharedGridPagingInfoModule,
		TilesModule,
		SearchFieldModule,
		PopupMenuModule,
		SiteModalModule,
		IgMessageBoxModule,
		PropertyGroupModule,
		FormFeedbackBadgesModule,
		ReactiveFormsModule,
		SidePanelModule,
		AngularSplitModule,
		AdminSecurityRoutingModule
    ],
	declarations: [
		Policies,
		PolicyDetail,
		PolicyDelete,
		PolicyEditor,
		PolicyList,
		PoliciesSidePanelWrapperComponent,

		Roles,
		RoleDetail,
		RoleDelete,
		RoleEditor,
		RoleList,
		RolesSidePanelWrapperComponent
    ],
    providers: [
	],
	exports: [
		Policies,
		Roles
	]
})
export class AdminSecurityModule { }