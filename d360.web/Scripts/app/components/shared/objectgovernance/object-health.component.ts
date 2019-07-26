import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChange} from '@angular/core';
import { BaseComponent } from '../base.component';
import { ScoreService } from '../../../services/score.service';
import { PointBreakdown, AverageScore } from '../../../models/score.model';

declare var require: any;
const Highcharts = require('highcharts/highstock.src');


@Component({
    selector: 'd3s-object-health',    
    template: `            
            <table class="governance-value" (click)="toggleDetails()">
                <tr>
                    <td style="text-align:center;width:30px">
                        <i *ngIf="isTrend('up')" class="fa fa-arrow-circle-up governance-value-pass" aria-hidden="true" title="score trending up"></i>
                        <i *ngIf="isTrend('down')" class="fa fa-arrow-circle-down governance-value-fail" aria-hidden="true" title="score trending down"></i>
                    </td>                 
                    <td style="width:100px">
                        <chart *ngIf="score!=null;else noScoreBlock" [options]="scoreChart"></chart>
                        <ng-template #noScoreBlock><span>N/A</span></ng-template>
                    <td>
                    <td class="hide-on-med-and-down"><span class="title" style="vertical-align:top">Score</span></td>                    
                </tr>
            </table>
            <div *ngIf="!isLoading" class="governance-note">
                {{lastCalculatedMessage()}}
            </div>            
        `,
    providers: [ScoreService],    
})

export class ObjectHealthComponent extends BaseComponent implements OnChanges {    
    @Input() score: any = null;

    @Input() showDetails: boolean = false;    
    @Input() uid: string;
    @Input() objectID: number;
    @Input() objectType: string;
    @Output() showDetailsChange = new EventEmitter();

    private lastCalculatedDate: number;

    smallChart: Object;
    scoreChart: Object;

    averageScore: AverageScore;
    
    constructor(private scoreService: ScoreService) {
        super();
    }
        
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.objectType && this.objectID && this.uid) {
            this.loadSeriesData();
            this.loadScoreData();
        }
        
        if (this.score != null && changes['score']) {
            
            this.scoreChart = {

                chart: {
                    type: 'solidgauge',
                    backgroundColor: 'transparent',
                    height: 55,
                    width: 100,
                    spacingTop: 0,
                    spacingLeft: 0,
                    spacingRight: 0,
                    spacingBottom: 0
                },

                title: '',

                pane: {
                    center: ['50%', '85%'],
                    size: '150%',
                    startAngle: -90,
                    endAngle: 90,
                    background: {
                        backgroundColor: (Highcharts.theme && Highcharts.theme.background2) || '#EEE',
                        innerRadius: '80%',
                        outerRadius: '100%',
                        shape: 'arc',
                        borderColor: 'transparent'
                    }
                },

                tooltip: {
                    enabled: false
                },

                // the value axis
                yAxis: {
                    min: 0,
                    max: 100,
                    stops: [
                        [0.1, '#BC1B01'], // red
                        [0.5, '#FFB230'], // yellow
                        [0.9, '#02981B'] // green
                    ],
                    lineWidth: 0,
                    minorTickLength: 0,
                    tickLength: 100,
                    tickWidth: 4,
                    tickColor: 'transparent',
                    gridLineWidth: 0,
                    gridLineColor: 'transparent',
                    tickAmount: 2,
                    title: {
                        y: -70
                    },
                    labels: {
                        y: 16
                    }
                },

                plotOptions: {
                    solidgauge: {
                        innerRadius: '80%',
                        outerRadius: '100%',
                        dataLabels: {
                            y: 8,
                            borderWidth: 0,
                            useHTML: true,
                            style: {
                                fontFamily: '',
                                fontSize: '.9em',
                                color: '#646464'
                            }
                        }
                    }
                },
                credits: {
                    enabled: false
                },

                series: [{
                    data: [this.score],
                    dataLabels: {
                        format: '<div style="text-align:center">{y}%</div>',
                    }
                }],

            };     
        }
    }

    private toggleDetails() {        
        this.showDetails = !this.showDetails;        
        this.showDetailsChange.emit( this.showDetails );
    }
    
    private lastCalculatedMessage() {
        if (!this.lastCalculatedDate) {
            return "Governance Score not yet calculated";
        }
        
        var diff = new Date(Date.now() - this.lastCalculatedDate);

        var years = diff.getUTCFullYear() - 1970;

        if (years > 0) return "Governance Score last calculated " + years + " years ago.";
        
        var months = diff.getUTCMonth();

        if (months > 0) return "Governance Score last calculated " + months + " months ago.";
        
        var days = diff.getUTCDate() - 1;
                
        if (days > 0) return "Governance Score last calculated " + days + " days ago.";
                
        var hours = diff.getUTCHours();

        if (hours > 0) return "Governance Score last calculated " + hours + " hours ago.";

        var minutes = diff.getUTCMinutes();

        if (minutes > 0) return "Governance Score last calculated " + minutes + " minutes ago.";

        return "Governance Score last calculated a few seconds ago.";
    }

    private isTrend(direction: string): boolean{
        if (!this.averageScore || !this.score) return false;

        if (direction == 'up')
            return this.averageScore.AverageScore < (+this.averageScore.ObjectScore);

        if (direction == 'down')
            return this.averageScore.AverageScore > (+this.averageScore.ObjectScore);
    }

    private loadScoreData() {
        this.isLoading = true;
        this.scoreService.getAverageScore(this.uid).
            subscribe(res => {
                this.averageScore = res;
                this.isLoading = false;
            });
    }

    private loadSeriesData() {
        this.scoreService.getScoreHistory(this.uid).
            subscribe(res => {
                this.lastCalculatedDate = res.length > 0 ? Date.parse(res[res.length-1].Date) : null;
                let data = res.map(val => {
                    return [Date.parse(val.Date), val.Score];
                });
                                
                this.smallChart = {

                    chart: {
                        backgroundColor: 'transparent',
                        borderWidth: 0,
                        type: 'area',
                        margin: [2, 0, 2, 0],
                        width: 100,
                        height: 40,
                        style: {
                            overflow: 'visible'
                        },
                        skipClone: true
                    },
                    title: {
                        text: '',
                    },
                    credits: {
                        enabled: false
                    },
                    xAxis: {
                        type: 'datetime',
                        labels: {
                            enabled: false
                        },
                        title: {
                            text: null
                        },
                        startOnTick: false,
                        endOnTick: false,
                        tickPositions: []
                    },
                    yAxis: {
                        endOnTick: false,
                        startOnTick: false,
                        labels: {
                            enabled: false
                        },
                        title: {
                            text: null
                        },
                        tickPositions: [0]
                    },
                    legend: {
                        enabled: false
                    },
                    tooltip: {
                        backgroundColor: null,
                        borderWidth: 0,
                        shadow: false,
                        useHTML: true,
                        hideDelay: 0,
                        shared: true,
                        padding: 0,
                        positioner: function (w, h, point) {
                            return { x: point.plotX - w / 2, y: point.plotY - h };
                        }
                    },
                    plotOptions: {
                        series: {
                            animation: false,
                            lineWidth: 1,
                            shadow: false,
                            states: {
                                hover: {
                                    lineWidth: 1
                                }
                            },
                            marker: {
                                radius: 1,
                                states: {
                                    hover: {
                                        radius: 2
                                    }
                                }
                            },
                        },
                        column: {
                            negativeColor: '#910000',
                            borderColor: 'silver'
                        }
                    },
                    series: [{
                        type: 'area',
                        name: 'Governance Score',
                        data: data,
                        color: '#426A84',
                        marker: { enabled: false },
                    }]
                };
            });
    }
}