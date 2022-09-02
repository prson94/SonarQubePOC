import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AssetTypeClass } from '../../models/asset.model';
import { AssetService } from '../../services/asset.service';
import { CompanySettingsService } from '../../services/settings.service';
import { BaseComponent } from '../shared/base.component';

@Component({
	selector: 'd3s-asset',
	template: `<div id="main">
		<d3s-artifact-item *ngIf="showArtifactComponent" [assetUid]="assetUid"></d3s-artifact-item>
		<d3s-hierarchy-item *ngIf="showHierarchyComponent" [assetTypeClass]="assetTypeClass" [assetUid]="assetUid"></d3s-hierarchy-item>
		<d3s-rule-item *ngIf="showRuleComponent" [assetUid]="assetUid"></d3s-rule-item>
	</div>`,
	providers: [AssetService],
})

export class AssetComponent extends BaseComponent implements OnInit, OnDestroy {
	private sub: any;
	assetUid: string = '';
	assetTypeClass: AssetTypeClass;

	showArtifactComponent: boolean = false;
	showHierarchyComponent: boolean = false;
	showRuleComponent: boolean = false;

	constructor(
		private assetService: AssetService,
		protected settingsService: CompanySettingsService,
		private route: ActivatedRoute,
		private router: Router) {
		super(settingsService);
	}

	ngOnInit() {
		this.sub = this.route.params.subscribe(params => {
			this.assetUid = params['assetUid'];
			this.assetService.getAssetTypeClassForAsset(this.assetUid)
				.subscribe((res) => {
					this.assetTypeClass = res;
					this.showArtifactComponent = this.assetTypeClass === AssetTypeClass.BusinessAsset
						|| this.assetTypeClass === AssetTypeClass.TechnicalAsset;

					this.showHierarchyComponent = this.assetTypeClass === AssetTypeClass.Policy
						|| this.assetTypeClass === AssetTypeClass.Model;

					this.showRuleComponent = this.assetTypeClass === AssetTypeClass.Rule;
				}
				);
		});
	}

	ngOnDestroy() {
		if (this.sub) {
			this.sub.unsubscribe();
		}
	}
}
