import { Component, Input } from "@angular/core";
import { AssetTypeClass } from "../../../../../../models/asset.model";
import { AssetTypeService } from "../../../../../../services/asset-type.service";
import { typeClassToHeaderSettings } from "../../../shared/typeClassToHeaderSettings";


@Component({
    selector: "d3s-configuration-asset-type-header",
    templateUrl: './asset-type-header.component.html',
    styleUrls: ['./asset-type-header.component.less']
})
export class ConfigurationAssetTypeHeaderComponent {
    @Input() assetTypeClass: AssetTypeClass;
    @Input() uid: string;

    assetType: { Name: string };

    constructor(private assetTypeService: AssetTypeService) {
    }

    get icon() {
        return this.settings.icon;
    }

    get header() {
        return this.assetType?.Name ?? '…';
    }

    ngOnChanges() {
        this.loadAssetType(this.uid);
    }

    async loadAssetType(uid: string) {
        if (uid !== this.uid) {
            this.assetType = null;
        }

        const newAssetType = await this.assetTypeService.GetAssetTypeByUid(uid).toPromise();
        if (uid === this.uid) {
            this.assetType = newAssetType;
        }
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