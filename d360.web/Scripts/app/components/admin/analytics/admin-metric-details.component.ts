import { Input, Component, OnInit, OnDestroy, Output } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
//import { RightSidebarItem } from '../../../models/secondaryNav.model';
import { AssetTypeMetricModel } from '../../../models/asset.model';
import { MetricsService } from '../../../services/metrics.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
    selector: 'd3s-admin-analytics-details',
    template: `  <div class="col l9 m7 s12">
                        <div class="row" *ngIf="selectedAssetType != null">
                            <div class="col s12">
                                <div class="tile tile-detail">  
                                    <d3s-admin-metric-list [assetType]="selectedAssetType" (selectionChange)="selectedMetric = $event"></d3s-admin-metric-list>
                                </div>
                            </div>
                        </div>
                    < /div>
                `,
    providers: [MetricsService]
})

export class AdminAnalyticsDetailsComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    private selectedAssetType: AssetTypeMetricModel = null;
    private selectedMetric = null;
    routeParamsSubscription: any;

    private assetGuid: number;
    private scoreTypeEnumValue: number;

    constructor(
        secondaryNavService: SecondaryNavService,
        private route: ActivatedRoute,
        private router: Router,
        protected messagesService: MessagesObservableService,
        private metricsService: MetricsService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title) {
        super(headerBreadcrumbService, titleService, secondaryNavService);
        this.areaName = "Scoring";
        this.tabTitle = 'Scoring';
        this.setCommonItems();
        this.setCommonSecondaryNavTabs(false);

    }

    ngOnInit() {
        this.routeParamsSubscription = this.route.params.subscribe(params => {
            this.assetGuid = +params['assetTypeUid'];
            this.scoreTypeEnumValue = +params['scoreTypeEnumValue'];
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private changeAssetType(event) {
        this.selectedAssetType = event;
    }

}
