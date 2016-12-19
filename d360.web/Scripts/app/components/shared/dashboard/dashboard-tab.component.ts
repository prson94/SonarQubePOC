import { Component, Input, OnInit} from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MessagesService } from '../../../services/messages.service';
import { DashboardService } from '../../../services/dashboard.service';
import { Dashboard } from '../../../models/dashboard.model'

@Component({
    selector: 'd3s-dashboard-tab',
    template: `
            <d3s-loading [isLoading]="isLoading"></d3s-loading>
            <div class="row" *ngIf="!isLoading">
                <div class="col s12">
                    <div class="tile tile-detail">  
                        <header>Dashboards for {{objectName}}</header>
                        <div class="row">
                            <div class="col s12">
                                <span style="padding:0 10px;">Dashboard:</span>
                                <select [(ngModel)]="dashboard" style="width:300px;">
                                    <option></option>
                                    <option *ngFor="let dashboard of dashboards" [ngValue]="dashboard">{{dashboard.Name}}</option>
                                </select>                                
                                
                                <button pButton type="button" (click)="selected=dashboard;" label="Render" style="width: '150px';padding:4px;"></button>
                            </div>  
                            <div *ngIf="dashboard?.Description" class="col s12" [innerHtml]="dashboard?.Description"></div>                          
                        </div>                        
                    </div>
                    <div class="tile tile-detail" *ngIf="selected">
                        <d3s-powerbi-viewer [dashboard]="selected"></d3s-powerbi-viewer>                        
                    </div>
                    <div class="tile tile-detail" *ngIf="!selected">
                        <h4 class="center" style="padding:30px;">Please choose a dashboard from the dropdown above and press render to view the specified dashboards content.</h4>
                    </div>
                </div>
            </div>
        `,
    providers: [DashboardService],
})

export class DashboardTabComponent extends BaseComponent implements OnInit {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;

    dashboards: Dashboard[] = [];
    dashboard: Dashboard;
    selected: Dashboard;

    constructor(protected dashboardService: DashboardService) {
        super();
    }    

    ngOnInit() {
        this.loadAvailableDashboards();
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