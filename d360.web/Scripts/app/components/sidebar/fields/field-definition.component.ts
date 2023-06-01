import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { AssetTypeService } from '../../../services/asset-type.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
	selector: 'd3s-field-definition',
	template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div class="row"
             *ngIf="!isLoading">
            <div class="col s12">
                    <d3s-field-definition-tile [assetTypeUid]="assetTypeUid"
                                               [relationshipTypeUid]="relationshipTypeUid"
						[typeName]="assetType?.Name"
                                               [allowSingleSegmentPath]="false"
                                               [title]="objectName"></d3s-field-definition-tile>
            </div>
        </div>
    `,
	providers: []
})

export class FieldDefinitionComponent extends BaseComponent implements OnInit, OnDestroy {
	private sub: any;

	assetTypeUid: string;
	relationshipTypeUid: string;
	assetType: { Name: string };

	constructor(
		private route: ActivatedRoute,
		secondaryNavService: SecondaryNavService,
		breadcrumbService: HeaderBreadcrumbService,
		private assetTypeService: AssetTypeService,
		protected settingsService: CompanySettingsService
	) {
		super(settingsService);
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = breadcrumbService;
	}

	ngOnInit() {
		this.isLoading = true;
		this.sub = this.route.params.subscribe(
			(params) => {
				if (this.route.snapshot.data?.type === "relationship") {
					this.relationshipTypeUid = this.baseIntersectTypeUid = params['assetTypeUid'];
					this.buildSecondaryNavigation({ intersectTypeUid: this.relationshipTypeUid });

				}
				else {
					this.assetTypeUid = this.baseAssetTypeUid = params['assetTypeUid'];
					this.loadAssetType(this.assetTypeUid);
					this.buildSecondaryNavigationForAssetTypeUid(this.baseAssetTypeUid, null);
				}
				this.isLoading = false;
			}
		);
	}

	async loadAssetType(uid: string) {
		// Note, that we don't set up loading indicator, because in this specific case assetType is not really important
		// It's used only when we add relationship field definition to show information that user already knows.

		if (uid !== this.assetTypeUid) {
			this.assetType = null;
		}

		const newAssetType = await this.assetTypeService.GetAssetTypeByUid(uid).toPromise();
		if (uid === this.assetTypeUid) {
			this.assetType = newAssetType;
		}
	}

	ngOnDestroy() {
		if (this.sub) {
			this.sub.unsubscribe();
		}
	}

	load() {

	}
}
