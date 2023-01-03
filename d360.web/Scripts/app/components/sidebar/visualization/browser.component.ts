import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-browser',
    template: `
        <ng-container>
            <d3s-assetbrowser (saveStateChanged)="saveStateChanged($event)" [assetUid]="uid" [readonly]="true"></d3s-assetbrowser>
        </ng-container>
        `
})

export class BrowserComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    public isSaved: boolean = null;

    constructor(
        private route: ActivatedRoute,
        secondaryNavService: SecondaryNavService,
        headerbreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = headerbreadcrumbService;

    }

    ngOnInit() {
        this.sub = this.route.params.subscribe((params) => {
            this.uid = params['assetUid'];
			this.buildSecondaryNavigationByAssetUid(this.uid);
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }

    saveStateChanged($event: boolean) {
        this.isSaved = $event;
    }
}
