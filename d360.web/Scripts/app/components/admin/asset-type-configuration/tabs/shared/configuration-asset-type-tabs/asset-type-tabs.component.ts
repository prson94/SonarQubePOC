import { Component, Input } from "@angular/core";
import { AssetTypeClass } from "../../../../../../models/asset.model";
import { Tab } from "../../../../../shared/tabs/tabs.models";

/*global $localize*/

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
				url: `${baseUrl}/details`,
				title: $localize`Details`,
				isVisible: () => [AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule, AssetTypeClass.DiagramAsset].includes(this.assetTypeClass),
			},
            {
                url: `${baseUrl}/fields`,
				title: $localize`Fields`,
				isVisible: () => [AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule, AssetTypeClass.DiagramAsset].includes(this.assetTypeClass),
            },
            {
                url: `${baseUrl}/owners`,
                title: $localize`Responsibility Type Assignment`,
				isVisible: () => [AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule].includes(this.assetTypeClass),
            },
            {
                url: `${baseUrl}/allocations`,
                title: $localize`Allocations`,
				isVisible: () => [AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Policy].includes(this.assetTypeClass),
            },
            {
                url: `${baseUrl}/relationships`,
                title: $localize`Relationship Types`,
				isVisible: () => [AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule, AssetTypeClass.DiagramAsset].includes(this.assetTypeClass),
			},
			{
				url: `${baseUrl}/levels`,
				title: $localize`Levels`,
				isVisible: () => [AssetTypeClass.Model, AssetTypeClass.Policy].includes(this.assetTypeClass),
			},
            {
                url: `${baseUrl}/log`,
                title: $localize`Change Log`,
				isVisible: () => [AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Rule, AssetTypeClass.DiagramAsset].includes(this.assetTypeClass),
            }
        ];
    }
}
