import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { IOutputData } from "angular-split";
import { AssetTypeClass } from "../../../../../models/asset.model";
import { LinkClickInterceptor } from "../../../../../services/href-click-service";
import { SidePanelService } from "../../../../../services/side-panel.service";


@Component({
	selector: "d3s-configuration-asset-type-details-page",
	templateUrl: './configuration-asset-type-details-page.component.html'
})
export class ConfigurationAssetTypeDetailsPageComponent {
	assetTypeClass: AssetTypeClass;
	uid: string;
	assetType: { Name: string };

	sidePanelStorageKey: string = '';
	selectedItem: Record<string, object>;

	sidePanelOpen = false;
	selectedForInfoPanel: unknown;
	constructor(
		private route: ActivatedRoute,
		public sidePanelService: SidePanelService,
		private linkClickInterceptor: LinkClickInterceptor) {
		this.linkClickInterceptor.getEvents().subscribe((res) => {
			if (res && res.data) {
				this.selectedItem = res.data;
				this.sidePanelService.setSidePanelState({ expanded: true });
			}
		});
	}

	ngOnInit() {
		this.route.params.subscribe((params) => {
			this.assetTypeClass = AssetTypeClass[params["typeClass"] as string];
			this.uid = params["uid"];
			this.sidePanelStorageKey = "side_panel_asset_type_Details_" + this.uid;
		});
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
