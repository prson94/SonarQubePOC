import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { AssetTypeClass } from "../../../../../models/asset.model";

@Component({
    selector: "d3s-configuration-asset-type-owners-page",
    templateUrl: './configuration-asset-type-owners-page.component.html'
})
export class ConfigurationAssetTypeOwnersPageComponent {
    assetTypeClass: AssetTypeClass;
    uid: string;

    constructor(private route: ActivatedRoute) {
    }

    ngOnInit() {
        this.route.params.subscribe(params => {
            this.assetTypeClass = AssetTypeClass[params["typeClass"] as string];
            this.uid = params["uid"];
        });
    }
}