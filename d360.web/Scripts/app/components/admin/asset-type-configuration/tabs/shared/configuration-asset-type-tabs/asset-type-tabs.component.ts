import { Component, Input } from "@angular/core";
import { AssetTypeClass } from "../../../../../../models/asset.model";
import { Tab } from "../../../../../shared/tabs/tabs.models";


@Component({
    selector: "d3s-configuration-asset-type-tabs",
    templateUrl: './asset-type-tabs.component.html',
    styleUrls: ['./asset-type-tabs.component.less']
})
export class ConfigurationAssetTypeTabsComponent {
    @Input() assetTypeClass: AssetTypeClass;
    @Input() uid: string;

    get tabs(): Tab[] {
        const baseUrl = `/admin/configuration/assets/${AssetTypeClass[this.assetTypeClass]}/${this.uid}`;
        return [
            {
                url: `${baseUrl}/fields`,
                title: $localize`Field Definition`,
                isVisible: () => [AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset].includes(this.assetTypeClass),
            },
            {
                url: `${baseUrl}/owners`,
                title: $localize`Responsibility Type Assignment`,
                isVisible: () => [AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset].includes(this.assetTypeClass),
            },
            {
                url: `${baseUrl}/allocations`,
                title: $localize`Allocations`,
                isVisible: () => [AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset].includes(this.assetTypeClass),
            },
            {
                url: `${baseUrl}/relationships`,
                title: $localize`Relationship Types`,
                isVisible: () => [AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset].includes(this.assetTypeClass),
            },
            {
                url: `${baseUrl}/log`,
                title: $localize`Change Log`,
                isVisible: () => [AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset].includes(this.assetTypeClass),
            }
        ];
    }
}
