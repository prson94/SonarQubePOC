import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { PermissionsService } from '../../../services/permissions.service';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { AssetService } from '../../../services/asset.service';
import { AssetTypeClass } from '../../../models/asset.model';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
	selector: 'd3s-relationships-wrapper',
	template: `
     <gov-relationship-grid *ngIf="assetUid" [assetUid]="assetUid" [assetTypeUid]="assetTypeUid"></gov-relationship-grid>
    `,
	providers: [PermissionsService, ObjectDetailService]
})

export class RelationshipsComponent extends BaseComponent implements OnInit, OnDestroy {
	private sub: any;
	assetUid: string = '';
	assetTypeUid: string = '';

	constructor(
		private route: ActivatedRoute,
		private router: Router,
		private permissionsService: PermissionsService,
		private assetService: AssetService,
		private objectDetailService: ObjectDetailService,
		secondaryNavService: SecondaryNavService,
		breadcrumbService: HeaderBreadcrumbService,
		protected settingsService: CompanySettingsService
	) {
		super(settingsService);
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = breadcrumbService;
	}

	ngOnInit() {
		this.sub = this.route.params.subscribe((params) => {
			const uid = params['uid'];

			this.assetService.getAssetTypeClassForAsset(uid)
				.subscribe((res) => {
					if (res === AssetTypeClass.Reference) {
						this.permissionsService.getAssetTypePermissions(uid)
							.subscribe((p) => {
								this.objectPermission = p;
								this.buildSecondaryNavigationForAssetTypeUid(uid);
								this.assetTypeUid = uid;
								this.assetUid = uid;
							});
					}
					else {
						this.permissionsService.getAssetPermissions(uid)
							.subscribe((p) => {
								this.objectPermission = p;
								this.assetUid = uid;
								this.buildSecondaryNavigationByAssetUid(this.assetUid);
							});
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
