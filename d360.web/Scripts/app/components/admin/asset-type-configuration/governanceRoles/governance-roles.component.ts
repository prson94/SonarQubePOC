import { Component, ChangeDetectionStrategy, OnInit, OnDestroy, ChangeDetectorRef } from "@angular/core";
import { Subscription } from "@datadog/browser-core";
import { AssetTypeClass } from "../../../../models/asset.model";
import { GovernanceRole } from "../../../../models/governance-role.model";
import { CompanySettingEnum, GuidSetting, SettingsPutModel } from "../../../../models/settings.model";
import { AssetTypeService } from "../../../../services/asset-type.service";
import { MessagesObservableService } from "../../../../services/messages-observable.service";
import { CompanySettingsService } from "../../../../services/settings.service";


@Component({
    selector: 'd3s-governance-roles',
    templateUrl: './governance-roles.component.html',
    providers: [AssetTypeService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GovernanceRolesComponent implements OnInit, OnDestroy {
    assetTypeClass = AssetTypeClass.DiagramAsset;

    private refListSub: Subscription;
    constructor(
        private assetsService: AssetTypeService,
        private cdRef: ChangeDetectorRef,
        protected settingsService: CompanySettingsService,
        private messagesService: MessagesObservableService
    ) {
    }

    private model: GovernanceRole;
    private originalModel: GovernanceRole;
    private refListDDL: { value: string, label: string }[] = [];

    public isSaving = false;
    public isLoading = true;

    ngOnInit() {
        this.isLoading = true;
        this.refListSub = this.assetsService.getAssetTypesByClass(AssetTypeClass.Reference)
            .subscribe((res) => {
                this.refListDDL = [];
                this.refListDDL.push({ value: '', label: $localize`Select Reference List...` });
                res.forEach((x) => {
                    this.refListDDL.push({ value: x.uid, label: x.Name });
                });

                this.cdRef.detectChanges();

            });

        const setting = this.settingsService.getSettingById(CompanySettingEnum.GovernanceRoleReferenceListUid);
        this.originalModel = new GovernanceRole();
        if (setting.ScalarValue && setting.ScalarValue !== "00000000-0000-0000-0000-000000000000") {
            this.originalModel.RefListUid = setting.ScalarValue;
        }

        this.model = this.getInitialData();
        this.isLoading = false;
        this.cdRef.detectChanges();
    }

    discard() {
        this.model = this.getInitialData();
    }

    private getInitialData(): GovernanceRole {
        return JSON.parse(JSON.stringify(this.originalModel));
    }

    public save() {
        //calling save function
        this.isSaving = true;

        const setting = new SettingsPutModel();
        setting.SettingID = CompanySettingEnum.GovernanceRoleReferenceListUid;
        setting.GuidSetting = new GuidSetting();
        setting.GuidSetting.Value = this.model.RefListUid;

        this.settingsService.putSetting(setting)
            .subscribe(
                (res) => {
                    this.isSaving = false;
                    this.originalModel = this.model;
                    this.messagesService.showInfoMessage($localize`Success`, $localize`Governance Role successfully updated`);
                    this.cdRef.detectChanges();
                },
                (err) => {
                    this.messagesService.showError($localize`Error saving governance role`, err.error.message);
                }
            );
    }

    ngOnDestroy() {
        if (this.refListSub) {
            this.refListSub.unsubscribe();
        }
    }
}
