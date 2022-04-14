import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseComponent } from '../../shared/base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { GovernanceRole } from '../../../models/governance-role.model';
import { AssetTypeService } from '../../../services/asset-type.service';
import { AssetTypeClass } from '../../../models/asset.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { forkJoin } from 'rxjs';
import { CompanySettingEnum, SettingsPutModel, StringSetting, GuidSetting } from '../../../models/settings.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-governance-roles',
    templateUrl: './governance-roles-sidebar.component.html',
    providers: [AssetTypeService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GovernanceRolesComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private refListSub: any;
    constructor(
        private route: ActivatedRoute,
        private router: Router,
        secondaryNavService: SecondaryNavService,
        private assetsService: AssetTypeService,
        breadcrumbService: HeaderBreadcrumbService,
        private cdRef: ChangeDetectorRef,
        protected settingsService: CompanySettingsService,
        private messagesService: MessagesObservableService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    private datatype: number = 0;
    private model: GovernanceRole;
    private originalModel: GovernanceRole;
    private refListDDL: any[] = [];
    private isSaving: boolean = false;


    ngOnInit() {
        this.isLoading = true;
        this.sub = this
            .route
            .params
            .subscribe(params => {
                this.buildSecondaryNavigationForObject(0, 'TaskType');
            });
        this.refListSub = this.assetsService.getAssetTypesByClass(AssetTypeClass.Reference)
            .subscribe(res => {
                this.refListDDL = [];
                this.refListDDL.push({ value: '', label: $localize`Select Reference List...` });
                res.forEach(x => {
                    this.refListDDL.push({ value: x.uid, label: x.Name });
                })

                this.cdRef.detectChanges();

            });

        let setting = this.settingsService.getSettingById(CompanySettingEnum.GovernanceRoleReferenceListUid);
        this.originalModel = new GovernanceRole();
        if (setting.ScalarValue && setting.ScalarValue !== "00000000-0000-0000-0000-000000000000") {
            this.originalModel.RefListUid = setting.ScalarValue;
            this.datatype = 4;
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

    private isDirty() {
        var orig = this.getInitialData();
        return orig.Name != this.model.Name || orig.Description != this.model.Description || orig.RefListUid != this.model.RefListUid;
    }

    private save() {
        //calling save function
        this.isSaving = true;

        var setting = new SettingsPutModel();
        setting.SettingID = CompanySettingEnum.GovernanceRoleReferenceListUid;
        setting.GuidSetting = new GuidSetting();
        setting.GuidSetting.Value = this.model.RefListUid;

        this.settingsService.putSetting(setting)
            .subscribe(
                (res) => {
                    this.isSaving = false;
                    this.originalModel = this.model;
                    this.messagesService.showInfoMessage($localize`Success`, $localize`Governance Role successfully updated`);
                },
                (err) => {
                    this.messagesService.showError($localize`Error saving governance role`, err.error.message);
                }
            );
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
        if (this.refListSub) {
            this.refListSub.unsubscribe();
        }
    }
}
