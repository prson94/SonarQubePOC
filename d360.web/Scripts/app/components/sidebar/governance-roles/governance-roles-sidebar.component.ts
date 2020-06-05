import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { BaseComponent } from '../../shared/base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { GovernanceRole } from '../../../models/governance-role.model';
import { AssetTypeService } from '../../../services/asset-type.service';
import { AssetTypeClass } from '../../../models/asset.model';

declare var CompanySettings;

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
        secondaryNavService: SecondaryNavService,
        private assetsService: AssetTypeService,
        breadcrumbService: HeaderBreadcrumbService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    private model: GovernanceRole;
    private refListDDL: any[] = [];
    private isSaving: boolean = false;
    

    ngOnInit() {
        this.sub = this
            .route
            .params
            .subscribe(params => {
                this.buildSecondaryNavigationForObject(0, 'TaskType');
            });
        this.isLoading = true;
        this.refListSub = this.assetsService.getAssetTypesByClass(AssetTypeClass.Reference)
            .subscribe(res => {
                this.refListDDL = [];
                this.refListDDL.push({ value: '', label: 'Select Reference List...' });
                res.forEach(x => {
                    this.refListDDL.push({ value: x.uid, label: x.Name });
                })

                this.isLoading = false;
                this.cdRef.detectChanges();
                
            });
        this.model = this.getInitialData();
    }

    discard() {
        this.model = this.getInitialData();
    }

    private getInitialData(): GovernanceRole {
        var gr = new GovernanceRole();
        gr.RefListUid = CompanySettings["GovernanceRoleReferenceListUid"];
        gr.Name = CompanySettings["GovernanceRoleLabel"];
        gr.Description = CompanySettings["GovernanceRoleDescription"];
        return gr;
    }

    private isDirty() {
        var orig = this.getInitialData();
        return orig.Name != this.model.Name || orig.Description != this.model.Description || orig.RefListUid != this.model.RefListUid;
    }

    private save() {
        //calling save function
        this.isSaving = true;
        this.refListSub = this.assetsService.getAssetTypesByClass(AssetTypeClass.Reference)
            .subscribe(res => {
                this.isSaving = false;

            });
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
