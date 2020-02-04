import { Input, Component, OnInit, OnDestroy, Output } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { AssetTypeMetricModel } from '../../../models/asset.model';
import { MetricsService } from '../../../services/metrics.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Router, ActivatedRoute } from '@angular/router';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { AssetService } from '../../../services/asset.service';
import { AssetTypeService } from '../../../services/asset-type.service';
import { SearchResult } from '../../../models/search-result.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { AllocationService } from '../../../services/allocations.service';
import { ScoreType } from '../../../models/metrics.model';

@Component({
    selector: 'd3s-admin-analytics-details',
    template: `  <div class="col s12">
                        <div class="row" *ngIf="selectedAssetType != null">
                            <div class="col s12">
                                <div class="tile tile-detail">  
                                    <d3s-admin-metric-list *ngIf="scoreTypeEnumValue == 'Governance'" [assetType]="selectedAssetType" (selectionChange)="selectedMetric = $event"></d3s-admin-metric-list>
                                
                                    <d3s-admin-data-quality-list *ngIf="scoreTypeEnumValue == 'DataQuality'"></d3s-admin-data-quality-list>
                                </div>
                            </div>
                        </div>
                   </div>
                `,
    providers: [MetricsService, AssetTypeService, AllocationService]
})

export class AdminAnalyticsDetailsComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    private selectedAssetType: AssetTypeMetricModel = null;
    private selectedMetric = null;
    routeParamsSubscription: any;

    private assetGuid: string;
    private scoreTypeEnumValue: ScoreType;

    constructor(
        secondaryNavService: SecondaryNavService,
        private route: ActivatedRoute,
        private router: Router,
        protected messagesService: MessagesObservableService,
        private metricsService: MetricsService,
        private allocationService: AllocationService,
        private assetTypeService: AssetTypeService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title) {
        super(headerBreadcrumbService, titleService, secondaryNavService);
        this.areaName = "Scoring";
    }

    ngOnInit() {
        this.routeParamsSubscription = this.route.params.subscribe(params => {
            this.assetGuid = params['assetTypeUid'];
            this.scoreTypeEnumValue = params['scoreTypeEnumValue'];
            this.assetTypeService.GetAssetTypeByUid(this.assetGuid).subscribe(res => {
                this.selectedAssetType = { Class: res.Class.Name, Name: res.Name, Uid: res.uid };
                this.changeAssetType(this.selectedAssetType);
            });
            this.setScoringSecondaryNavTabs(this.scoreTypeEnumValue, this.assetGuid);
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private changeAssetType(event) {
        this.selectedAssetType = event;
        this.areaName = 'Scoring';
        this.areaLink = '/admin/scoring';
        this.tabTitle = 'Governance Score';
        this.setCommonItems(true, this.selectedAssetType.Name);
        this.setCommonSecondaryNavTabs(false);
        this.setScoringSecondaryNavTabs(this.scoreTypeEnumValue, this.selectedAssetType.Uid);
        this.allocationService.getAllocations()
            .subscribe(r => {
                var crumb = new Breadcrumb(this.selectedAssetType.Name, null, null, 'allocation', 1);
                r.forEach(x => {
                    var url = `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SCORING}/${x.assetTypeUid}/${x.scoreType}`;
                    let searchRes: SearchResult = new SearchResult();
                    searchRes.Name = x.assetTypePath;
                    searchRes.Url = url;
                    searchRes.Uid = x.assetTypeUid;
                    crumb.preLoadedTypeAhead.push(searchRes);
                });
                this.headerBreadcrumbService.showBreadcrumb(crumb);
                this.isLoading = false;
            });
    }

}
