import { Component, OnInit, Output, EventEmitter, Input} from '@angular/core';
import { Router } from '@angular/router';

import { BaseComponent } from '../shared/base.component';
import { ArtifactService } from '../../services/artifacts.service';
import { Count} from '../../models/counts.model';
import { AssetDetail } from '../../models/asset.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-activity-details-tile',
    providers: [ArtifactService],
    templateUrl: './activity-details-tile.component.html'
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
        this
            .router
            .navigateByUrl(`${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${this.selected.TypeID}/${this.selected.ObjectID}`);
    }

    private artifactLink(
        artifactTypeId,
        artifactId
    ) {
        this
            .router
            .navigateByUrl(`${SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT}/${artifactTypeId}/${artifactId}`);           
    }

    private load() {
        this.isLoading = true;
        this
            .artifactService
            .getActivityDetails(this.objectId, this.daysToLookBack)
            .subscribe(
                res => {
                    this.items = res;
                    this.isLoading = false;
                }
            )
        ;
    }
}
