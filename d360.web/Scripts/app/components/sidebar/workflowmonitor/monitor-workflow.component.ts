import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { AssetService } from '../../../services/asset.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { TitleAndTabsService } from '../../../services/title-and-tabs.service';

@Component({
    selector: 'd3s-workflow-monitor',
    providers: [AssetService],
    template: ` 
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="showMonitor">
                <div class="col s12">
                    <div class="tile tile-detail">
                        <d3s-monitor [objectType]="objectType" [objectId]="objectID"></d3s-monitor>
                    </div>
                </div>
            </div>
        `
})

export class MonitorWorkflowComponent extends BaseComponent implements OnInit {
    sub: any;
    objectType: string;
    objectID: number;
    showMonitor: boolean = false;

    constructor(
        private route: ActivatedRoute,
        private assetService: AssetService,
        private titleAndTabsService: TitleAndTabsService,
        breadcrumbService: HeaderBreadcrumbService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        if (!this.titleAndTabsService.isInitialize) {
            this.titleAndTabsService.initializeTitleAndTabsInRightSidebar(this.route.params, 'Workflow');
        }

        this.sub = this.route.params.subscribe(params => {
            let reloadNav = params['isAdminPage'] && params['isAdminPage'] == 'false' ? false : true;
            let assetUid = params['assetUid'];
            if (assetUid == null || assetUid == undefined) {
                this.objectID = +params['objectId'];
                this.objectType = params['objectType'];
                this.showMonitor = true;
                if (reloadNav)
                    this.buildSecondaryNavigationForObject(this.objectID, this.objectType);
            }
            else
            {
                this.assetService.GetObjectUIDetailsForAssetUID(assetUid)
                .subscribe(res => {
                    this.objectID = +res.ObjectId;
                    this.objectType = res.Object;
                    this.showMonitor = true;
                    if (reloadNav)
                        this.buildSecondaryNavigationForObject(this.objectID, this.objectType);
                });
            }
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }
}