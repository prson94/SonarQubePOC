import { Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from "@angular/core";
import { IOutputData } from "angular-split";
import { TreeNode } from "primeng/api";
import { Subscription } from "rxjs";
import { LinkClickInterceptor } from "../../../../services/href-click-service";
import { SidePanelService } from "../../../../services/side-panel.service";
import { SidePanelComponent } from "../../../shared/sidepanel/side-panel.component";


@Component({
    selector: "d3s-asset-type-list-sidepanel-wrapper",
    templateUrl: './asset-type-list-sidepanel-wrapper.component.html',
    styleUrls: ['./asset-type-list-sidepanel-wrapper.component.less']
})
export class AssetTypeListSidePanelWrapperComponent implements OnDestroy, OnChanges {
    @Input() sidePanelStorageKey: string;
	@Input() selectedItem: TreeNode;

	@Output() onEditClick: EventEmitter<string> = new EventEmitter<string>();

	sidePanelOpen = false;

	hrefSub: Subscription;
	selectedForInfoPanel: unknown;

	@ViewChild('sidePanel', { static: false }) sidePanel: SidePanelComponent;

	constructor(public sidePanelService: SidePanelService,
		private linkClickInterceptor: LinkClickInterceptor
	) {
		this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
			this.selectedItem = null;
			this.selectedForInfoPanel = { AssetUid: ev.data.uid, Object: ev.data.type };
		});
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes.selectedItem && changes.selectedItem.currentValue !== changes.selectedItem.previousValue) {
			this.selectedForInfoPanel = null;
		}
	}

	expandPanel() {
		this.sidePanel.expandSidePanel();
	}

	ngOnDestroy() {
		if (this.hrefSub) {
			this.hrefSub.unsubscribe();
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

	get anySelectedItem(): unknown {
		if (this.selectedItem) {
			return this.selectedItem;
		}
		else {
			return this.selectedForInfoPanel;
		}
	}
}
