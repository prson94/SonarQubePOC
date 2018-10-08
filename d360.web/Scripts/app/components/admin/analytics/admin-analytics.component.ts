import { Input, Component, OnInit, OnDestroy, Output } from '@angular/core';
import { MessagesService } from '../../../services/messages.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../../models/rightsidebar.model';
import { AssetTypeMetricModel } from '../../../models/asset.model';
import { EventEmitter } from 'events';
import { MetricsService } from '../../../services/metrics.service';

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
    private models: AssetTypeMetricModel[] = [];

    constructor(
        rightSidebarService: RightSidebarService,
        protected messagesService: MessagesService,
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
        this.metricsService.getAssetTypes()
            .then(r => {
                this.models = r;
                this.isLoading = false;
                if (this.models.length && this.models.length > 0) {
                    this.selectedAssetType = this.models[0];
                    //this.selectionChange.emit(this.selection);
                }
            });
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private changeAssetType(event) {
        this.selectedAssetType = event;
    }

}