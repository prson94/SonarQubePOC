import { ChangeDetectionStrategy, Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { BaseComponent } from '../../shared/base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { AuditService } from '../../../services/audit.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-audit-page',
    providers: [AuditService],
    templateUrl: './audit-page.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class AuditPageComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;

    constructor(
        private route: ActivatedRoute,
        private auditService: AuditService,
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        this.sub = this
            .route
            .params
            .subscribe((params) => {
                this.uid = params['uid'];

                this.auditService.getLegacyDetails(this.uid).subscribe((res) => {
                    this.objectName = res.DisplayValue;
                    this.objectID = res.ObjectId;
                    this.objectType = res.Object;

                    if (this.objectName === "MetricAllocation") {
                        this.objectName = "Score Definition";
                    }
                    let reloadNav = params['isAdminPage'] && params['isAdminPage'] == 'false' ? false : true;

                    //do not reload 2nd navigation for audit page as both grid pages and config pages share same URL
                    if (["PolicyType", "TaxonomyType", "Report", "IntersectType", "ResponsibilityType", "ReferenceItemType"].indexOf(this.objectType) > -1) {
                        reloadNav = false;
                    }

                    const objectID = this.objectType == 'Tag' ? params['uid'] : this.objectID;

					if (this.uid === this.metricAllocationUid) {
						this.buildSecondaryNavigation({ isScoringDefinitionPage:true });
					}
					else if (this.uid.toLowerCase() === this.groupTypeUid.toLowerCase()) {
						this.buildSecondaryNavigationForAssetTypeUid(this.groupTypeUid);
					}
                    else if (reloadNav) {
                        this.buildSecondaryNavigationForObject(objectID, this.objectType);
                    }

                    if (!this.objectName && this.objectType.toLocaleLowerCase() === 'semantic') {
                        this.objectName = res.DisplayValue;
                    }
                });
            });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}
