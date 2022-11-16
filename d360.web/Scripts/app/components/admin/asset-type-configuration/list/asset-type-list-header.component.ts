import { Component, Input } from "@angular/core";
import { AssetTypeClass } from "../../../../models/asset.model";
import { typeClassToHeaderSettings } from "../shared/typeClassToHeaderSettings";


@Component({
    selector: "d3s-asset-type-list-header",
    templateUrl: './asset-type-list-header.component.html',
    styleUrls: ['./asset-type-list-header.component.less']
})
export class AssetTypeListHeaderComponent {
    @Input() assetTypeClass: AssetTypeClass;
    get icon() {
        return this.settings.icon;
    }

    get header() {
        return this.settings.title;
    }

    get settings() {
        const settings = this.typeClassToSettings.get(this.assetTypeClass);
        if (!settings) {
            throw new Error(`Failed to find settings for asset type class ${this.assetTypeClass} (${AssetTypeClass[this.assetTypeClass]})`);
        }

        return settings;
    }

    typeClassToSettings = typeClassToHeaderSettings;
}