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
                                <ng-template pTemplate="header">
	                                <tr>
		                                <th  style="width:60%;text-align:left">Analytic</th>
		                                <th style="width:20%;text-align:right">Value</th>
		                                <th style="width:20%;text-align:right">Weight</th>
	                                </tr>
                                </ng-template>
                                <ng-template pTemplate="body" let-rowNode let-item="rowData">
	                                <tr [ttSelectableRow]="rowNode">
		                                <td  style="width:60%;">
			                                <d3s-treeTableToggler [rowNode]="rowNode"></d3s-treeTableToggler>
			                                <span *ngIf="item.MapID" [innerText]="item.Name"></span>
                                            <b *ngIf="!item.MapID"><span [innerText]="item.Name"></span></b>
		                                </td>
                                        <td  style="width:20%;text-align:right">
                                            <span *ngIf="item.MapID">
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

export class ObjectHealthDetailsComponent extends BaseComponent implements OnChanges{
    @Input() objectID: number;
    @Input() objectType: string;
    @Input() objectName: string;

    scoreHistory: Object;
    averageScore: number;
    scoreDate: string = null;
    
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
        }
    }

    private loadSeriesData() {
        this.scoreService.getAverageScore(this.objectID, this.objectType)
            .then(res => {
                this.averageScore = (res == null || res.AverageScore == null) ? 0 : res.AverageScore;
            })
            .then(() => this.scoreService.getScoreHistory(this.objectID, this.objectType))
            .then(res => {
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
    }

    private loadPoints() {
        this.isLoading = true;
        this.scoreService.getPointBreakdown(this.objectID, this.objectType, this.scoreDate)
            .then(res => {

                this.pointBreakdown = res;
                this.pointBreakdownTree = [];

                let tree = (node: any) => {
                    let childGroups = this.pointBreakdown.filter(p => p.ParentID == node.data.GroupID && p.ID == null && p.ParentID != null); //any relevant groups
                    let childScores = this.pointBreakdown.filter(p => p.GroupID == node.data.GroupID && p.ID != null);


                    node.leaf = true;
                    node.children = null;

                    //console.log('childGroups', childGroups);
                    if (childScores != null && childScores.length > 0) {

                        node.leaf = false;
                        node.children = [];

                        childScores.forEach(c => {
                            node.children.push({
                                data: c,
                                expanded: true,
                                leaf: true
                            });
                        });
                    }


                    if (childGroups != null && childGroups.length > 0) {
                        node.leaf = false;
                        if (node.children == null)
                            node.children = [];

                        childGroups.forEach(c => {
                            var child = {
                                data: c,
                                expanded: true
                            }
                            tree(child);
                            node.children.push(child);
                        });
                    }

                };

                this.pointBreakdown.filter(p => p.ID == null && p.ParentID == null).forEach(p => {
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