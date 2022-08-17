import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, ActivatedRoute, Route } from '@angular/router';
import { AssetTypeClass } from '../../models/asset.model';
import { AssetTypeService } from '../../services/asset-type.service';
import { AssetService } from '../../services/asset.service';
import { CompanySettingsService } from '../../services/settings.service';
import { BaseComponent } from '../shared/base.component';

@Component({
	selector: 'd3s-assets-base',
	template: `<div id="main">
		<d3s-artifact-list *ngIf="showArtifactList" [assetTypeUid]="assetTypeUid"></d3s-artifact-list>
		<d3s-rule-list *ngIf="showRuleList" [assetTypeUid]="assetTypeUid"></d3s-rule-list>
		<d3s-hierarchy-item-structure *ngIf="showHierarchyList" [assetTypeClass]="assetTypeClass" [assetTypeUid]="assetTypeUid"></d3s-hierarchy-item-structure>
</div>`,
    providers: [AssetService],
})

export class AssetsBaseComponent extends BaseComponent implements OnInit, OnDestroy {
	assetTypeUid: string;
	assetTypeClass: AssetTypeClass;

	showArtifactList: boolean = false;
	showRuleList: boolean = false;
	showHierarchyList: boolean = false;

	constructor(
		private assetTypeService: AssetTypeService,
        protected settingsService: CompanySettingsService,
        private route: ActivatedRoute,
        private router: Router) {
        super(settingsService);
    }

    ngOnInit() {
		this.route.params.subscribe(params => {
			this.assetTypeService.GetAssetTypeByUid(params['assetTypeUid'])
				.subscribe((res) => {
					let cs = res.Class.ID;
					console.log(cs);
					this.showArtifactList = cs === AssetTypeClass.BusinessAsset || cs === AssetTypeClass.TechnicalAsset;
					this.showRuleList = cs === AssetTypeClass.Rule;
					this.showHierarchyList = cs === AssetTypeClass.Policy || cs === AssetTypeClass.Model;

					this.assetTypeUid = res.uid;
					this.assetTypeClass = cs;
				})
		});
    }

    ngOnDestroy() {

    }
}
