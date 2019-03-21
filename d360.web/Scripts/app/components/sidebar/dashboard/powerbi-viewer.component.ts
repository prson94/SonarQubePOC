import {Component, ElementRef, Input, OnChanges, SimpleChange} from '@angular/core';
import * as pbi from 'powerbi-client';

import {Dashboard, DashboardTokens} from '../../../models/dashboard.model'

import {DashboardService} from '../../../services/dashboard.service';
import {WebAnalyticsService} from '../../../services/web-analytics.service';

import {BaseComponent} from '../../shared/base.component';

@Component({
    selector: 'd3s-powerbi-viewer',
    templateUrl: './powerbi-viewer.component.html',
    providers: [DashboardService],
})

export class PowerBIViewerComponent extends BaseComponent implements OnChanges {
    @Input() dashboard: Dashboard;

    private powerBIDetails: DashboardTokens;
    private shouldRender: boolean = false;
    private report: pbi.Report = null;

    constructor(
        protected el: ElementRef,
        protected dashboardService: DashboardService,
        webAnalyticsService: WebAnalyticsService
    ) {
        super();
        this.webAnalyticsService = webAnalyticsService;
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.dashboard) {
            this.loadTokens();
        }
    }

    showFullscreen() {
        if (this.report) {
            this.report.fullscreen();
        }
    }

    showReport() {
        if (this.shouldRender) {
            this.shouldRender = false;

            let config = {
                type: 'report',
                accessToken: this.powerBIDetails.AccessToken,
                embedUrl: this.powerBIDetails.Report.embedUrl,
                tokenType: 1,
                id: this.powerBIDetails.Report.id,
                settings: {
                    filterPaneEnabled: true,
                    navContentPaneEnabled: true
                }
            };

            let reportContainer = <HTMLElement>document.getElementById('biContainer');

            let powerbi = new pbi.service.Service(pbi.factories.hpmFactory, pbi.factories.wpmpFactory, pbi.factories.routerFactory);
            this.report = <pbi.Report>powerbi.embed(reportContainer, config);

            var report = this.report;
            var objectId = this.dashboard.ObjectID;
            var object = this.dashboard.ObjectType;
            report.on('loaded', event => {
                report.getFilters()
                    .then(filters => {
                        var objectIdTable = "";
                        var objectTable = "";
                        for (let filter of filters) {
                            var target = <pbi.models.IFilterColumnTarget>filter.target;

                            if (!target) continue;
                            if (target.column == "ObjectID")
                                objectIdTable = target.table;
                            else if (target.column == "Object")
                                objectTable = target.table;
                        }

                        if (objectTable && objectIdTable) {
                            report.removeFilters();
                            var newFilters: pbi.models.IBasicFilter[] = [];
                            newFilters.push({
                                $schema: "http://powerbi.com/product/schema#basic",
                                target: {
                                    table: objectIdTable,
                                    column: "ObjectID"
                                },
                                operator: "In",
                                values: [objectId]
                            });
                            newFilters.push({
                                $schema: "http://powerbi.com/product/schema#basic",
                                target: {
                                    table: objectTable,
                                    column: "Object"
                                },
                                operator: "In",
                                values: [object]
                            });

                            report.setFilters(newFilters);
                        }
                    });
            });

            console.info("DEV: RENDERING POWER BI REPORT");
            this.logAction('open', 'Report', this.dashboard.ID);
        }
    }

    loadTokens() {
        this.isLoading = true;
        this.dashboardService.getPowerBIReportTokens(this.dashboard.PowerBIReportID).subscribe(
            result => {
                this.shouldRender = true; /* make sure only one call to power bi per load of this. */
                this.powerBIDetails = result;
                this.showReport();

                this.isLoading = false;
            }
        );
    }
}
