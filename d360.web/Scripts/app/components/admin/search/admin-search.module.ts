import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";

import { TreeTableModule } from "primeng/treetable";
import { TooltipModule } from 'primeng/tooltip';
import { CoreModule } from "../../shared/core.module";
import { AdminSearchComponent } from "./admin-search.component";
import { AdminSearchRoutingModule } from "./admin-search.routes";
import { PopupMenuModule } from "../../shared/controls/popup-menu/popup-menu.component";
import { AdminSearchTreeTableDirective } from "./admin-search.table.directive";
import { AdminSearchCheckboxDirective } from "./admin-search.checkbox.directive";

@NgModule({
    imports: [
        CommonModule,
        AdminSearchRoutingModule,
        //primeng
        TreeTableModule,
        TooltipModule,
        //d3s                
        CoreModule,
		PopupMenuModule,
    ],
    declarations: [
        AdminSearchComponent,
		AdminSearchTreeTableDirective,
		AdminSearchCheckboxDirective,
    ],
	exports: [
		AdminSearchComponent
	],
    providers: []
})
export class AdminSearchModule { }