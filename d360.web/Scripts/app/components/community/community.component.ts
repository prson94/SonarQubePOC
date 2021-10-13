import { Component, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ResponsibilityTypeService } from '../../services/responsibility-type.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { ResourceResponsibilityTypeCount } from '../../models/responsibility-type.model';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import * as Highcharts from 'highcharts';
import { CompanySettingsService } from '../../services/settings.service';


@Component({
    selector: 'd3s-community-component',
    styles: [`
      chart {
        display: block;
      }
    `],
    template: `
        <div class="row">
            <div class="col l6 m12 s12">
                <div class="tile tile-detail">   
                    <header>User's Responsibilities</header>
                    <div *ngIf="isLoading">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    </div>
                    <div [hidden]="isLoading">
                        <div id="responsibilitiesPie"></div>                        
                    </div>
                </div>
            </div>
            <div class="col l6 m12 s12" *ngIf="selectedResponsibilityUid">
                <div class="tile tile-detail">  
                    <d3s-user-list [UserListHeading] = "GetHeadingLabel()" [ResponsibilityTypeUid]="selectedResponsibilityUid" [IsCommunityUserResposibility]="true" [(selected)]="selectedResource"></d3s-user-list>                    
                </div>
            </div>
            <div class="col s12" *ngIf="selectedResource">
                <div class="tile tile-detail">   
                   <d3s-resource-responsibility-tile [responsibilityTypeUid]="selectedResponsibilityUid" [resourceId]="selectedResource.ResourceID"></d3s-resource-responsibility-tile>
                </div>
            </div>
        </div>
         `,
    providers: [ResponsibilityTypeService],
})

export class CommunityComponent extends BaseComponent implements OnInit {
    responsibilitiesPie: Object;
    selectedResponsibilityUid: string = "";
    selectedResponsibilityName: string;
    selectedResource: any;

    constructor(protected responsibilityTypeService: ResponsibilityTypeService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Community');
        this.headerBreadcrumbService.getFolderTitle('#Community').then((res) => {
            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.clearCurrentObjectInfo();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(res));

            this.headerBreadcrumbService.getFolderIcon(res).subscribe(icon => {
                this.clearSidebar();
                this.secondaryNavService.setCurrentArea(res, icon, 'Community');
                this.secondaryNavService.clearCurrentObject();
            });
            this.secondaryNavService.showHeader(true);

        });
        this.load();
    }


    private load() {
        this.isLoading = true;
        this.responsibilityTypeService.getResponsibilityTypeBreakdown().
            subscribe(result => {    
                let options: any = {
                    chart: {
                        plotBackgroundColor: null,
                        plotBorderWidth: null,
                        plotShadow: false,
                        type: 'pie',
                        backgroundColor: 'transparent',
                        style: {
                            fontFamily: 'Source Sans Pro'
                        }
                    },
                    title: {
                        text: null
                    },
                    subtitle: {
                        text: 'Click on a pie piece for more details.'
                    },
                    credits: {
                        enabled: false
                    },
                    tooltip: {
                        formatter: function () {
                            return this.point.name + '<br>' + Highcharts.numberFormat(this.y, 0, '.', ',') + ' Total Assigned Items';
                        }
                    },
                    plotOptions: {
                        pie: {
                            allowPointSelect: true,
                            cursor: 'pointer',
                            dataLabels: {
                                enabled: true,
                                formatter: function () {
                                    return '<b>' + this.point.name + '</b>: ' + Highcharts.numberFormat(this.y, 0, '.', ',');
                                }
                            }
                        }
                    },
                    series: [{
                        name: 'Responsibilities',
                        colorByPoint: true,
                        data: result.map(x => ({
                            name: x.ResponsibilityType,
                            y: x.Count,
                            id: x.ResponsibilityTypeID,
                            uid: x.ResponsibilityTypeUID
                        })),
                        events: {
                            click: function (e) { this.onPieClick(e) }.bind(this)
                        }
                    }]
                };

                Highcharts.chart('responsibilitiesPie', options);

                this.isLoading = false;
            });
    }

    onPieClick(e) {
        this.selectedResource = null;
        this.selectedResponsibilityName = e.point.name; //name
        this.selectedResponsibilityUid = e.point.uid; // triggers user responsibilities piece to load.    
    }
    GetHeadingLabel() {
        return 'Users Assigned As ' + this.selectedResponsibilityName;
    }
}