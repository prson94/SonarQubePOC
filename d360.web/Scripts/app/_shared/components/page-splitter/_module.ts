import { NgModule } from "@angular/core";
import { AngularSplitModule } from 'angular-split';
import { PageSplitterComponent } from "./control";
import { SidePanelModule } from "../../../components/shared/sidepanel/side-panel.module";

@NgModule({
	imports: [
		AngularSplitModule,
		SidePanelModule
    ],
	declarations: [
		PageSplitterComponent
    ],
    exports: [
        PageSplitterComponent
    ]
})
export class PageSplitterModule { }