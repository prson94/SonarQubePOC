import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";

import { TreeTableModule } from "primeng/treetable";
import { TooltipModule } from 'primeng/tooltip';
import { CoreModule } from "../../../shared/core.module";
import { PopupMenuModule } from "../../../shared/controls/popup-menu/popup-menu.component";
import { RaiseIssueComponent } from "./raise-issue.component";
import { ActionModalFormModule } from "./action-form/action-modal-form.module";

@NgModule({
	imports: [
		CommonModule,
		//primeng
		TreeTableModule,
		TooltipModule,
		//d3s                
		CoreModule,
		PopupMenuModule,
		ActionModalFormModule
	],
	declarations: [
		RaiseIssueComponent
	],
	exports: [
		RaiseIssueComponent
	],
	providers: []
})
export class RaiseIssueModule { }