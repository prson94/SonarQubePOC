import { Component } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { ActivatedRoute } from '@angular/router';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { Subscription } from 'rxjs';

@Component({
	templateUrl: './assignment-sidebar.component.html'
})
export class AssignmentSidebarComponent extends BaseComponent {
	private routeParamsSubscription: Subscription;

	constructor(
		private route: ActivatedRoute,
		secondaryNavService: SecondaryNavService,
		headerBreadcrumbService: HeaderBreadcrumbService,
		protected settingsService: CompanySettingsService
	) {
		super(settingsService);
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = headerBreadcrumbService;
	}

	ngOnInit() {
		this.routeParamsSubscription = this.route.params.subscribe((params) => {
			this.baseAssetUid = params['assetUid'];
			this.baseAssetTypeUid = params['assetTypeUid'];
			if (this.baseAssetUid) {
				this.buildSecondaryNavigationByAssetUid(this.baseAssetUid);
			}
		});
	}

	ngOnDestroy() {
		this.routeParamsSubscription?.unsubscribe();
	}
}
