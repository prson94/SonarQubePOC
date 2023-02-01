import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from "@angular/core";
import { IOutputData } from "angular-split";
import { TreeNode } from "primeng/api";
import { SidePanelService } from "../../../../services/side-panel.service";


@Component({
	selector: "d3s-admin-relationshipst-sidepanel-wrapper",
	templateUrl: './admin-relationships-sidepanel-wrapper.component.html',
	styleUrls: ['./admin-relationships-sidepanel-wrapper.component.less']
})
export class AdminRelationshipsSidePanelWrapperComponent implements OnChanges {
	@Input() sidePanelStorageKey: string;
	@Input() selectedItem: TreeNode;
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

	onResourceLinkClick($event) {
		this.selectedItem = null;
		this.selectedForInfoPanel = { AssetUid: $event.uid, Object: $event.type };
	}

	get anySelectedItem(): unknown {
		if (this.selectedItem) {
			return this.selectedItem;
		}
		else {
			return this.selectedForInfoPanel;
		}
	}
}
