
import {Component, Input, OnChanges, SimpleChange, ViewChildren, ElementRef, AfterViewInit, QueryList} from '@angular/core';
import * as pbi from 'powerbi-client';
import { BaseComponent } from '../shared/base.component';
import { DashboardService, WebAnalyticsService } from '../../services/index';
import { Dashboard, DashboardTokens } from '../../models/dashboard.model'

@Component({
    selector: 'd3s-powerbi-viewer',  
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading" #biContainer style="height:75vh" class="powerbi"
                    powerbi-type="report"
                    [attr.powerbi-embed-url]="powerBIDetails?.Report?.embedUrl"
                    [attr.powerbi-access-token]="powerBIDetails?.AccessToken"
                ></div>
            `,
    providers: [DashboardService],        
})

export class PowerBIViewerComponent extends BaseComponent implements AfterViewInit, OnChanges {
    @Input() dashboard: Dashboard;
    @ViewChildren("biContainer") biContainer: QueryList<ElementRef>;
    private powerBIDetails: DashboardTokens;
    private shouldRender: boolean = false;

    constructor(protected el: ElementRef, protected dashboardService: DashboardService, webAnalyticsService: WebAnalyticsService) {
        super(undefined, webAnalyticsService);            
    }
    
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.dashboard)
            this.loadTokens();                
    }

    ngAfterViewInit() {
        this.biContainer.changes.subscribe(() => this.initPowerBi());                
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
                //console.log("DEV: RENDERING POWER BI REPORT");
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