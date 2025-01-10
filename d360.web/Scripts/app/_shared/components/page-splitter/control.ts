import { Component, Input } from "@angular/core";
import { SidePanelService } from "../../../services/side-panel.service";
import { IOutputData } from "angular-split";

@Component({
	selector: 'page-splitter',
	templateUrl: './control.html'
})
export class PageSplitterComponent {
	@Input() storageKey: string;

	sidePanelOpen: boolean;

	constructor(protected sidePanelService: SidePanelService) { }

	expandPanel() {
		this.sidePanelService.setSidePanelState({ expanded: true });
	}

	getSidePanelWidth(): number {
		return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.storageKey);
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

	get selected() {
		return this.sidePanelService.sidePanelItem;
	}
}