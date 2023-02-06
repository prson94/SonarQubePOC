import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewChild } from "@angular/core";
import { IOutputData } from "angular-split";
import { WorkflowIssueType } from "../../../../models/workflow.model";
import { SidePanelService } from "../../../../services/side-panel.service";
import { SidePanelComponent } from "../../../shared/sidepanel/side-panel.component";


@Component({
	selector: "d3s-issuetype-sidepanel-wrapper",
	templateUrl: './issuetype-sidepanel-wrapper.component.html',
	styleUrls: ['./issuetype-sidepanel-wrapper.component.less']
})
export class IssueTypeSidePanelWrapperComponent implements OnChanges {
	@Input() sidePanelStorageKey: string;
	@Input() selectedItem: WorkflowIssueType;
	@Output() onEdit = new EventEmitter();

	sidePanelOpen = false;
	selectedForInfoPanel: unknown;

	@ViewChild('sidePanel', { static: false }) sidePanel: SidePanelComponent;

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

	expandPanel() {
		this.sidePanel.expandSidePanel();
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
