import {Component, Input, Output, EventEmitter, OnChanges, SimpleChange, AfterViewInit, ViewChildren, QueryList, ElementRef, AfterContentInit} from '@angular/core';
import { BaseComponent } from '../base.component';
import { ScoreService } from '../../../services/score.service';
import { PointBreakdown, AverageScore } from '../../../models/score.model';
import { TreeNode } from 'primeng/api';
import * as Highcharts from 'highcharts';
import { ScoreType } from '../../../models/metrics.model';
import { ObjectHealthDetailsItemComponent } from './object-health-details-item.component';
import { ignoreElements } from 'rxjs/operators';
import { debug } from 'util';
import { SearchDetail } from '../../../models/search-result.model';
import { ObjectStatisticsService } from '../../../services/object-statistics.service';
import { Element } from '@angular/compiler';


@Component({
    selector: 'd3s-object-health-details',    
    templateUrl: `./object-health-details.component.html`,
    providers: [ScoreService, ObjectStatisticsService],
})

export class ObjectHealthDetailsComponent extends BaseComponent implements OnChanges{
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
    private showEmptyMessage: boolean = false;
    private searchDetails: SearchDetail;
    @ViewChildren(ObjectHealthDetailsItemComponent) OHDitems: QueryList<ObjectHealthDetailsItemComponent>;
    private showExpandAndCollapse: boolean = true;

    constructor(protected scoreService: ScoreService, protected objectStatisticsService: ObjectStatisticsService) {
        super();
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
            this.loadTypesAndLatestScore();
        }
    }
    private loadTypesAndLatestScore() {
        if (this.uid) {
            this.scoreService.getScoreTypes(this.uid).subscribe(x => {
                this.scoreTypes = x;
                if (x.length > 0) {
                    this.setSelectedButton(x[0])
                }
            });
            this.objectStatisticsService.getSearchDetails(this.uid).subscribe(
                result => {
                    this.searchDetails = result;
                    this.getCurrentScoreDateText();
                }
            );
        }
    }
    private loadSeriesData() {
        if (this.uid) {
            this.historicalData = [];
            this.isLoading = true;
            this.scoreService.getScoreHistory(this.selectedScoreType, this.uid)
                .subscribe(res => {
                    this.historicalData = res.map(val => {
                        return [Date.parse(val.Date), val.Score, this.getScoreType()];
                    });
                    this.getCurrentScoreDateText();
                    this.scoreHistory = {
                        chart: {
                            zoomType: 'x',
                            style: {
                                fontFamily: 'Source Sans Pro'
                            },
                            height: '240px',
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
                            labels: {
                                format: '{value}%'
                            },
                            gridLineWidth: 2,
                            min: 0,
                            max: 100,
                            tickInterval: 20,
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
                                        lineWidth: 4
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
                        tooltip: {
                            pointFormatter: function () {
                                var additionalValue = this.series.userOptions.data[this.index][2];
                                return '<span style="font-weight: bold">' + additionalValue + ' Score<span style="padding-left: 4px;font-weight: normal;">' + this.y + '%</span></span>';
                            },
                            headerFormat: '<span>{point.key}</span><br/>',
                            useHTML: 'true',
                            shape: 'square',
                            borderColor: '#c8cfd9',
                            borderWidth: 2,
                        },
                        series: [{
                            type: 'line',
                            name: 'Governance Score',
                            marker: {
                                enabled: false,
                                symbol: 'circle',
                                radius: 5,
                                states: {
                                    hover: {
                                        fillColor: 'white',
                                        lineColor: '#FF7155',
                                        lineWidth: 3
                                    }
                                }
                            },
                            data: this.historicalData,
                            color: '#FF7155'
                        }]
                    };
                    this.isLoading = false;
                });
        }
    }

    private loadPoints() {
        this.isLoading = true;
        if (this.uid) {
            this.scoreService.getPointBreakdown(this.uid, this.selectedScoreType, this.scoreDate)
            .subscribe(res => {
                this.pointBreakdown = res;
                this.isDQAndNoItems();
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

    private isDQAndNoItems() {
        if (this.pointBreakdown) {
            this.showEmptyMessage =  this.pointBreakdown.filter(x => { return x.ScoreType == ScoreType.DataQuality; }).length == 0
                && this.selectedScoreType == ScoreType.DataQuality;
        }
    }
    private loadDefinition() {
        if (this.uid) {
            this.scoreService.getScoreitemDetails(this.uid).subscribe(res => { this.scoreDefinition = res;});
            this.isLoading = false;
        }
    }
    hasAnyExpanders() {
        if (this.OHDitems) {
            this.showExpandAndCollapse = this.OHDitems.filter(x => {
                return x.expandable;
            }).length > 0;
        }
    }

    private setSelectedButton(scoreType: ScoreType) {
        switch (scoreType) {
            case ScoreType.Governance:
                this.showGovernanceScores = true;
                this.showDQScores = false;
                this.selectedScoreType = ScoreType.Governance;
                this.loadDefinition();
                this.loadSeriesData();
                this.loadPoints();
                this.isDQAndNoItems();
                break;
            case ScoreType.DataQuality:
                this.showGovernanceScores = false;
                this.showDQScores = true;
                this.selectedScoreType = ScoreType.DataQuality;
                this.loadDefinition();
                this.loadSeriesData();
                this.loadPoints();
                this.isDQAndNoItems();
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
        if (this.scoreTypes && this.scoreTypes.length > 0)
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
        else {
            return "Calculating...";
        }

    }

    getScoreType() {
        switch (this.selectedScoreType) {
            case ScoreType.Governance:
                return "Governance";
            case ScoreType.DataQuality:
                return "Data Quality";
            default:
                return "";
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
        let latestScore = score;
        if (this.searchDetails)
            latestScore = this.searchDetails.Scores.filter(x => { return x.ScoreType == ScoreType[this.selectedScoreType] }).length > 0 ?
                this.searchDetails.Scores.filter(x => { return x.ScoreType == ScoreType[this.selectedScoreType] })[0].Value : score;

        if (days == 0 || days == 1) {
            this.calculatedScoreText = "Your " + type + " Score changed to  <strong> " + this.getAsPrecentage(latestScore) + " </strong> today</strong>";
        }
        else if (days > 0 && days <= 90) {
            this.calculatedScoreText = "Your " + type + " Score has been <strong> " + this.getAsPrecentage(latestScore) + " </strong> for <strong>" + days + " days</strong>";
        }
        else if (days > 90 && days <= 780) {
            this.calculatedScoreText = "Your " + type + " Score has been <strong> " + this.getAsPrecentage(latestScore) + " </strong> for <strong>" + months + " months</strong>";
        }
        else if (days > 780) {
            this.calculatedScoreText = "Your " + type + " Score has been <strong> " + this.getAsPrecentage(latestScore) + " </strong> for <strong>" + years + " years</strong>";
        }
    }
    getAsPrecentage(val: number) {
        if (val == 0)
            return '0%';
        if (!val)
            return;
        if (val == 1)
            return '100%'
        let s = val + '0000';
        s = s.replace('0.', '');
        if (s.length > 6)
            s = (s.substr(0, 2)) + '.' + s[2] + "%";
        else
            s = (s.substr(0, 2)) + "%";
        if (s.startsWith('0'))
            s = s.substr(1, s.length);
        return s;
    }
}