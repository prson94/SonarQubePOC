///<reference path="../../es6-shim.d.ts"/>
import {Component, Input, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { MessagesService, DashboardService } from '../../services/index';
import { Dashboard } from '../../models/dashboard.model'
import { PowerBIViewerComponent } from './powerbi-viewer.component';
import {Button} from 'primeng/primeng';

@Component({
    selector: 'd3s-dashboard-tab',
    template: `
            <div *ngIf="isLoading" style="width:100%; text-align:center;">
                <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
            </div>
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
                            <div class="col s12" [innerHtml]="dashboard?.Description"></div>                          
                        </div>                        
                    </div>
                    <div class="tile tile-detail" *ngIf="selected">
                        <header>{{selected?.Name}}</header>
                        <div class="row">
                            <div class="col s12">
                                <d3s-powerbi-viewer [dashboard]="selected"></d3s-powerbi-viewer>
                            </div>
                        </div>                    
                    </div>
                    <div class="tile tile-detail" *ngIf="!selected">
                        <h4 class="center" style="padding:30px;">Please choose a dashboard from the dropdown above and press render to view the specified dashboards content.</h4>
                    </div>
                </div>
            </div>
        `,
    providers: [DashboardService],
    directives: [PowerBIViewerComponent, Button],
})

export class DashboardTabComponent extends BaseComponent implements OnInit {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;

    dashboards: Dashboard[] = [];
    dashboard: Dashboard;
    selected: Dashboard;
 //   openedDashboards: MenuItem[] = [];

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

  /*  private showDashboard(event, item: Dashboard) {
        console.log(item);
    }

    private selectDashboard(selected) {
        var selectedArray = this.dashboards.filter(x => x.ID == selected.value);

        if (selectedArray.length != 1) return;

        var item = selectedArray[0];
                
        this.openedDashboards.push({
            label: item.Name, command: (event) => {
                this.showDashboard(event,item);
            }
        });
    }*/
}