import { Input, Component, OnInit, OnDestroy, Output } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';



@Component({
    selector: 'd3s-admin-analytics-component',
    template: ` <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                            <d3s-admin-metric-asset-type-list></d3s-admin-metric-asset-type-list>
                        </div>
                    </div>
                <div>
                `
})

export class AdminAnalyticsComponent extends AdminBaseComponent implements OnInit, OnDestroy {

    constructor(
        secondaryNavService: SecondaryNavService,
        protected messagesService: MessagesObservableService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title) {
        super(headerBreadcrumbService, titleService, secondaryNavService);
        this.areaName = "Scoring";
        this.tabTitle = 'Scoring';
        this.setCommonItems();
        this.setCommonSecondaryNavTabs(false);

    }

    ngOnInit() {
    }

    ngOnDestroy() {
        this.clearSidebar();
    }


}