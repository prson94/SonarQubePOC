import { Component } from "@angular/core";
import { AssetTypeClass } from "../../../../models/asset.model";


@Component({
    selector: "d3s-configuration-asset-type-connector-labels-page",
    templateUrl: './configuration-asset-type-connector-labels-page.component.html'
})
export class ConfigurationAssetTypeConnectorLabelsPageComponent {
    assetTypeClass: AssetTypeClass = AssetTypeClass.DiagramAsset
}
