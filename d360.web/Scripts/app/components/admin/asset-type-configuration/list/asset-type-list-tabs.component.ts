import { Component, Input } from "@angular/core";
import { AssetTypeClass } from "../../../../models/asset.model";
import { CompanySettingEnum } from "../../../../models/settings.model";
import { CompanySettingsService } from "../../../../services/settings.service";
import { Tab } from "../../../shared/tabs/tabs.models";


@Component({
    selector: "d3s-configuration-asset-type-list-tabs",
    templateUrl: './asset-type-list-tabs.component.html',
    styleUrls: ['./asset-type-list-tabs.component.less']
})
export class ConfigurationAssetTypeListTabsComponent {
    @Input() assetTypeClass: AssetTypeClass;

    constructor(private settingsService: CompanySettingsService) {
    }

    get showGovernanceRolesWarning() {
        const setting = this.settingsService.getSettingById(CompanySettingEnum.GovernanceRoleReferenceListUid);
        if (!setting.ScalarValue || setting.ScalarValue === "00000000-0000-0000-0000-000000000000") {
            return true;
        } else {
            return false;
        }
    }

    get tabs(): Tab[] {
        const baseUrl = `/admin/configuration/assets/${AssetTypeClass[this.assetTypeClass]}`;
        return [
            {
                url: `${baseUrl}`,
                title: $localize`Asset Types`,
                isVisible: () => [
                    AssetTypeClass.DiagramAsset
                ].includes(this.assetTypeClass),
            },
            {
                url: `${baseUrl}/governanceRoles`,
                title: $localize`Governance Roles`,
                isVisible: () => [
                    AssetTypeClass.DiagramAsset
                ].includes(this.assetTypeClass),
                warningMessage: this.showGovernanceRolesWarning
                        ? 'GovRoleWarning'
                    : null
            }
        ];
    }
}
