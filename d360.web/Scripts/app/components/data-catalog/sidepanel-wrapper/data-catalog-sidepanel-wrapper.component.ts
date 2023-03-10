import { Component, Input, OnChanges, OnDestroy, SimpleChanges } from "@angular/core";
import { IOutputData } from "angular-split";
import { Subscription } from "rxjs";
import { LinkClickInterceptor } from "../../../services/href-click-service";
import { SidePanelService } from "../../../services/side-panel.service";

@Component({
	selector: "d3s-data-catalog-sidepanel-wrapper",
	templateUrl: './data-catalog-sidepanel-wrapper.component.html',
	styleUrls: ['./data-catalog-sidepanel-wrapper.component.less']
})
export class DataCatalogSidePanelWrapperComponent implements OnChanges, OnDestroy {
	@Input() sidePanelStorageKey: string;

	sidePanelOpen = false;

	selectedAsset: Record<string, unknown>;

	hrefSub: Subscription;

	constructor(public sidePanelService: SidePanelService) {
		this.sidePanelService.sidePanelStateChange$.subscribe((res) => {
			if (res.assetUid) {
				this.selectedAsset = { uid: res.assetUid, type: 'Artifact' };
			}
		});
	}
	ngOnDestroy(): void {
		if (this.hrefSub) {
			this.hrefSub.unsubscribe();
		}
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes.selectedItem && changes.selectedItem.currentValue !== changes.selectedItem.previousValue) {
			this.selectedAsset = null;
		}
	}

	onResourceLinkClick($event) {
		this.selectedAsset = { uid: $event.uid, type: $event.type };
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
		return this.selectedAsset;
	}
}
