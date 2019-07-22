import { Input, Component, OnInit, OnDestroy, Output } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
//import { RightSidebarItem } from '../../../models/rightsidebar.model';
import { AssetTypeMetricModel } from '../../../models/asset.model'; 
import { MetricsService } from '../../../services/metrics.service'; 
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-analytics-component',
    template: ` <div class="row">
                    <div class="col l3 m5 s12">
                        <div class="tile tile-detail">
                            <d3s-admin-metric-asset-type-list (selectionChange)="changeAssetType($event)"></d3s-admin-metric-asset-type-list>
                        </div>
                    </div>
                    <div class="col l9 m7 s12">
                        <div class="row" *ngIf="selectedAssetType != null">
                            <div class="col s12">
                                <div class="tile tile-detail">  
                                    <d3s-admin-metric-list [assetType]="selectedAssetType" (selectionChange)="selectedMetric = $event"></d3s-admin-metric-list>
                                </div>
                            </div>
                        </div>
                    </div>
                <div>
                `,
    providers: [MetricsService]
})

export class AdminAnalyticsComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    private selectedAssetType: AssetTypeMetricModel = null;
    private selectedMetric = null;

    constructor(
        rightSidebarService: RightSidebarService,
        protected messagesService: MessagesObservableService,
        private metricsService: MetricsService, 
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Scoring";
        this.setCommonItems();
        this.setCommonRightSideBar(false);
        //this.rightSidebarService.showItem(new RightSidebarItem('Measures', 'measures',['fa-balance-scale'], '/admin/analytics/measures' ))

    }

    ngOnInit() {
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private changeAssetType(event) {
        this.selectedAssetType = event;
    }

}