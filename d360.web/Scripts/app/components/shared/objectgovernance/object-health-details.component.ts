import { Component, Input, OnChanges, SimpleChange, ViewChildren, QueryList, ChangeDetectorRef, ViewChild, ElementRef, AfterViewChecked, HostListener, } from '@angular/core';
import { BaseComponent } from '../base.component';
import { ScoreService } from '../../../services/score.service';
import { PointBreakdown, ScorePoint } from '../../../models/score.model';
import { TreeNode } from 'primeng/api';
import * as Highcharts from 'highcharts';
import { ScoreType } from '../../../models/metrics.model';
import { ObjectHealthDetailsItemComponent } from './object-health-details-item.component';
import { SearchDetail } from '../../../models/search-result.model';
import { ObjectStatisticsService } from '../../../services/object-statistics.service';



@Component({
    selector: 'd3s-object-health-details',
    templateUrl: `./object-health-details.component.html`,
    providers: [ScoreService, ObjectStatisticsService],
})

export class ObjectHealthDetailsComponent extends BaseComponent implements OnChanges, AfterViewChecked {
    @Input() uid: string;
    @Input() objectName: string;
    scoreHistory: Object;
    scoresPoints: ScorePoint[];
    lastScorePoint: Date;
    averageScore: number;
    scoreDate: string = null;
    private showGovernanceScores: boolean = true;
    private showDQScores: boolean = false;

    private historicalData: any[];
    private calculatedScoreText: string = 'Calculating...';
    private pointBreakdown: PointBreakdown[] = [];
    private ScoreType = ScoreType;
    private selectedScoreType = ScoreType.Governance;
    private scoreTypes: number[] = [];
    private showEmptyMessage: boolean = false;
    private searchDetails: SearchDetail;
    private handle: any;
    private loadingPoints: boolean = false;
    private loadingDefinition: boolean = false;
    private loadingHistory: boolean = false;
    @ViewChildren(ObjectHealthDetailsItemComponent) OHDitems: QueryList<ObjectHealthDetailsItemComponent>;
    private showExpandAndCollapse: boolean = true;

    constructor(protected scoreService: ScoreService,
        protected objectStatisticsService: ObjectStatisticsService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
        this.scoreDate = new Date().toDateString();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        let requiresLoad: boolean = false;
        for (let p in changes) {
            if (p == 'uid') {
                requiresLoad = (changes['uid'].currentValue != changes['uid'].previousValue) && changes['uid'] != undefined;
            }
        }
        if (requiresLoad) {
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
            this.loadingHistory = true;
            this.scoreService.getScoreHistory(this.selectedScoreType, this.uid)
                .subscribe(res => {

                    this.historicalData = res.map(val => {
                        return [Date.parse(val.EffectiveDate), val.Score, this.getScoreType()];
                    });

                    this.scoresPoints = null;
                    this.scoresPoints = res.sort(function (a, b) {
                        if (a.EffectiveDate > b.EffectiveDate) return -1;
                        if (a.EffectiveDate < b.EffectiveDate) return 1;
                    })

                    //handle non existing todays score point used for plotting graph
                    var nullEndDate = this.scoresPoints.filter(x => x.EndDate == null);
                    if (nullEndDate.length > 1)
                        this.scoresPoints.shift();

                    this.lastScorePoint = new Date(this.scoresPoints[0].EffectiveDate);

                    for (var i = 0; i < this.scoresPoints.length - 1; i++) {
                        if (this.scoresPoints[i].Score > this.scoresPoints[i + 1].Score)
                            this.scoresPoints[i].ScoreProgression = 1;

                        if (this.scoresPoints[i].Score < this.scoresPoints[i + 1].Score)
                            this.scoresPoints[i].ScoreProgression = -1;

                        if (this.scoresPoints[i].Score == this.scoresPoints[i + 1].Score)
                            this.scoresPoints[i].ScoreProgression = 0;

                    }

                    this.scoresPoints[this.scoresPoints.length - 1].ScoreProgression = 0;
                    this.getCurrentScoreDateText();
                    this.scoreHistory = {
                        chart: {
                            zoomType: 'x',
                            style: {
                                fontFamily: 'Source Sans Pro'
                            },
                            height: '240px'
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
                                allowPointSelect: true,
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
                                            this.selectPointOnGraph();
                                            this.loadPoints();
                                        }
                                    }
                                }
                            }
                        },
                        tooltip: {
                            pointFormatter: function () {

                                var additionalValue = this.series.userOptions.name;
                                return '<span style="font-weight: bold">' + additionalValue + '<span style="padding-left: 4px;font-weight: normal;">' + this.y + '%</span></span>';
                            },
                            headerFormat: '<span>{point.key}</span><br/>',
                            useHTML: 'true',
                            shape: 'square',
                            borderColor: '#c8cfd9',
                            borderWidth: 2
                        },
                        series: [{
                            type: 'line',
                            name: 'Governance Score',
                            marker: {
                                enabled: false,
                                symbol: 'circle',
                                radius: 9,
                                states: {
                                    hover: {
                                        fillColor: 'white',
                                        lineColor: '#FF7155',
                                        lineWidth: 3,
                                        opacity: 1
                                    },
                                    select: {
                                        fillColor: '#FF7155',
                                        lineColor: '#FF7155',
                                        lineWidth: 3
                                    }
                                }
                            },
                            data: this.historicalData,
                            color: '#FF7155'
                        }]
                    };
                    this.loadingHistory = false;
                });
        }
    }

    private loadPoints(isTabChange: boolean = false) {
        this.loadingPoints = true;
        if (this.uid) {
            this.scoreService.getPointBreakdown(this.uid, this.selectedScoreType, this.scoreDate)
                .subscribe(res => {
                    this.pointBreakdown = res;
                    this.isDQAndNoItems();
                     
                    if (isTabChange) {
                        this.scoreDate = new Date().toDateString();
                    }
                    this.loadingPoints = false;
                });
        }
    }

    private isDQAndNoItems() {
        if (this.pointBreakdown) {
            this.showEmptyMessage = this.pointBreakdown.filter(x => { return x.ScoreType == ScoreType.DataQuality; }).length == 0
                && this.selectedScoreType == ScoreType.DataQuality;
        }
    }

    hasAnyExpanders() {
        clearTimeout(this.handle);
        this.handle = window.setTimeout(() => {
            if (this.OHDitems && !this.loadingHistory && !this.loadingPoints) {
                this.showExpandAndCollapse = this.OHDitems.filter(x => {
                    return x.expandable;
                }).length > 0;
            }
        }, 100);
    }

    private setSelectedButton(scoreType: ScoreType) {
        switch (scoreType) {
            case ScoreType.Governance:
                this.showGovernanceScores = true;
                this.showDQScores = false;
                this.scoreDate = new Date().toDateString();
                this.selectedScoreType = ScoreType.Governance;
                this.loadSeriesData();
                this.loadPoints(true);
                this.isDQAndNoItems();
                break;
            case ScoreType.DataQuality:
                this.showGovernanceScores = false;
                this.showDQScores = true;
                this.scoreDate = new Date().toDateString();
                this.selectedScoreType = ScoreType.DataQuality;
                this.loadSeriesData();
                this.loadPoints(true);
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

        if (this.searchDetails) {
            let searchDetailRelevantScores = this.searchDetails.Scores.filter(x => { return x.ScoreType == ScoreType[this.selectedScoreType] });
            if (searchDetailRelevantScores.length > 0) {
                latestScore = searchDetailRelevantScores[0].Value;
            }
            searchDetailRelevantScores = null;
        }
        
        if (latestScore > 1) {
            latestScore /= 100;
        }

        let scorePercentage = this.getAsPrecentage(latestScore);

        if (days == 0 || days == 1) {
            this.calculatedScoreText = "Your " + type + " Score changed to  <strong> " + scorePercentage + " </strong> today</strong>";
        }
        else if (days > 0 && days <= 90) {
            this.calculatedScoreText = "Your " + type + " Score has been <strong> " + scorePercentage + " </strong> for <strong>" + days + " days</strong>";
        }
        else if (days > 90 && days <= 780) {
            this.calculatedScoreText = "Your " + type + " Score has been <strong> " + scorePercentage + " </strong> for <strong>" + months + " months</strong>";
        }
        else if (days > 780) {
            this.calculatedScoreText = "Your " + type + " Score has been <strong> " + scorePercentage + " </strong> for <strong>" + years + " years</strong>";
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

    private isSameDate(date1, date2) {
        var tempDate = new Date();
        var today = new Date(tempDate.getFullYear(), tempDate.getMonth(), tempDate.getDate());

        if (date1 && date2) {
            var d1 = new Date(date1.toString());
            var d2 = new Date(date2.toString());
            if (d2.getTime() == today.getTime() && d1.getTime() == this.lastScorePoint.getTime()) {
                return true;
            }

            return d1.getTime() === d2.getTime();
        }
        else return false;
    }


    //scoring carousel, table and graph interactivity
    private setSelectedPoint: boolean = false;
    private selectPointOnGraph() {
        var idx = this.getSelectedIndex();
        if (idx > -1) {

            for (var i = 0; i < this.chartInstance.series[0].data.length; i++) {
                this.chartInstance.series[0].data[i].select(false, true);
            }
            var point = this.chartInstance.series[0].data[this.getSelectedIndex()];
            if (point)
                point.select(true, true);
        }
        this.cdRef.detectChanges();
        this.cdRef.markForCheck();
    }

    private getSelectedIndex() {
        var item = null;
        if (!this.scoresPoints)
            return -1;

        this.scoresPoints.forEach(p => {
            var date = new Date(this.scoreDate.toString());
            var scoreDate = new Date(p.EffectiveDate.toString());
            if (date.toString() == scoreDate.toString()) {
                item = p;
            }
        });
        return this.scoresPoints.length - 1 - this.scoresPoints.indexOf(item);
    }

    private scoreTableClick(item: ScorePoint) {
        this.scoreDate = item.EffectiveDate;
        this.tableSelectedIDX = this.scoresPoints.indexOf(item);
        this.loadPoints();
    }

    private onCarouselScoreClick(item: ScorePoint) {
        this.scoreDate = item.EffectiveDate;
        this.loadPoints();
    }

    private chartInstance: Highcharts.Chart;
    getChartInstance(chartInstance) {
        this.chartInstance = chartInstance;
    }

    @ViewChild('scoreTable', { static: false }) scoreTable: ElementRef;
    private tableSelectedIDX: number = 0;
    ngAfterViewChecked() {
        this.selectPointOnGraph()
        //table autoscroll to selected item
        if (this.scoreTable) {
            var tblBody = (this.scoreTable.nativeElement as Element).querySelector('.body');
            var height = tblBody.clientHeight;

            for (var i = 0; i < tblBody.children.length - 1; i++) {
                var selected = tblBody.children[i].className.toLowerCase().indexOf('selected') > -1 ? tblBody.children[i] : null;

                if (!selected)
                    continue;

                if (selected && this.tableSelectedIDX != i) {
                    this.tableSelectedIDX = i;
                    var scrollFor = (tblBody.scrollTop + selected.getBoundingClientRect().top) - tblBody.getBoundingClientRect().top;
                    if (scrollFor < 0) {
                        tblBody.scrollTop = tblBody.scrollTop + Math.round(scrollFor);
                    }
                    else {
                        tblBody.scrollTop = (scrollFor - height) + selected.clientHeight;
                    }
                }
            }
        }
        this.cdRef.detectChanges();

    }
}