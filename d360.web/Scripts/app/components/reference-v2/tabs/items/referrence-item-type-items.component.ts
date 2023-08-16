import { Component, OnInit } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { AssetTypeService } from "../../../../services/asset-type.service";


@Component({
	selector: "d3s-reference-item-type-items",
	templateUrl: './reference-item-type-items.component.html'
})
export class ReferenceItemTypeItemsComponent implements OnInit {
	uid: string;
	name: string;

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
}
