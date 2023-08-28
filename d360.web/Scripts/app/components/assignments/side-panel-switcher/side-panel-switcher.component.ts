import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NodeModel } from '../../../models/workflow.model';
import { AssetDetailClickType } from '../../../services/href-click-service';

/*global $localize*/

@Component({
	selector: 'd3s-side-panel-switcher',
	templateUrl: './side-panel-switcher.component.html',
	styleUrls: ['side-panel-switcher.component.less']
})
export class SidePanelSwitcherComponent {
	selectedTag: { selectedTag: string, uid: string };
	selectedReferenceItem: { url: string, assetUid: string, uid: string };
	_selectedAsset: {
		selectedNodeModel: NodeModel;
		workflowTypeVersion: number;
		workflowTypeUid: string,
		workflowActionUid: string,
		workflowItemUid: string,
		itemStepUid: string,
		showCompleteAssignment: boolean,
		id: number,
		uid: string,
		type: string
	};
	_sidePanelTab: string;
	panelHeader: string;

	set sidePanelTab(value: string) {
		this._sidePanelTab = value;
		this.panelHeader = this.getPanelHeader();
		this.panelHeaderLabel.emit(this.panelHeader);
	}

	set selectedAsset(value: {
		selectedNodeModel: NodeModel;
		workflowTypeVersion: number;
		workflowTypeUid: string,
		workflowActionUid: string,
		workflowItemUid: string,
		itemStepUid: string,
		showCompleteAssignment: boolean,
		id: number,
		uid: string,
		type: string
	}) {
		this._selectedAsset = value;
		this.panelHeader = this.getPanelHeader();
		this.panelHeaderLabel.emit(this.panelHeader);
	}

	dataProfile: object;
	selection: { HasProfiling: boolean, AssetUid: string };
	assetGrid: { triggerEdit: (event: { assetUid: string, type: string, assetTypeUid: string }) => void };
	@Output() closeClick: EventEmitter<void> = new EventEmitter();
	@Output() panelHeaderLabel: EventEmitter<string> = new EventEmitter();
	@Input() showPanelHeader: boolean = true;
	@Input() outsideModal: boolean = true;

	clear(): void {
		this.selectedTag = null;
		this.selectedReferenceItem = null;
		this._selectedAsset = null;
		this.selection = null;
	}

	getPanelHeader(): string {
		switch (this._sidePanelTab) {
			case AssetDetailClickType.WorkflowStep:
				return $localize`Step Information`;
			case AssetDetailClickType.WorkflowTypeInformation:
				return $localize`Workflow Information`;
			case AssetDetailClickType.WorkflowItemInformation:
				return $localize`Assignment Information`;
			case 'detail':
				if (this._selectedAsset?.type === 'Resource') {
					return $localize`User Information`;
				} else if (this._selectedAsset?.type === 'Artifact') {
					return $localize`Asset Information`;
				} else {
					return $localize`Information`;
				}
			default:
				return $localize`Information`;
		}
	}

	protected readonly AssetDetailClickType = AssetDetailClickType;
}
