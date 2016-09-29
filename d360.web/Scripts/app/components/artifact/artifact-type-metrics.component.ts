import { Input, Component, OnInit } from '@angular/core';
import { ArtifactTypeService } from '../../services/index';
import { BaseComponent} from '../shared/base.component';
import { ArtifactTypeStatusCount, ArtifactTypeUsedVsUnusedResponsibility } from '../../models/artifact-type.model';
import { Highcharts } from 'angular2-highcharts';

@Component({
    selector: 'd3s-artifact-type-metrics',
    template: `     
                <d3s-loading [isLoading]="isLoading"></d3s-loading>            
                <div class="row" *ngIf="!isLoading">                    
                    <div class="col s12 m12 l6">                        
                        <div class="tile tile-detail">                            
                            <header>Responsibilities Assigned</header>
                            <chart [options]="responsibilitiesBar"></chart>                                                 
                        </div>
                    </div>
                    <div class="col s12 m12 l6">                        
                        <div class="tile tile-detail">                            
                            <header>Status Breakdown</header>                                                      
                            <chart [options]="statusPie"></chart> 
                        </div>
                    </div>
                </div>
                `,
    providers: [ArtifactTypeService],
})

export class ArtifactTypeMetricsComponent extends BaseComponent implements OnInit {
    @Input() objectID: number;
    @Input() objectName: string;
    @Input() objectType: string;

    private responsibilitiesBar: Object;
    private statusPie: Object;

    private status: ArtifactTypeStatusCount[] = [];
    private responsibilities: ArtifactTypeUsedVsUnusedResponsibility[] = [];

    constructor(private artifactTypeService: ArtifactTypeService) {
        super();
    }

    ngOnInit() {        
        this.loadStatusPie();
        this.loadResponsibilityBar();
    }

    private loadResponsibilityBar() {
        this.isLoading = true;
        this.artifactTypeService.getArtifactTypeUsedVsUnusedResponsibilities(this.objectID)
            .then(result => {
                this.responsibilities = result;
                this.isLoading = false;

                this.responsibilitiesBar = {
                    chart: {
                        type: 'column',
                        backgroundColor: 'transparent',                        
                    },
                    title: {
                        text: null
                    },
                    xAxis: {
                        categories: this.responsibilities.map(x => x.Responsibility)
                    },
                    yAxis: {
                        min: 0,
                        title: {
                            text: '# Items'
                        },
                        stackLabels: {
                            enabled: true,
                            style: {
                                fontWeight: 'bold',
                                color: (Highcharts.theme && Highcharts.theme.textColor) || 'gray'
                            }
                        }
                    },
                    credits: {
                        enabled: false
                    },
                    legend: {
                        align: 'right',
                        x: -30,
                        verticalAlign: 'top',
                        y: 25,
                        floating: true,
                        backgroundColor: (Highcharts.theme && Highcharts.theme.background2) || 'white',
                        borderColor: '#CCC',
                        borderWidth: 1,
                        shadow: false
                    },
                    tooltip: {
                        headerFormat: 'Responsibility - <b>{point.x}</b><br/>',
                        pointFormat: '{series.name}: {point.y}<br/>Total: {point.stackTotal}'
                    },
                    plotOptions: {
                        column: {
                            stacking: 'normal',
                            dataLabels: {
                                enabled: true,
                                color: (Highcharts.theme && Highcharts.theme.dataLabelsColor) || 'white'
                            }
                        }
                    },
                    series: [{
                        name: 'Unassigned',
                        data: this.responsibilities.map(x => x.UnassignedCount)
                    }, {
                            name: 'Assigned',
                            data: this.responsibilities.map(x => x.AssignedCount)
                        }]
                };
            });
    }

    private loadStatusPie() {
        this.isLoading = true;
        this.artifactTypeService.getArtifactTypeStatus(this.objectID)
            .then(result => {
                this.status = result;

                this.statusPie = {
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
                    credits: {
                        enabled: false
                    },
                    tooltip: {
                        pointFormat: '{series.name}: <b>{point.y} - {point.percentage:.1f}%</b><br>'
                    },
                    plotOptions: {
                        pie: {
                            allowPointSelect: true,
                            cursor: 'pointer',
                            dataLabels: {
                                enabled: false
                            },
                            showInLegend: true
                        }
                    },
                    series: [{
                        name: 'Status',
                        colorByPoint: true,
                        data: this.status.map(x => ({
                            name: x.Status,
                                y: x.Count
                        })),                     
                    }]
                };


                this.isLoading = false;
            });           
    }    
};