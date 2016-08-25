///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ScoreService } from '../../services/index';
import { PointBreakdown, AverageScore } from '../../models/score.model';
import { Highcharts } from 'angular2-highcharts';

@Component({
    selector: 'd3s-object-health',
    template: `
            <header>Health</header>
            <div class="governance-value" [ngClass]="{'governance-value-fail':isFail(), 'governance-value-warning': isWarning(), 'governance-value-pass': isPass()}" (click)="toggleDetails()">
                <div class="row">
                    <div class="col l4 s12">
                        {{score}}%
                    </div>
                    <div class="col l8 s12">
                        <div style="width:120;height:50;">
                        <chart [options]="smallChart"></chart>
                        </div>
                    </div>
                </div>
            </div>
        `,
    providers: [ScoreService]
})

export class ObjectHealthComponent extends BaseComponent implements OnInit {    
    @Input() score: number = 0;

    @Input() showDetails: boolean = false;    
    @Input() objectID: number;
    @Input() objectType: string;
    @Output() showDetailsChange = new EventEmitter();

    smallChart: Object;

    constructor(private scoreService: ScoreService) {
        super();
    }

    ngOnInit() {
        this.loadSeriesData();
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


    private loadSeriesData() {
        this.scoreService.getScoreHistory(this.objectID, this.objectType).
            then(res => {
                let data = res.map(val => {
                    return [Date.parse(val.Date), val.Score];
                });

                this.smallChart = {

                    chart: {
                        backgroundColor: null,
                        borderWidth: 0,
                        type: 'area',
                        margin: [2, 0, 2, 0],
                        width: 120,
                        height: 50,
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
                    }]
                };
            });
    }

}