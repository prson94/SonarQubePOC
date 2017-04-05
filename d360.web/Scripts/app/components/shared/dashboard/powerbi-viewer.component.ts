import {Component, Input, OnChanges, SimpleChange, ViewChildren, ElementRef, AfterViewInit, QueryList} from '@angular/core';
import * as pbi from 'powerbi-client';
import { BaseComponent } from '../../shared/base.component';
import { DashboardService } from '../../../services/dashboard.service';
import { WebAnalyticsService } from '../../../services/web-analytics.service';
import { Dashboard, DashboardTokens } from '../../../models/dashboard.model'

@Component({
    selector: 'd3s-powerbi-viewer',  
    template: ` 
                <header>{{dashboard?.Name}}<d3s-tile-actions [hasFullScreen]="true" (fullScreenClick)="showFullscreen()"></d3s-tile-actions></header>
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>                        
                        <div *ngIf="!isLoading" #biContainer style="height:75vh" class="powerbi"
                                powerbi-type="report"
                                [attr.powerbi-embed-url]="powerBIDetails?.Report?.embedUrl"
                                [attr.powerbi-access-token]="powerBIDetails?.AccessToken"
                        ></div>
                    </div>
                </div>
            `,
    providers: [DashboardService],        
})

export class PowerBIViewerComponent extends BaseComponent implements AfterViewInit, OnChanges {
    @Input() dashboard: Dashboard;
    @ViewChildren("biContainer") biContainer: QueryList<ElementRef>;
    private powerBIDetails: DashboardTokens;
    private shouldRender: boolean = false;

    

    constructor(protected el: ElementRef, protected dashboardService: DashboardService, webAnalyticsService: WebAnalyticsService) {
        super();            
        this.webAnalyticsService = webAnalyticsService;
    }
    
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.dashboard)
            this.loadTokens();                
    }

    ngAfterViewInit() {
        this.biContainer.changes.subscribe(() => this.initPowerBi());            
    }

    showFullscreen() {
        if (this.biContainer) {
            var report = window.powerbi.get(this.biContainer.first.nativeElement);

            report.fullscreen();
        }
    }

    initPowerBi() {
        if (this.biContainer && this.biContainer.length > 0 && this.shouldRender) {            
            if (!this.biContainer.first)
                console.log("ERROR: FIRST BICONTAINER ELEMENT IS NULL!");
            else if (!this.biContainer.first.nativeElement)
                console.log("ERROR: FIRST BICONTAINER NATIVE ELEMENT IS NULL!");
            else {
                this.shouldRender = false;
                window.powerbi.embed(this.biContainer.first.nativeElement);
                console.log("DEV: RENDERING POWER BI REPORT");
                this.logAction('open', 'Report', this.dashboard.ID);
            }
        }
    }

    loadTokens() {        
        this.isLoading = true;
        this.dashboardService.getPowerBIReportTokens(this.dashboard.PowerBIReportID)
            .then(result => { 
                this.shouldRender = true;    // make sure only one call to power bi per load of this.           
                this.powerBIDetails = result;      
                this.isLoading = false;
            });
    }
}