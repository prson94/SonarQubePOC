import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { AssetTypeClass } from "../../../models/asset.model";


@Component({
    selector: "d3s-stub-page",
    template: `
        <d3s-configuration-asset-type-header [assetTypeClass]="assetTypeClass"
            [uid]="uid">
        </d3s-configuration-asset-type-header>
        <d3s-configuration-asset-type-tabs [assetTypeClass]="assetTypeClass"
            [uid]="uid">
        </d3s-configuration-asset-type-tabs>

        Stub
    `
})
export class StubComponent {
    assetTypeClass: AssetTypeClass;
    uid: string;

    constructor(
        private route: ActivatedRoute) {
    }

    ngOnInit() {
        this.route.params.subscribe(params => {
            this.assetTypeClass = AssetTypeClass[params["typeClass"] as string];
            this.uid = params["uid"];
        });
    }
}