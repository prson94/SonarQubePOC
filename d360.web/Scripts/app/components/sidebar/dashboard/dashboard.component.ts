import {Component, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {BaseComponent} from '../../shared/base.component';
import {DashboardService} from '../../../services/dashboard.service';
import {Dashboard} from '../../../models/dashboard.model'

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
    constructor(
        protected dashboardService: DashboardService,
        private route: ActivatedRoute,
        private router: Router
    ) {
        super();
    }

    ngOnInit() {
        this.showSingle = false;
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];
            this.dashboardName = params['name'];
            console.log(params);
            this.loadAvailableDashboards();
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
                }
                this.isLoading = false;
            }
        );
    }
}
