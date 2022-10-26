import { Component, Input } from "@angular/core";
import { Title } from "@angular/platform-browser";
import { AssetTypeClass } from "../../../../models/asset.model";
import { CompanySettingEnum } from "../../../../models/settings.model";
import { CompanySettingsService } from "../../../../services/settings.service";


// TODO: split this to two component: simple header + asset-type-list specific
@Component({
    selector: "d3s-asset-type-list-header",
    templateUrl: './asset-type-list-header.component.html',
    styleUrls: ['./asset-type-list-header.component.less']
})
export class AssetTypeListHeaderComponent {
    @Input() assetTypeClass: AssetTypeClass;

    constructor(private titleService: Title, private settingsService: CompanySettingsService) { }

    ngOnChanges() {
        this.titleService.setTitle(`${this.settingsService.getSettingById(CompanySettingEnum.BrowserTitlePrefix).StringSetting.Value} - ${this.header}`);
    }

    get icon() {
        return this.settings.icon;
    }

    get header() {
        return this.settings.title;
    }

    get settings() {
        let settings = this.typeClassToSettings.get(this.assetTypeClass);
        if (!settings) {
            throw new Error(`Failed to find settings for asset type class ${this.assetTypeClass} (${AssetTypeClass[this.assetTypeClass]})`);
        }

        return settings;
    }

    typeClassToSettings = new Map([
        [
            AssetTypeClass.BusinessAsset, {
                icon: 'fa-sliders',
                title: $localize`Business Assets`
            }
        ],
        [
            AssetTypeClass.TechnicalAsset, {
                icon: 'fa-sliders',
                title: $localize`Technical Assets`
            }
        ]
    ])
}