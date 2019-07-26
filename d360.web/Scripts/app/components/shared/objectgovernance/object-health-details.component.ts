import {Component, Input, Output, EventEmitter, OnChanges, SimpleChange, AfterViewInit} from '@angular/core';
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
                                <ng-template pTemplate="header">
	                                <tr>
		                                <th  style="width:60%;text-align:left">Analytic</th>
		                                <th style="width:20%;text-align:right">Value</th>
		                                <th style="width:20%;text-align:right">Adjusted Weight</th>
	                                </tr>
                                </ng-template>
                                <ng-template pTemplate="body" let-rowNode let-item="rowData">
	                                <tr [ttSelectableRow]="rowNode">
		                                <td  style="width:60%;">
			                                <d3s-treeTableToggler [rowNode]="rowNode"></d3s-treeTableToggler>
			                                <span *ngIf="!item.IsGroup" [innerText]="item.Name"></span>
                                            <b *ngIf="item.IsGroup"><span [innerText]="item.Name"></span></b>
		                                </td>
                                        <td  style="width:20%;text-align:right">
                                            <span *ngIf="!item.IsGroup">
                                                <i *ngIf="item.Value" class="fa fa-check enabled" title="Passed"></i>
                                                <i *ngIf="!item.Value" class="fa fa-times disabled" title="Failed"></i>
                                            </span>
                                        </td>
                                        <td style="width:20%;text-align:right">
                                            <span [innerText]="item.Weight"></span>
                                        </td>
	                                </tr>
                                </ng-template>
                            </p-treeTable>  
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </div>
            </div>
            
        `,
    providers: [ScoreService],
})

export class ObjectHealthDetailsComponent extends BaseComponent implements OnChanges, AfterViewInit{
    @Input() uid: string;
    @Input() objectName: string;

    scoreHistory: Object;
    averageScore: number;
    scoreDate: Date = null;
    
    private pointBreakdown: PointBreakdown[] = [];
    private pointBreakdownTree: TreeNode[] = [];

    constructor(protected scoreService: ScoreService) {
        super();
    }

    ngAfterViewInit(): void {
        this.loadPoints();
        this.loadSeriesData();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        let requiresLoad: boolean = false;
        for (let p in changes) {
            if (p == 'uid') {
                requiresLoad = changes['uid'].currentValue != changes['uid'].previousValue;
            }
        }
        if (requiresLoad) {
            this.loadPoints();
            this.loadSeriesData();
        }
    }

    private loadSeriesData() {
        if (this.uid) {
            this.scoreService.getAverageScore(this.uid)
            .subscribe(res => {
                this.averageScore = (res == null || res.AverageScore == null) ? 0 : res.AverageScore;
                this.scoreService.getScoreHistory(this.uid)
                    .subscribe(res => {
                        let data = res.map(val => {
                            return [Date.parse(val.Date), val.Score];
                        });

                        this.scoreHistory = {
                            chart: {
                                zoomType: 'x'
                            },
                            title: {
                                text: ''
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
                                plotLines: [{
                                    value: this.averageScore,
                                    color: '#6b5a51',
                                    dashStyle: 'solid',
                                    width: 2,
                                    label: {
                                        text: 'Average Score'
                                    }
                                }
                                ]
                            },
                            credits: {
                                enabled: false
                            },
                            legend: {
                                enabled: false
                            },
                            plotOptions: {
                                line: {
                                    marker: {
                                        radius: 1
                                    },
                                    lineWidth: 2,
                                    states: {
                                        hover: {
                                            lineWidth: 3
                                        }
                                    },
                                    threshold: null
                                },
                                series: {
                                    cursor: 'pointer',
                                    point: {
                                        events: {
                                            click: e => {
                                                this.scoreDate = Highcharts.dateFormat('%Y-%m-%d', e.point.x);
                                                this.loadPoints();
                                            }
                                        }
                                    }
                                }
                            },
                            series: [{
                                type: 'line',
                                name: 'Governance Score',
                                data: data,
                                color: '#426A84'
                            }]
                        };
                    });
            }) 
        }
    }

    private loadPoints() {
        this.isLoading = true;
        if (this.uid) {
            this.scoreService.getPointBreakdown(this.uid, this.scoreDate)
            .subscribe(res => {
                this.pointBreakdown = res;
                this.pointBreakdownTree = [];

                let tree = (node: any) => {
                    let childItems = this.pointBreakdown.filter(p => p.ParentUid == node.data.Uid && p.ParentUid != null);

                    node.leaf = true;
                    node.children = null;

                    if (childItems != null && childItems.length > 0) {

                        node.leaf = false;
                        node.children = [];

                        childItems.forEach(c => {

                            var child = {
                                data: c,
                                expanded: true,
                                leaf: true
                            };

                            tree(child);

                            node.children.push(child);
                        });
                    }
                };

                this.pointBreakdown.filter(p => !p.ParentUid).forEach(p => {
                    var root = {
                        data: p,
                        leaf: false,
                        expanded: true,
                        children: []
                    };

                    tree(root);
                    this.pointBreakdownTree.push(root);
                });

                //console.log(this.pointBreakdownTree);
                this.isLoading = false;
            });
        }
    }
}