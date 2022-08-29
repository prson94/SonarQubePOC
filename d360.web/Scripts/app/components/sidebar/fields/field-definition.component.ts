import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
	selector: 'd3s-field-definition',
	template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div class="row"
             *ngIf="!isLoading">
            <div class="col s12">
                <div class="tile tile-detail">
                    <d3s-field-definition-tile [assetTypeUid]="assetTypeUid"
                                               [relationshipTypeUid]="relationshipTypeUid"
                                               [allowSingleSegmentPath]="false"
                                               [objectName]="objectName"
                                               [title]="objectName"></d3s-field-definition-tile>
                </div>
            </div>
        </div>
    `,
	providers: [ObjectDetailService]
})

export class FieldDefinitionComponent extends BaseComponent implements OnInit, OnDestroy {
	private sub: any;
	objectID: number;
	objectType: string;
	objectName: string;
	assetTypeUid: string;
	relationshipTypeUid: string;

	constructor(
		private objectDetailService: ObjectDetailService,
		private route: ActivatedRoute,
		secondaryNavService: SecondaryNavService,
		private router: Router,
		breadcrumbService: HeaderBreadcrumbService,
		protected settingsService: CompanySettingsService
	) {
		super(settingsService);
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = breadcrumbService;
	}

	ngOnInit() {
		this.isLoading = true;
		this.sub = this.route.params.subscribe(
			params => {
				this.baseAssetTypeUid = params['assetTypeUid']; // (+) converts string 'id' to a number

				this.objectDetailService.getObjectDetailByObjectUid(this.baseAssetTypeUid).subscribe(
					res => {
						if (res) {
							this.objectName = $localize`Field Definitions for` + " " + (res.Name ? res.Name : res.DisplayValue);
							if (res.Object.toLowerCase() === 'intersecttype') {
								this.relationshipTypeUid = res.UID;
							}
							else {
								this.assetTypeUid = res.AssetTypeUid;
							}
							this.isLoading = false;
						}
						this.isLoading = false;
					}
				);

				this.buildSecondaryNavigationForAssetTypeUid(this.baseAssetTypeUid, null);
			}
		);
	}

	ngOnDestroy() {
		if (this.sub) {
			this.sub.unsubscribe();
		}
	}

	load() {

	}
}
