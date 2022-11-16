import { Component, Input } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { AssetTypeClass } from "../../../../models/asset.model";
import { AssetTypeService } from "../../../../services/asset-type.service";
import { AssetService } from "../../../../services/asset.service";
import { MessagesObservableService } from "../../../../services/messages-observable.service";
import { CompanySettingsService } from "../../../../services/settings.service";
import { StateService } from "../../../../services/state.service";
import { BaseComponent } from "../../../shared/base.component";

@Component({
    selector: "d3s-configuration-asset-type-delete-page",
    templateUrl: './configuration-asset-type-delete-page.component.html'
})
export class ConfigurationAssetTypeDeletePageComponent extends BaseComponent {
    assetTypeClass: AssetTypeClass;
    uid: string;

    assetType: any;
    assetsCount: any;

    loadingCounter = 0;

    constructor(
        private route: ActivatedRoute,
        private stateService: StateService,
        private assetTypeService: AssetTypeService,
        private assetsService: AssetService,
        protected messagesService: MessagesObservableService,
        settingsService: CompanySettingsService,
        private router: Router) {
        super(settingsService);
    }

    ngOnInit() {
        this.route.params.subscribe((params) => {
            this.assetTypeClass = AssetTypeClass[params["typeClass"] as string];
            this.uid = params["uid"];
            this.loadAssetType(this.uid);
            this.loadCount(this.uid);
        });
    }

    cancel() {
        this.goBack();
    }

    async loadAssetType(uid: string) {
        this.loadingCounter++;
        try {
            const assetType = await this.assetTypeService.GetAssetTypeByUid(uid).toPromise();
            if (uid === this.uid) {
                this.assetType = assetType;
            }
        } finally {
            this.loadingCounter--;
        }
    }

    async loadCount(uid: string) {
        this.loadingCounter++;
        try {
            const assetsCount = (await this.assetsService.getAssetCountOfArtifactTypeUid(uid).toPromise()).count;
            if (uid === this.uid) {
                this.assetsCount = assetsCount;
            }
        } finally {
            this.loadingCounter--;
        }
    }

    delete = async (uid: string) => {
        const result = await this.assetTypeService.deleteSingleAssetType(uid).toPromise();
        result.title = $localize`Success` + "!";
        this.showMessageForResult(this.messagesService, result, $localize`Item successfully removed` + ".");
        this.stateService.reloadLeftNavMenu();
        this.goBack();
    }

    goBack() {
        this.router.navigateByUrl(`/admin/configuration/assets/${AssetTypeClass[this.assetTypeClass]}`)
    }
}