import { Component, Input } from "@angular/core";
import { AngularSplitModule, IOutputData } from "angular-split";
import { AssetOwnerModel } from "../../../../models/security.model";
import { SidePanelService } from "../../../../services/side-panel.service";
import { OwnerDetail } from "./owner-detail";
import { SidePanelModule } from "../../../../components/shared/sidepanel/side-panel.module";

@Component({
	selector: "owners-sidepanel-wrapper",
	templateUrl: './owners-sidepanel-wrapper.html',
	standalone: true,
	imports: [
		AngularSplitModule,
		OwnerDetail,
		SidePanelModule
	],
	styles: [`
		.main-panel {
			display: flex;
			flex-direction: column;
		}`]
})
export class OwnersSidePanelWrapper {
	@Input() assetUid: string;
	@Input() sidePanelStorageKey: string;
	@Input() selectedItem: AssetOwnerModel;

	sidePanelOpen = false;

	constructor(protected sidePanelService: SidePanelService) {

	}

	getSidePanelWidth(): number {
		return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
	}

	getSidePanelMaxWidth(): number {
		return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
	}

	getSidePanelMinWidth(): number {
		return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
	}

	onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
		this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
	}
}
