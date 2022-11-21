import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { AssetTypeClass } from "../../../../../models/asset.model";
import { AssetTypeService } from "../../../../../services/asset-type.service";

@Component({
    selector: "d3s-configuration-asset-type-log-page",
    templateUrl: './configuration-asset-type-log-page.component.html',
    styleUrls: ['./configuration-asset-type-log-page.component.less'],
})
export class ConfigurationAssetTypeLogPageComponent {
    assetTypeClass: AssetTypeClass;
    uid: string;

    assetType?: { Object: string; ObjectID: number; };
    loadingCount = 0;

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
        if (uid !== this.uid) {
            this.assetType = null;
        }

        this.loadingCount++;
        try {
            const newAssetType = await this.assetTypeService.getAssetTypeObjectAndID(uid).toPromise();
            if (uid === this.uid) {
                this.assetType = newAssetType;
            }
        }
        finally {
            this.loadingCount--;
        }
    }
}
