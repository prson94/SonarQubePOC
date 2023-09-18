import { Component, OnInit, ViewChild } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { AssetTypeService } from "../../../../services/asset-type.service";
import { ReferenceItemTypeTabsComponent } from "../shared/reference-tabs.component";


@Component({
	selector: "d3s-reference-item-type-items",
	templateUrl: './reference-item-type-items.component.html'
})
export class ReferenceItemTypeItemsComponent implements OnInit {
	uid: string;
	name: string;

	@ViewChild("tabs", { static: false }) tabs: ReferenceItemTypeTabsComponent;

	constructor(
		private route: ActivatedRoute,
		private assetTypeService: AssetTypeService) {
	}

	ngOnInit() {
		this.route.params.subscribe((params) => {
			this.uid = params["uid"];
			this.load();
		});
	}

	public load(): void {
		this.assetTypeService.GetAssetTypeByUid(this.uid).subscribe((res) => {
			this.name = res.Name;
		});
	}

	updateItemCount(count: number) {
		this.tabs.updateItemCount(count);
	}
}
