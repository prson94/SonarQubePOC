import { Component } from '@angular/core';

@Component({
	selector: 'd3s-side-panel-switcher',
	templateUrl: './side-panel-switcher.component.html',
	styleUrls: ['./side-panel-switcher.component.less']
})
export class SidePanelSwitcherComponent {
	selectedTag: { selectedTag: string, uid: string };
	selectedReferenceItem: { url: string, assetUid: string, uid: string };
	selectedAsset: { workflowItemUid: string, itemStepUid: string, id: number, uid: string, type: string };
	sidePanelTab: string;
	dataProfile: object;
	selection: { HasProfiling: boolean, AssetUid: string };
	assetGrid: { triggerEdit: (event: { assetUid: string, type: string, assetTypeUid: string }) => void };
	isInitialized: boolean = false;

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
}
