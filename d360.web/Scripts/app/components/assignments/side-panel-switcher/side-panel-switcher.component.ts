import { Component } from '@angular/core';
import { NodeModel } from '../../../models/workflow.model';
import { AssetDetailClickType } from '../../../services/href-click-service';

@Component({
	selector: 'd3s-side-panel-switcher',
	templateUrl: './side-panel-switcher.component.html',
	styleUrls: ['./side-panel-switcher.component.less']
})
export class SidePanelSwitcherComponent {
	selectedTag: { selectedTag: string, uid: string };
	selectedReferenceItem: { url: string, assetUid: string, uid: string };
	selectedAsset: {
		selectedNodeModel: NodeModel;
		workflowTypeVersion: number;
		workflowTypeUid: string,
		workflowItemUid: string,
		itemStepUid: string,
		id: number,
		uid: string,
		type: string
	};
	sidePanelTab: string;
	dataProfile: object;
	selection: { HasProfiling: boolean, AssetUid: string };
	assetGrid: { triggerEdit: (event: { assetUid: string, type: string, assetTypeUid: string }) => void };

	constructor() {
	}

	secondaryPanelOpen(): void {

	}

	clear(): void {
		this.selectedTag = null;
		this.selectedReferenceItem = null;
		this.selectedAsset = null;
		this.selection = null;
	}

	protected readonly AssetDetailClickType = AssetDetailClickType;
}
