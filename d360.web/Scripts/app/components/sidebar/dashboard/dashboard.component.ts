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

    constructor(
        protected dashboardService: DashboardService,
        private route: ActivatedRoute,
        private router: Router
    ) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];

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

                this.isLoading = false;
            }
        );
    }
}
