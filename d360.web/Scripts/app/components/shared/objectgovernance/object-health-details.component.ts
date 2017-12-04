import {Component, Input, Output, EventEmitter, OnChanges, SimpleChange} from '@angular/core';
import { BaseComponent } from '../base.component';
import { ScoreService } from '../../../services/score.service';
import { PointBreakdown, AverageScore } from '../../../models/score.model';
import { TreeNode } from 'primeng/primeng';
declare var require: any;
const Highcharts = require('highcharts/highstock.src');

@Component({
    selector: 'd3s-object-health-details',    
    template: `
            <div class="row">
                <div class="col l6 m12 s12">
                    <header>Score History</header>
                    <chart [options]="scoreHistory"></chart>
                </div>
                <div class="col l6 m12 s12">
                    <div class="row">
                        <div class="col s12">
                            <header>Point Breakdown</header>
                            <p-treeTable  scrollable="true" scrollWidth="100%" [value]="pointBreakdownTree" selectionMode="single">                                
                                <p-column field="Name" header="Analytic" [style]="{'width':'250px'}"></p-column>                                
                                <p-column header="Value" [style]="{'width':'250px'}">
                                    <ng-template let-item="rowData" pTemplate type="body">
                                        <i *ngIf="item.data.Value" class="fa fa-check enabled" title="Passed"></i>
                                        <i *ngIf="!item.data.Value" class="fa fa-times disabled" title="Failed"></i>
                                    </ng-template>
                                </p-column>
                            </p-treeTable>  
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                    <div class="row">
                        <div class="col s12">
                            <header>Score</header>
                            <chart [options]="scorePie"></chart>
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
    scorePie: Object;
    
    private pointBreakdown: PointBreakdown[] = [];
    private pointBreakdownTree: TreeNode[] = [];

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
                        text:''
                    },                    
                    xAxis: {
                        type: 'datetime',
                        minTickInterval: (24 * 3600 * 1000),                    
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
                            marker: {
                                radius: 1
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
                this.scorePie = this.getKpi((+res.ObjectScore), 100 - (+res.ObjectScore), res.AverageScore, 100 - res.AverageScore, true);               
            });
    }


    private loadPoints() {
        this.isLoading = true;
        this.scoreService.getPointBreakdown(this.objectID, this.objectType)
            .then(res => {
                this.pointBreakdown = res;
                this.pointBreakdownTree = [];
                this.pointBreakdown.forEach(p => {
                    this.pointBreakdownTree.push({
                        data: p,
                        leaf: true
                    });
                });
                this.isLoading = false;
            });
    }


    private getKpi(score: number, remaining: number, average: number, remainingAvg: number, isPercent?: boolean) {        
        return {
            chart: {
                type: 'pie',
                backgroundColor: 'transparent',
                height: 300,
                width: 500
            },
            title: {
                text: null
            },         
            credits: {
                enabled: false
            },
            yAxis: {                
                max: 1.0                
            },
            plotOptions: {
                pie: {
                    shadow: false
                }
            },
            tooltip: {
                formatter: function () {
                    if (!this.point.name) return null;
                    return '<b>' + this.point.name + '</b>: ' + this.y + ' %';
                }
            },
            series: [{
                    name: 'Score',
                    data: [{ name: "Current Score", y: score, color: '#84745C' }, { name: "", y: remaining, color: "white" }],
                    showInLegend: false,
                    innerSize: '55%',
                    size: '80%',
                },
                {                    
                    size: '55%',
                    name: 'Average',
                    showInLegend: false,
                    data: [{ name: "Average Score", y: average, color: '#C4AC89' }, { name: "", y: remainingAvg, color: "white" }],
                }
            ]
        };
    }
}