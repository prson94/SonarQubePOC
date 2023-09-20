import { Component, OnInit, OnDestroy } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { Subscription } from "rxjs";
import { AssetTypeApiModel } from "../../../../models/asset.model";
import { AssetTypeService } from "../../../../services/asset-type.service";

@Component({
	selector: "d3s-reference-item-type-fields",
	templateUrl: './reference-item-type-fields.component.html'
})
export class ReferenceItemTypeFieldsComponent implements OnInit, OnDestroy {
	uid: string;
	assetType: AssetTypeApiModel;
	objectName: string = null;
	subscription: Subscription;

	constructor(
		protected assetTypeService: AssetTypeService,
		private route: ActivatedRoute) {
	}

	ngOnInit() {
		this.route.params.subscribe((params) => {
			this.uid = params["uid"];
			this.load();
		});
	}

	private load() {
		this.subscription = this.assetTypeService.GetAssetTypeByUid(this.uid).subscribe((res) => {
			this.assetType = res;
		});
	}

	ngOnDestroy() {
		this.subscription?.unsubscribe();
	}
}
