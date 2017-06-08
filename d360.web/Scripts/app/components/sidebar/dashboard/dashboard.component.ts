import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { MessagesService } from '../../../services/messages.service';
import { DashboardService } from '../../../services/dashboard.service';
import { Dashboard } from '../../../models/dashboard.model'

@Component({
    selector: 'd3s-dashboard',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">  
                        <header>Dashboards for {{objectName}}</header>
                        <div class="row">
                            <div class="col s12">
                                <span style="padding:0 10px;">Dashboard:</span>
                                <select [(ngModel)]="dashboard" (change)="selected=null" style="width:300px;">
                                    <option></option>
                                    <option *ngFor="let dashboard of dashboards" [ngValue]="dashboard">{{dashboard.Name}}</option>
                                </select>                                
                                
                                <button pButton type="button" (click)="selected=dashboard;" label="Render" style="width: '150px';padding:4px;"></button>
                            </div>  
                            <div *ngIf="dashboard?.Description" class="col s12" [innerHtml]="dashboard?.Description"></div>                          
                        </div>                        
                    </div>
                    <div class="tile tile-detail" *ngIf="selected">
                        <d3s-powerbi-viewer *ngIf="selected.ReportType =='powerbi'" [dashboard]="selected"></d3s-powerbi-viewer>                        
                        <d3s-sagacity-viewer *ngIf="selected.ReportType =='sagacity'" [dashboard]="selected"></d3s-sagacity-viewer>
                    </div>
                    <div class="tile tile-detail" *ngIf="!selected">
                        <h4 class="center" style="padding:30px;">Please choose a dashboard from the dropdown above and press render to view the specified dashboards content.</h4>
                    </div>
                </div>
            </div>
        `,
    providers: [DashboardService],
})

export class DashboardComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    dashboards: Dashboard[] = [];
    dashboard: Dashboard;
    selected: Dashboard;

    constructor(protected dashboardService: DashboardService,
        private route: ActivatedRoute,
        private router: Router) {
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
        this.dashboardService.getDashboards(this.objectID, this.objectType)
            .then(result => {
                this.dashboards = result;
                this.isLoading = false;
            });
    }
}