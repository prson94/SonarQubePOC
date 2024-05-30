import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from "@angular/core";
import { IOutputData } from "angular-split";
import { SidePanelService } from "../../../../services/side-panel.service";
import { ReadRole } from "../../../../models/security.model";

@Component({
	selector: "roles-sidepanel-wrapper",
	templateUrl: './roles-sidepanel-wrapper.html',
	styles: [`
		.main-panel {
			display: flex;
			flex- direction: column;
		}`]
})
export class RolesSidePanelWrapperComponent implements OnChanges {
	@Input() sidePanelStorageKey: string;
	@Input() selectedItem: ReadRole;
	@Output() onEditClick = new EventEmitter();

	sidePanelOpen = false;
	selectedForInfoPanel: unknown;

	constructor(public sidePanelService: SidePanelService) {
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes.selectedItem && changes.selectedItem.currentValue !== changes.selectedItem.previousValue) {
			this.selectedForInfoPanel = null;

		}
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

	//onResourceLinkClick($event) {
	//	this.selectedItem = null;
	//	this.selectedForInfoPanel = { AssetUid: $event.uid, Object: $event.type };
	//}

	get anySelectedItem(): unknown {
		if (this.selectedItem) {
			return this.selectedItem;
		}
		else {
			return this.selectedForInfoPanel;
		}
	}
}
