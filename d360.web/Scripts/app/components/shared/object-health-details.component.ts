///<reference path="../../es6-shim.d.ts"/>
import {Component, Input, Output, EventEmitter, OnChanges, SimpleChange} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ScoreService } from '../../services/index';
import { PointBreakdown, AverageScore } from '../../models/score.model';
import { Highcharts } from 'angular2-highcharts';

@Component({
    selector: 'd3s-object-health-details',
    template: `
            <div class="row">
                <div class="col l6 s12">
                    <header>Score History</header>
                    <chart [options]="scoreHistory"></chart>
                </div>
                <div class="col l6 s12">
                    <div class="row">
                        <div class="col s12">
                            <header>Point Breakdown</header>
                            <p-dataTable  scrollable="true" scrollWidth="100%" [value]="pointBreakdown" selectionMode="single">                                
                                <p-column field="Name" header="Analytic" [style]="{'width':'250px'}"></p-column>                                
                                <p-column header="Score" [style]="{'width':'250px'}">
                                    <template let-col let-data="rowData">
                                        <span>{{data.Score}} out of {{data.MaxScore}}</span>
                                    </template>
                                </p-column>
                            </p-dataTable>  
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s6">
                            <header>Average Score</header>
                            <chart [options]="scoreAverage"></chart>
                        </div>                        
                    </div>
                </div>
            </div>
            
        `,
    providers: [ScoreService],
})

export class ObjectHealthDetailsComponent extends BaseComponent implements OnChanges{
    @Input() objectID: number;
    @Input() objectType: string;
    @Input() objectName: string;

    scoreHistory: Object;
    scoreAverage: Object;
    scoreCurrent: Object;

    private pointBreakdown: PointBreakdown[] = [];

    constructor(protected scoreService: ScoreService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        let requiresLoad: boolean = false;
        for (let p in changes) {
            if (p == 'objectType') {
                requiresLoad = changes['objectType'].currentValue != changes['objectType'].previousValue;
            }
            if (p == 'objectID') {
                requiresLoad = changes['objectID'].currentValue != changes['objectID'].previousValue;
            }
        }

        if (requiresLoad) {
            this.loadPoints();
            this.loadSeriesData();
            this.loadScores();
        }
    }

    private loadSeriesData() {
        this.scoreService.getScoreHistory(this.objectID, this.objectType).
            then(res => {
                let data = res.map(val => {
                    return [Date.parse(val.Date), val.Score];
                });

                this.scoreHistory = {
                    
                    chart: {
                        zoomType: 'x'
                    },
                    title: {
                      //  text: 'Score History'
                        text:''
                    },
                    /*subtitle: {
                        text: document.ontouchstart === undefined ?
                            'Click and drag in the plot area to zoom in' : 'Pinch the chart to zoom in'
                    },*/
                    xAxis: {
                        type: 'datetime'
                    },
                    yAxis: {
                        title: {
                            text: 'Governance Score'
                        },
                        min: 0,
                    },
                    credits: {
                        enabled: false
                    },
                    legend: {
                        enabled: false
                    },
                    plotOptions: {
                        area: {
                            /*fillColor: {
                                linearGradient: {
                                    x1: 0,
                                    y1: 0,
                                    x2: 0,
                                    y2: 1
                                },
                                stops: [
                                    [0, Highcharts.getOptions().colors[0]],
                                    [1, Highcharts.Color(Highcharts.getOptions().colors[0]).setOpacity(0).get('rgba')]
                                ]
                            },*/
                            marker: {
                                radius: 2
                            },
                            lineWidth: 1,
                            states: {
                                hover: {
                                    lineWidth: 1
                                }
                            },
                            threshold: null
                        }
                    },

                    series: [{
                        type: 'area',
                        name: 'Governance Score',
                        data: data,
                        color: '#426A84'
                    }]
                };
            });        
    }

    private loadScores() {
        this.scoreService.getAverageScore(this.objectID, this.objectType)
            .then(res => {
                this.scoreAverage = this.getKpi("Average Score", res.AverageScore, 100 - res.AverageScore, true);
               // this.scoreCurrent = this.getKpi("Score", (-res.ObjectScore), 100 - (-res.ObjectScore), true);
            });
    }


    private loadPoints() {
        this.isLoading = true;
        this.scoreService.getPointBreakdown(this.objectID, this.objectType)
            .then(res => {
                this.pointBreakdown = res;
                this.isLoading = false;
            });
    }


    private getKpi(title: string, score: number, remaining: number, isPercent?: boolean) {
        console.log(score);
        console.log(remaining);
        return {
            chart: {
                type: 'pie'
            },
            title: {
                text: score + (isPercent ? '%' : ''),
                align: 'center',
                verticalAlign: 'middle',
                //y: 40
            },
            credits: {
                enabled: false
            },
            yAxis: {
                title: {
                    text: 'Total percent market share'
                }
            },
            plotOptions: {
                pie: {
                    shadow: false
                }
            },
            tooltip: {
                formatter: function () {
                    if (!this.point.name) return '';
                    return '<b>' + this.point.name + '</b>: ' + this.y + ' %';
                }
            },
            series: [{
                name: 'Score',
                data: [{ name: "Score", y: score }, { name: "", y: remaining, color: "white" }],
                size: '50%',
                innerSize: '80%',
                showInLegend: false,
                dataLabels: {
                    enabled: false
                }
            }]
        };
    }
}