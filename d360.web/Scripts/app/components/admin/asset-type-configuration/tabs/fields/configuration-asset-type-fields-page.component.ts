import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { AssetTypeClass } from "../../../../../models/asset.model";
import { AssetTypeService } from "../../../../../services/asset-type.service";
import { FieldDefinitionComponent } from "../../../../shared/fielddefinition/field-definition.component";

type FieldTileSettings = Pick<
    FieldDefinitionComponent,
    'supportsPrimaryFilterOption' | 'showDisplayInColumn' | 'allowSingleSegmentPath'
>;

@Component({
    selector: "d3s-configuration-asset-type-fields-page",
    templateUrl: './configuration-asset-type-fields-page.component.html'
})
export class ConfigurationAssetTypeFieldsPageComponent {
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
            this.loadAssetType(this.uid);
        });
    }

    async loadAssetType(uid: string) {
        // Note, that we don't set up loading indicator, because in this specific case assetType is not really important
        // It's used only when we add relationship field definition to show information that user already knows.

        if (uid !== this.uid) {
            this.assetType = null;
        }

        const newAssetType = await this.assetTypeService.GetAssetTypeByUid(uid).toPromise();
        if (uid === this.uid) {
            this.assetType = newAssetType;
        }
    }

    get fieldTileSettings(): FieldTileSettings {
        const settings = this.typeClassToFieldTileSettingsMap.get(this.assetTypeClass);
        if (settings == null) {
            throw new Error(`Can't find settings for asset type class ${this.assetTypeClass} (${AssetTypeClass[this.assetTypeClass]})`);
        }

        return settings;
    }

    typeClassToFieldTileSettingsMap = new Map<AssetTypeClass, FieldTileSettings>([
        [
            AssetTypeClass.BusinessAsset,
            {
                supportsPrimaryFilterOption: true,
                showDisplayInColumn: true,
                allowSingleSegmentPath: true
            }
        ],
        [
            AssetTypeClass.TechnicalAsset,
            {
                supportsPrimaryFilterOption: true,
                showDisplayInColumn: true,
                allowSingleSegmentPath: true
            }
        ]
    ])
}
