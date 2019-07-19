import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ResponsibilityTypeService } from '../../services/responsibility-type.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { ResponsibilityTypeCount, ResourceResponsibilityTypeCount } from '../../models/responsibility-type.model';
import { RightSidebarService } from '../../services/right-sidebar.service';

declare var require: any;
const Highcharts = require('highcharts/highstock.src');

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
                        <chart [options]="responsibilitiesPie">
                            <series (click)="onPieClick($event)">
                            </series>
                        </chart>
                    </div>
                </div>
            </div>
            <div class="col l6 m12 s12" *ngIf="selectedResponsibilityId">
                <div class="tile tile-detail">  
                    <d3s-community-responsibility-count [responsibilityTypeName]="selectedResponsibilityName" [responsibilityTypeId]="selectedResponsibilityId" [(selected)]="selectedResource"></d3s-community-responsibility-count>                    
                </div>
            </div>
            <div class="col s12" *ngIf="selectedResource">
                <div class="tile tile-detail">   
                   <d3s-resource-responsibility-tile [responsibilityTypeId]="selectedResponsibilityId" [resourceId]="selectedResource.ResourceID"></d3s-resource-responsibility-tile>
                </div>
            </div>
        </div>
         `,
    providers: [ResponsibilityTypeService],
})

export class CommunityComponent extends BaseComponent implements OnInit {
    private responsibilitiesPie: Object;
    private selectedResponsibilityId: number = 0;
    private selectedResponsibilityName: string;
    private selectedResource: ResourceResponsibilityTypeCount;

    constructor(protected responsibilityTypeService: ResponsibilityTypeService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        rightSidebarService: RightSidebarService
    ) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Community');
        this.headerBreadcrumbService.getFolderTitle('#Community').then((res) => {
            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.clearCurrentObjectInfo();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(res));
        });
        this.clearSidebar();
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.responsibilityTypeService.getResponsibilityTypeBreakdown().
            subscribe(result => {
                this.responsibilitiesPie = {
                    chart: {
                        plotBackgroundColor: null,
                        plotBorderWidth: null,
                        plotShadow: false,
                        type: 'pie',
                        backgroundColor: 'transparent',
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
                            return this.point.name +'<br>' + Highcharts.numberFormat(this.y, 0, '.', ',') +  ' Total Assigned Items';
                        }
                    },
                    plotOptions: {
                        pie: {
                            allowPointSelect: true,
                            cursor: 'pointer',
                            dataLabels: {
                                enabled: true,
                                formatter: function () {
                                    return '<b>' + this.point.name+'</b>: ' + Highcharts.numberFormat(this.y, 0, '.', ',');
                                },
                                style: {
                                    color: (Highcharts.theme && Highcharts.theme.contrastTextColor) || 'black'
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
                            id: x.ResponsibilityTypeID
                        })),
                    }]
                };

                this.isLoading = false;
            });
    }

    onPieClick(e) {
        this.selectedResource = null;
        this.selectedResponsibilityName = e.originalEvent.point.name; //name
        this.selectedResponsibilityId = e.originalEvent.point.id; // triggers user responsibilities piece to load.    
    }
};