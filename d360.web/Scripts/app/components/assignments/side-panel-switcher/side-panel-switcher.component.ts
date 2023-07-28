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
	@Output() closeClick: EventEmitter<void> = new EventEmitter();
	@Input() showPanelHeader: boolean = true;

	clear(): void {
		this.selectedTag = null;
		this.selectedReferenceItem = null;
		this.selectedAsset = null;
		this.selection = null;
	}

	get panelHeader(): string {
		switch (this.sidePanelTab) {
			case AssetDetailClickType.WorkflowStep:
				return $localize`Step Information`;
			case AssetDetailClickType.WorkflowTypeInformation:
				return $localize`Workflow Information`;
			case AssetDetailClickType.WorkflowItemInformation:
				return $localize`Assignment Information`;
			case 'detail':
				if (this.selectedAsset && this.selectedAsset.type === 'Resource') {
					return $localize`User Information`;
				} else {
					return $localize`Asset Information`;
				}
			default:
				return $localize`Information`;
		}
	}

	protected readonly AssetDetailClickType = AssetDetailClickType;
}
