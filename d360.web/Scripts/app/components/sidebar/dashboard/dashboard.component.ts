import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { DashboardService } from '../../../services/dashboard.service';
import { Dashboard } from '../../../models/dashboard.model'
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { TitleAndTabsService } from '../../../services/title-and-tabs.service';

@Component({
    selector: 'd3s-dashboard',
    templateUrl: './dashboard.component.html',
    providers: [DashboardService],
})

export class DashboardComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    dashboards: Dashboard[] = [];
    dashboard: Dashboard;
    selected: Dashboard;
    dashboardName: string;
    reportID: number;
    showSingle: boolean = false;
    showError: boolean;
    private folderTitle: string;

    constructor(
        protected dashboardService: DashboardService,
        private route: ActivatedRoute,
        private headerBreadcrumbService: HeaderBreadcrumbService,
        private titleAndTabsService: TitleAndTabsService,
        secondaryNavService: SecondaryNavService,
        private router: Router,
        breadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

    ngOnInit() {
        this.showSingle = false;
        this.sub = this.route.params.subscribe(params => {
            this.titleAndTabsService.initializeTitleAndTabsCheck(this.route.params, params, $localize`Dashboards`);

            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];
            this.dashboardName = params['name'];
            this.reportID = +params['reportID'];
            this.loadAvailableDashboards();
            if (this.objectType && !this.objectType.endsWith("Type")) {
                this.buildSecondaryNavigationForObject(this.objectID, this.objectType, this.buildBreadcrumb.bind(this));
            }
        });
    }


    private buildBreadcrumb(clearInfo: boolean) {

        this.headerBreadcrumbService.clearBreadcrumbs();

        this.headerBreadcrumbService.getFolderTitle('#Dashboards').then(res => {
            this.folderTitle = res
            let areaBreadcrumb = new Breadcrumb(
                this.folderTitle ? this.folderTitle : 'Dashboards',
                '/dashboard',
                false
            );
            this.headerBreadcrumbService.showBreadcrumb(areaBreadcrumb);

            if (this.selected) {
                let dashboardCrumb = new Breadcrumb(
                    this.selected.Name,
                    SiteUrlHelpers.getObjectUrl("Dashboard", this.selected.ID),
                    false
                );
                this.headerBreadcrumbService.showBreadcrumb(dashboardCrumb);
            }
            if (clearInfo) {
                this.headerBreadcrumbService.getFolderIcon(res).subscribe(icon => {
                    this.clearSidebar();
                    this.secondaryNavService.setCurrentArea(res, icon, $localize`Dashboards`);
                    this.secondaryNavService.clearCurrentObject();
                    this.secondaryNavService.clearButtons();
                    this.secondaryNavService.showHeader(false);
                });
            }

        });
    }

    ngOnDestroy() {
        this.secondaryNavService.resetSecondaryNavActiveItem();

        if (this.sub) {
            this.sub.unsubscribe();
        }
    }

    private loadAvailableDashboards() {
        this.isLoading = true;
        if (this.reportID > 0) {
            this.dashboardService.getDashboardByID(this.reportID).subscribe(
                result => {
                    if (result) {
                        this.selected = result;
                        this.showSingle = true;
                    }
                    else {
                        this.showError = true;
                    }

                    if (this.showSingle || this.objectType == undefined) {
                        this.buildBreadcrumb(true);
                    } else {
                        this.buildBreadcrumb(false);
                    }

                    this.isLoading = false;
                }
            );
        } else {
            this.dashboardService.getDashboards(this.objectID, this.objectType).subscribe(
                result => {
                    this.dashboards = result;
                    if (this.objectType && this.objectID && this.dashboardName) {
                        this.selected = this.dashboards[0];
                        this.showSingle = true;
                    }

                    if (this.showSingle || this.objectType == undefined) {
                        this.buildBreadcrumb(true);
                    } else {
                        this.buildBreadcrumb(false);
                    }

                    this.isLoading = false;
                }
            );
        }
    }

    setSelected(dashboard) {
        this.selected = dashboard;
        this.buildBreadcrumb(false);
    }
}
