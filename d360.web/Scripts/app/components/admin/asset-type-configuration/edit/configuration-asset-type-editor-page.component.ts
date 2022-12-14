import { Component, Input } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { AssetTypeClass } from "../../../../models/asset.model";
import { AssetService } from "../../../../services/asset.service";
import { StateService } from "../../../../services/state.service";

@Component({
    selector: "d3s-configuration-asset-type-editor-page",
    templateUrl: './configuration-asset-type-editor-page.component.html'
})
export class ConfigurationAssetTypeEditorPageComponent {
    @Input() assetTypeClass: AssetTypeClass;
    @Input() uid: string;
    @Input() parentUid: string | undefined;

    id: number | undefined;
    parentObjectId: number | undefined;
    loadingCounter = 0;

    constructor(
        private route: ActivatedRoute,
        private stateService: StateService,
        private assetsService: AssetService,
        private router: Router) { }

    ngOnInit() {
        this.route.params.subscribe((params) => {
            this.assetTypeClass = AssetTypeClass[params["typeClass"] as string];
            this.uid = params["uid"];
            this.parentUid = params["parentUid"];

            this.loadIdByUid(this.uid);
            this.loadParentIdByUid(this.parentUid);
        });
    }

    async loadIdByUid(uid: string) {
        if (uid == null) {
            return;
        }

        this.loadingCounter++;
        try {
            const { AssetTypeID } = await this.assetsService.getAssetTypeLegacyData(uid).toPromise();
            if (this.uid === uid) {
                this.id = AssetTypeID;
            }
        }
        finally {
            this.loadingCounter--;
        }
    }

    async loadParentIdByUid(uid?: string) {
        if (uid == null) {
            return;
        }

        this.loadingCounter++;
        try {
            const { ObjectID } = await this.assetsService.getAssetTypeLegacyData(uid).toPromise();
            if (this.parentUid === uid) {
                this.parentObjectId = ObjectID;
            }
        }
        finally {
            this.loadingCounter--;
        }
    }

    cancel() {
        this.goBack();
    }

    actionComplete() {
        this.stateService.reloadLeftNavMenu();
        this.goBack();
    }

    goBack() {
        this.router.navigateByUrl(`/admin/configuration/assets/${AssetTypeClass[this.assetTypeClass]}`);
    }
    
    get formTitle() {
        const isEdit = this.uid != null;

        const baseTitle = isEdit ? $localize`Edit` : $localize`Add`;
        const typeToTitle = isEdit ? this.typeClassToConfigurationEditTitle : this.typeClassToConfigurationAddTitle;
        const typeClassTitle = typeToTitle.get(this.assetTypeClass);
        if (typeClassTitle == null) {
            throw new Error(`Failed to find localization for ${this.assetTypeClass} (${AssetTypeClass[this.assetTypeClass]})`);
        }

        return `${baseTitle} ${typeClassTitle}`;
    }
    
    typeClassToConfigurationAddTitle = new Map([
        [AssetTypeClass.BusinessAsset, $localize`Business Asset`],
		[AssetTypeClass.TechnicalAsset, $localize`Technical Asset`],
		[AssetTypeClass.Model, $localize`Model`],
		[AssetTypeClass.Policy, $localize`Policy`]
    ])
    
    typeClassToConfigurationEditTitle = new Map([
        [AssetTypeClass.BusinessAsset, $localize`Business Asset Type`],
		[AssetTypeClass.TechnicalAsset, $localize`Technical Asset Type`],
		[AssetTypeClass.Model, $localize`Model Type`],
		[AssetTypeClass.Policy, $localize`Policy Type`]
    ])
}
