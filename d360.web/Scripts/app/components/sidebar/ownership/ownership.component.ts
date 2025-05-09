import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subject } from "rxjs";
import { BaseComponent } from '../../shared/base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { FeatureFlagsInitService } from '../../../services/feature-flags-init.service';
import { FeatureFlags } from '../../../_shared/models/feature-flags';

@Component({
	selector: 'd3s-ownership',
	template: `<owner-list [assetUid]="uid"></owner-list>`
})
export class OwnershipComponent extends BaseComponent implements OnInit {
	destroySubject$: Subject<void> = new Subject();

	newSecurityEnabledFeatureFlag: boolean = true;

	constructor(
		private route: ActivatedRoute,
		secondaryNavService: SecondaryNavService,
		breadcrumbService: HeaderBreadcrumbService,
		protected settingsService: CompanySettingsService,
		featureFlagService: FeatureFlagsInitService
	) {
		super(settingsService);
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = breadcrumbService;

		featureFlagService.getFlagValue(FeatureFlags.NewSecurityModel).then((flag) => {
			this.newSecurityEnabledFeatureFlag = flag;
		});
	}

	ngOnInit() {
		this.route.params.subscribe(
			(params) => {
				this.uid = params['assetUid'];
				this.buildSecondaryNavigationByAssetUid(this.uid);
			}
		);
	}
}
