
import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChange} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ScoreService } from '../../services/index';
import { PointBreakdown, AverageScore } from '../../models/score.model';
import { Highcharts } from 'angular2-highcharts';

@Component({
    selector: 'd3s-object-health',
    template: `
            <!--header>Health</header-->
            <table class="governance-value" [ngClass]="{'governance-value-fail':isFail(), 'governance-value-warning': isWarning(), 'governance-value-pass': isPass()}" (click)="toggleDetails()">
                <tr>
                    <td>
                        {{scoreValue()}}
                        <i *ngIf="isTrend('up')" class="fa fa-arrow-circle-up governance-value-pass" aria-hidden="true" title="score trending up"></i>
                        <i *ngIf="isTrend('down')" class="fa fa-arrow-circle-down governance-value-fail" aria-hidden="true" title="score trending down"></i>
                    </td>
                    <td><chart [options]="smallChart"></chart></td>
                    <td class="title">&nbsp;</td>
                </tr>
            </table>
            <div *ngIf="!isLoading" class="governance-note">
                {{lastCalculatedMessage()}}
            </div>
        `,
    providers: [ScoreService],    
})

export class ObjectHealthComponent extends BaseComponent implements OnInit, OnChanges {    
    @Input() score: any = 0;

    @Input() showDetails: boolean = false;    
    @Input() objectID: number;
    @Input() objectType: string;
    @Output() showDetailsChange = new EventEmitter();

    private lastCalculatedDate: number;

    smallChart: Object;

    averageScore: AverageScore;

   

    constructor(private scoreService: ScoreService) {
        super();
    }

    ngOnInit() {
        
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.objectType && this.objectID) {
            this.loadSeriesData();
            this.loadScoreData();
        }
    }

    private isWarning(): boolean {
        return this.score < 80 && this.score > 60;
    }

    private isPass(): boolean {
        return this.score > 80;
    }

    private isFail(): boolean {
        return this.score < 60;
    }

    private toggleDetails() {        
        this.showDetails = !this.showDetails;        
        this.showDetailsChange.emit( this.showDetails );
    }

    private scoreValue() {
        if (this.score) return this.score + '%';
        return 'N/A';
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
        this.scoreService.getAverageScore(this.objectID, this.objectType).
            then(res => {
                this.averageScore = res;
                this.isLoading = false;
            });
    }

    private loadSeriesData() {
        this.scoreService.getScoreHistory(this.objectID, this.objectType).
            then(res => {
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
                        height: 30,
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
                           // fillOpacity: 0.25
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