import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from "@angular/core";
import { IOutputData } from "angular-split";
import { SidePanelService } from "../../../../services/side-panel.service";


@Component({
	selector: "d3s-connector-label-sidepanel-wrapper",
	templateUrl: './connector-label-sidepanel-wrapper.component.html',
	styleUrls: ['./connector-label-sidepanel-wrapper.component.less']
})
export class ConnectorLabelSidePanelWrapperComponent implements OnChanges {
	@Input() sidePanelStorageKey: string;
	@Input() selectedItem: string;
	@Output() onEdit = new EventEmitter();

	sidePanelOpen = false;
	selectedForInfoPanel: any;

	constructor(public sidePanelService: SidePanelService) {
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes.selectedItem && changes.selectedItem.currentValue !== changes.selectedItem.previousValue) {
			this.selectedForInfoPanel = null;
		}
	}

	onResourceLinkClick($event) {
		this.selectedItem = null;
		this.selectedForInfoPanel = { AssetUid: $event.uid, Object: $event.type };
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
