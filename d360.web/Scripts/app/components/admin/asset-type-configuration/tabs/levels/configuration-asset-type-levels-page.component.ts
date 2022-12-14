import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { AssetTypeClass } from "../../../../../models/asset.model";
import { AssetTypeService } from "../../../../../services/asset-type.service";


@Component({
    selector: "d3s-configuration-asset-type-levels-page",
    templateUrl: './configuration-asset-type-levels-page.component.html'
})
export class ConfigurationAssetTypeLevelsPageComponent {
    assetTypeClass: AssetTypeClass;
    uid: string;
	assetType: { Name: string, HierarchyMaximumDepth : number };
	assetTypeLegacyData: { Object: string, ObjectID: number };

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
		this.assetTypeLegacyData = await this.assetTypeService.getAssetTypeObjectAndID(uid).toPromise();
        if (uid === this.uid) {
			this.assetType = newAssetType;
        }
    }
}
