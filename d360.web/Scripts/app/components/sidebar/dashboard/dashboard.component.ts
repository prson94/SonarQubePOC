import {Component, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {BaseComponent} from '../../shared/base.component';
import {DashboardService} from '../../../services/dashboard.service';
import {Dashboard} from '../../../models/dashboard.model'
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { RightSidebarService } from '../../../services/right-sidebar.service';

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
    showSingle: boolean = false;

    private folderTitle: string;

    constructor(
        protected dashboardService: DashboardService,
        private route: ActivatedRoute,
        private headerBreadcrumbService: HeaderBreadcrumbService,
        rightSidebarService: RightSidebarService,
        private router: Router
    ) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        this.showSingle = false;
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];
            this.dashboardName = params['name'];
            this.loadAvailableDashboards();
           
        });
    }


    private buildBreadcrumb(clearInfo: boolean) {

        this.headerBreadcrumbService.clearBreadcrumbs();

        this.headerBreadcrumbService.getFolderTitle('#Dashboards').then(res => {
            this.folderTitle = res
            let areaBreadcrumb = new Breadcrumb(
                this.folderTitle ? this.folderTitle:'Dashboards',
                '/dashboard',
                false
            );
            this.headerBreadcrumbService.showBreadcrumb(areaBreadcrumb);

            if (this.selected) {
                let dashboardCrumb = new Breadcrumb(
                    this.selected.Name, 
                    SiteUrlHelpers.getObjectUrl("Dashboard",this.selected.ObjectID,null,this.selected.Name),
                    false
                );
                this.headerBreadcrumbService.showBreadcrumb(dashboardCrumb);
            }
            console.log(clearInfo);
            if (clearInfo) {
                this.headerBreadcrumbService.getFolderIcon(res).then(icon => {
                        this.clearSidebar();
                        this.rightSidebarService.setCurrentArea(res, icon, 'Dashboards');
                        this.rightSidebarService.clearCurrentObject();
                        this.rightSidebarService.clearButtons();
                        this.rightSidebarService.showHeader(false);
                });
            }

        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    private loadAvailableDashboards() {
        this.isLoading = true;
        this.dashboardService.getDashboards(this.objectID, this.objectType).subscribe(
            result => {
                this.dashboards = result;
                if (this.objectType && this.objectID && this.dashboardName) {
                    this.selected = this.dashboards[0];
                    this.showSingle = true;
                    this.buildBreadcrumb(true);
                } else {
                    this.buildBreadcrumb(false);
                }
                this.isLoading = false;
            }
        );
    }

    setSelected(dashboard) {
        this.selected = dashboard;
        this.buildBreadcrumb(false);
    }
}
