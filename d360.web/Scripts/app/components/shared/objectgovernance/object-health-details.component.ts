import {Component, Input, Output, EventEmitter, OnChanges, SimpleChange, AfterViewInit, ViewChildren, QueryList} from '@angular/core';
import { BaseComponent } from '../base.component';
import { ScoreService } from '../../../services/score.service';
import { PointBreakdown, AverageScore } from '../../../models/score.model';
import { TreeNode } from 'primeng/api';
import * as Highcharts from 'highcharts';
import { ScoreType } from '../../../models/metrics.model';
import { ObjectHealthDetailsItemComponent } from './object-health-details-item.component';


@Component({
    selector: 'd3s-object-health-details',    
    templateUrl: `./object-health-details.component.html`,
    providers: [ScoreService],
})

export class ObjectHealthDetailsComponent extends BaseComponent implements OnChanges, AfterViewInit{
    @Input() uid: string;
    @Input() objectName: string;
    scoreHistory: Object;
    averageScore: number;
    scoreDate: string = null;
    private showGovernanceScores: boolean = true;
    private showDQScores: boolean = false;

    private historicalData: any[];
    private calculatedScoreText: string = 'Calculating...';
    private pointBreakdown: PointBreakdown[] = [];
    private pointBreakdownTree: TreeNode[] = [];
    private scoreDefinition: any;
    private ScoreType = ScoreType;
    private selectedScoreType = ScoreType.Governance;
    private scoreTypes :number[] = [];

    @ViewChildren(ObjectHealthDetailsItemComponent) OHDitems: QueryList<ObjectHealthDetailsItemComponent>;

    constructor(protected scoreService: ScoreService) {
        super();
    }

    ngAfterViewInit(): void {
        this.loadPoints();
        this.loadSeriesData();
        this.loadDefinition();
        this.loadTypes();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        let requiresLoad: boolean = false;
        for (let p in changes) {
            if (p == 'uid') {
                requiresLoad = (changes['uid'].currentValue != changes['uid'].previousValue) && changes['uid'] != undefined;
            }
        }
        if (requiresLoad) {
            this.isLoading = true;
            this.loadPoints();
            this.loadSeriesData();
            this.loadDefinition();
            this.loadTypes();
        }
    }
    private loadTypes() {
        if (this.uid) {
            this.scoreService.getScoreTypes(this.uid).subscribe(x => {
                this.scoreTypes = x;
                if (x.length > 0)
                    this.selectedScoreType = x[0];
            });
        }
    }
    private loadSeriesData() {
        if (this.uid) {
            this.scoreService.getScoreHistory(this.selectedScoreType, this.uid)
                .subscribe(res => {
                    this.historicalData = res.map(val => {
                        return [Date.parse(val.Date), val.Score, val.];
                    });
                    this.getCurrentScoreDateText();
                    this.scoreHistory = {
                            chart: {
                                zoomType: 'x',
                                style: {
                                    fontFamily: 'Source Sans Pro'
                                }
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
                                    text: ''
                                },
                                min: 0,
                                max:100
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
                                    lineWidth: 4,
                                    states: {
                                        hover: {
                                            lineWidth: 6
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
                                data: this.historicalData,
                                color: '#FF7155'
                            }]
                        };
                });
        }
    }

    private loadPoints() {
        this.isLoading = true;
        if (this.uid) {
            this.scoreService.getPointBreakdown(this.uid, this.selectedScoreType, this.scoreDate)
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

                this.isLoading = false;
            });
        }
    }
     
    private loadDefinition() {
        if (this.uid) {
            this.scoreService.getScoreitemDetails(this.uid).subscribe(res => { this.scoreDefinition = res; });
            this.isLoading = false;
        }
    }

    private setSelectedButton(scoreType: ScoreType) {
        switch (scoreType) {
            case ScoreType.Governance:
                this.showGovernanceScores = true;
                this.showDQScores = false;
                this.selectedScoreType = ScoreType.Governance;
                this.loadSeriesData();
                break;
            case ScoreType.DataQuality:
                this.showGovernanceScores = false;
                this.showDQScores = true;
                this.selectedScoreType = ScoreType.DataQuality;
                this.loadSeriesData();
                break;
            default:
        }
    }
    private setCollapsed(val: boolean) {
        if (this.OHDitems && this.OHDitems.length > 0) 
            this.OHDitems.forEach(x => { x.setCollapsed(val); })
    }
    private isAllCollapsed() {
        if (this.OHDitems && this.OHDitems.length > 0) {
            let any = this.OHDitems.filter(x => { return !x.isCollapsed; });
            if (any && any.length > 0)
                return false;
            else 
                return true;
        }
    }
    private hasAnyScoreType(scoreType: ScoreType) {
        return this.scoreTypes.indexOf(scoreType) !== -1;
    }

    private getCurrentScoreDateText() {
        if (this.historicalData && this.historicalData.length > 0) {
            let dataArray = [...this.historicalData];
            dataArray.sort((a, b) => b[0] - a[0]);
            
            let mostRecent = dataArray.splice(0, 1)[0];
            let lastchangedDate = this.getLastChangedDate(dataArray, mostRecent);     
            let milliseconds = Math.floor((new Date(mostRecent[0])).getTime() - (new Date(lastchangedDate[0]).getTime()));
            this.formatCalculatedScoreText(milliseconds, mostRecent[1]);
        }

    }
    private getLastChangedDate(tempArr: any[], mostRecent: any): any {
        if (tempArr.length > 0) {
            var nextLatest = tempArr.splice(0, 1)[0];
            if (mostRecent[1] == nextLatest[1]) {
                return this.getLastChangedDate(tempArr, nextLatest);
            } else {
                return mostRecent;
            }
        } else
            return mostRecent;
    }

    private formatCalculatedScoreText(milliseconds: number, score: number) {
        var day = 1000 * 60 * 60 * 24;
        var days = Math.floor(milliseconds / day);
        var months = Math.floor(days / 31);
        var years = Math.floor(months / 12);
        let type = this.selectedScoreType == ScoreType.Governance ? 'Governance ' : ' Data Quality';
        if (days == 0 || days == 1) {
            this.calculatedScoreText = "Your " + type +" Score changed to  <strong> " + score + "% </strong> today</strong>";
        }
        else if (days > 0 && days <= 90) {
            this.calculatedScoreText = "Your " + type +" Score has been <strong> " + score + "% </strong> for <strong>" + days + " days</strong>";
        }
        else if (days > 90 && days <= 780) {
            this.calculatedScoreText = "Your " + type +" Score has been <strong> " + score + "% </strong> for <strong>" + months + " months</strong>";
        }
        else if (days > 780) {
            this.calculatedScoreText = "Your " + type +" Score has been <strong> " + score + "% </strong> for <strong>" + years + " years</strong>";
        }
    }
}