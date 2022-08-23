import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AssetService } from '../../services/asset.service';
import { CompanySettingsService } from '../../services/settings.service';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-asset',
	template: `<div id="main">
		<d3s-artifact-item [assetUid]="assetUid"></d3s-artifact-item>
	</div>`,
    providers: [AssetService],
})

export class AssetComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
	assetUid: string = '';

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
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}
