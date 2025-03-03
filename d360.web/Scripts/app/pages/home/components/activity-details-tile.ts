import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { BaseComponent } from '../../../components/shared/base.component';
import { AssetDetail } from '../../../models/asset.model';
import { ArtifactService } from '../../../services/artifacts.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { TilesModule } from '../../../components/shared/tiles/tiles.module';
import { TableModule } from 'primeng/table';
import { CoreModule } from '../../../components/shared/core.module';

@Component({
    selector: 'activity-details-tile',
	standalone: true,
	imports: [CommonModule, CoreModule, TableModule, TilesModule],
	//providers: [ArtifactService],
    templateUrl: './activity-details-tile.html'
})
export class ActivityDetailsTile extends BaseComponent implements OnInit {    
    private items: AssetDetail[] = [];
    private selected: AssetDetail;

    @Input() objectName: string;
    @Input() objectId: number = 0;
    @Input() daysToLookBack: number = 7;
    @Output() close = new EventEmitter();

    constructor(
        private router: Router,
        private artifactService: ArtifactService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
    }

    ngOnInit() {
        if (this.objectId > 0) {
            this.load();
        }
    }

	private navigateToArtifact() {
        this.router
			.navigateByUrl(this.federateUrl(SiteUrlHelpers.getAssetUrl(this.selected["uid"])));

    }

    private artifactLink(uid:string) {
        this.router
			.navigateByUrl(this.federateUrl(SiteUrlHelpers.getAssetUrl(uid)));
    }

    private load() {
        this.isLoading = true;
        this
            .artifactService
            .getActivityDetails(this.objectId, this.daysToLookBack)
            .subscribe(
                (res) => {
                    this.items = res;
                    this.isLoading = false;
                }
            )
        ;
    }
}
