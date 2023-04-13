import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { AssetTypeClass } from "../../../../../models/asset.model";
import { AssetTypeService } from "../../../../../services/asset-type.service";


@Component({
    selector: "d3s-configuration-asset-type-details-page",
	templateUrl: './configuration-asset-type-details-page.component.html'
})
export class ConfigurationAssetTypeDetailsPageComponent {
    assetTypeClass: AssetTypeClass;
    uid: string;
    assetType: { Name: string };

    constructor(
        private route: ActivatedRoute,
        private assetTypeService: AssetTypeService) {
    }

    ngOnInit() {
        this.route.params.subscribe((params) => {
            this.assetTypeClass = AssetTypeClass[params["typeClass"] as string];
            this.uid = params["uid"];
        });
    }
}
