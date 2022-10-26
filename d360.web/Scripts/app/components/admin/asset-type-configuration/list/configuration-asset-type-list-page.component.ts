import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { AssetTypeClass } from "../../../../models/asset.model";


@Component({
    selector: "d3s-configuration-asset-type-list-page",
    templateUrl: './configuration-asset-type-list-page.component.html'
})
export class ConfigurationAssetTypeListPageComponent {
    assetTypeClass: AssetTypeClass;

    constructor(
        private route: ActivatedRoute) { }

    ngOnInit() {
        this.route.params.subscribe(params => {
            this.assetTypeClass = AssetTypeClass[params["typeClass"] as string];
        })
    }
}