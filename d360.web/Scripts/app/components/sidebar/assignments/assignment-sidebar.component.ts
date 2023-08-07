import { Component } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { ActivatedRoute } from '@angular/router';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { Subscription } from 'rxjs';
import { LaunchDarklyService } from '@precisely/prism-ng/launch-darkly';

@Component({
	templateUrl: './assignment-sidebar.component.html'
})
export class AssignmentSidebarComponent extends BaseComponent {
	private routeParamsSubscription: Subscription;

	constructor(
		private route: ActivatedRoute,
		secondaryNavService: SecondaryNavService,
		headerbreadcrumbService: HeaderBreadcrumbService,
		protected settingsService: CompanySettingsService,
		launchDarklyService: LaunchDarklyService
	) {
		super(settingsService);
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = headerbreadcrumbService;
		this.launchDarklyService = launchDarklyService;
	}

	ngOnInit() {
		this.routeParamsSubscription = this.route.params.subscribe((params) => {
			this.baseAssetUid = params['assetUid'];
			this.baseAssetTypeUid = params['assetTypeUid'];
			if (this.baseAssetUid) {
				this.buildSecondaryNavigationByAssetUid(this.baseAssetUid);
			} else if (this.baseAssetTypeUid) {
				this.buildSecondaryNavigationForAssetTypeUid(this.baseAssetTypeUid);
			}
		});
	}

	ngOnDestroy() {
		this.routeParamsSubscription?.unsubscribe();
	}
}
