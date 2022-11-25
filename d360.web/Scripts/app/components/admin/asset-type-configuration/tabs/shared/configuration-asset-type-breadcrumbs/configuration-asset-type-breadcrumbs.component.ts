import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input } from "@angular/core";
import * as _ from "lodash";
import { AssetTypeClass } from "../../../../../../models/asset.model";
import { Breadcrumb } from "../../../../../../models/breadcrumb.model";
import { SiteMenuService } from "../../../../../../services/site-menu.service";
import { typeClassToHeaderSettings } from "../../../shared/typeClassToHeaderSettings";


@Component({
    selector: "d3s-configuration-asset-type-breadcrumbs",
    templateUrl: './configuration-asset-type-breadcrumbs.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfigurationAssetTypeBreadcrumbsComponent {
    @Input() assetTypeClass: AssetTypeClass;
    @Input() uid: string;

    constructor(
        private siteMenuService: SiteMenuService,
        private cdRef: ChangeDetectorRef) {
    }

    idToItemMap?: _.Dictionary<{ Class: AssetTypeClass; Name: string; Uid: string; ParentUid: string; }>;

    get breadcrumbs() {
        const breadcrumbs: Breadcrumb[] = [];

        if (this.uid != null && this.idToItemMap != null) {
            let currentUid = this.uid;
            // eslint-disable-next-line no-constant-condition
            while (true) {
                const item = this.idToItemMap[currentUid];
                breadcrumbs.push(new Breadcrumb(item.Name, `/admin/configuration/assets/${AssetTypeClass[this.assetTypeClass]}/${item.Uid}/fields`));
                if (item.ParentUid == null) {
                    break;
                }

                currentUid = item.ParentUid;
            }
        }
        
        breadcrumbs.push(
            new Breadcrumb(typeClassToHeaderSettings.get(this.assetTypeClass).title, `/admin/configuration/assets/${AssetTypeClass[this.assetTypeClass]}`),
            new Breadcrumb('Configuration'),
        );

        breadcrumbs.reverse();

        return breadcrumbs;
    }

    async ngOnInit() {
        this.idToItemMap = _.keyBy(await this.siteMenuService.getAdminConfigurationMenu().toPromise(), (x) => x.Uid);
        this.cdRef.markForCheck();
    }
}
