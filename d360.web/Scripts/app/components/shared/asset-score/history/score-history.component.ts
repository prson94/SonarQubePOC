import { Component, Input, OnChanges, ChangeDetectorRef, ViewChild, ElementRef, Output, SimpleChanges, EventEmitter, ViewEncapsulation } from '@angular/core';

import * as Highcharts from 'highcharts';
import { ObjectStatisticsService } from '../../../../services/object-statistics.service';
import { ScoreService } from '../../../../services/score.service';
import { ScoreType } from '../../../../models/metrics.model';
import { BaseComponent } from '../../base.component';
import { PointBreakdown, ScorePoint } from '../../../../models/score.model';


@Component({
    selector: 'score-history',
    templateUrl: `score-history.component.html`,
    styleUrls: ['score-history.less'],
    encapsulation: ViewEncapsulation.None,
    providers: [ScoreService, ObjectStatisticsService],
})
export class ScoreHistoryComponent extends BaseComponent implements OnChanges {
    @Input() scoreType: ScoreType = ScoreType.Governance;
    @Input() assetUid: string;
    @Input() selectedPoint: PointBreakdown;
    @Input() scoreDate: string;
    @Input() isExternallyCalculated: boolean;

    @Output() datePointChanged = new EventEmitter();
    @Output() measurePointsChanged = new EventEmitter();

    private scoresPoints: ScorePoint[] = [];
    private measurePoints: ScorePoint[];

    private historicalData: any[];
    private historicalMeasureData: any[];

    private tableHasVerticalScrollbar: boolean = false;

    private lastScorePoint: Date;
    private chartInstance: Highcharts.Chart;
    calculatedScoreText: string = 'Calculating...';
    scoreHistory: Highcharts.Options;

    private allLoadedPoints: any[];

    mainScoreGraphColor = '#9edae5';
    measureScoreGraphColor = '#d2edf4';

    private graphHash: string = '';
    private showMeasurePoints: boolean = false;
    private isHistoryLoaded: boolean = false;

    constructor(protected scoreService: ScoreService,
        protected objectStatisticsService: ObjectStatisticsService,
        private cdRef: ChangeDetectorRef
    ) {
        super();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes && (changes.selectedPoint || changes.scoreType || changes.assetUid)) {
            if (this.selectedPoint)
                this.measureChanged();
        }

        if (changes && (changes.scoreType || changes.assetUid)) {
            this.loadDataPoints();
        }
    }

    loadDataPoints() {
        this.isHistoryLoaded = false;
        this.scoreService.getAssetScoreGraphPoints(this.assetUid, this.scoreType).
            subscribe(res => {
                this.allLoadedPoints = res;
                this.scoresPoints = this.getDataForKey('score');
                this.drawGraph();
                this.isHistoryLoaded = true;
            });
    }

    private getDataForKey(key: string): any[] {
        let arr: ScorePoint[] = [];
        try {
            this.allLoadedPoints.forEach(dataSet => {
                if (dataSet['key'] == key) {
                    (dataSet.data as []).forEach(pt => {
                        var sp = new ScorePoint();
                        sp.EffectiveDate = pt['EffectiveDate'];
                        sp.Score = Math.round((+pt['Value'] * 100) * 10) / 10;
                        arr.push(sp);
                    })

                    arr = arr.sort(function (a, b) {
                        if (a.EffectiveDate > b.EffectiveDate) return -1;
                        if (a.EffectiveDate < b.EffectiveDate) return 1;
                    });

                    for (var i = 0; i < arr.length - 1; i++) {
                        if (arr[i].Score > arr[i + 1].Score)
                            arr[i].ScoreProgression = 1;

                        if (arr[i].Score < arr[i + 1].Score)
                            arr[i].ScoreProgression = -1;

                        if (arr[i].Score == arr[i + 1].Score)
                            arr[i].ScoreProgression = 0;

                    }

                    arr[arr.length - 1].ScoreProgression = 2;
                }
            })
        }
        catch (ex) {
            arr = [];
            console.warn(ex);
        }

        return arr;
    }

    private measureChanged() {
        this.drawGraph();
    }

    private refreshGraph() {
        this.graphHash = '';
        this.drawGraph();
    }
    public drawGraph() {

        if (this.scoresPoints.length <= 0)
            return;

        var currentGraphHash = this.assetUid + '_' + this.scoreType;
        if (this.selectedPoint) {
            currentGraphHash += '_' + this.selectedPoint.Uid;
        }

        if (currentGraphHash == this.graphHash)
            return;

        this.graphHash = currentGraphHash;
        if (this.chartInstance)
            this.chartInstance.destroy();

        this.lastScorePoint = new Date(this.scoresPoints[0].EffectiveDate);

        this.historicalData = this.scoresPoints.map(val => {
            return [Date.parse(val.EffectiveDate), val.Score, this.getScoreType()];
        });

        if (this.selectedPoint && this.selectedPoint.Uid) {
            this.measurePoints = this.getDataForKey(this.selectedPoint.Uid);
            this.historicalMeasureData = this.measurePoints.map(val => {
                return [Date.parse(val.EffectiveDate), val.Score, this.getScoreType()];
            });
        }

        // Adds arbitrary last point for current date.
        let currentDate = new Date(Date.now());
        currentDate = new Date(currentDate.getFullYear(), currentDate.getMonth(), currentDate.getDate());
        let currenDateMs = currentDate.getTime();
        if (currenDateMs > Date.parse(this.scoresPoints[0].EffectiveDate)) {
            this.historicalData.unshift(
                [currenDateMs, this.scoresPoints[0].Score, this.getScoreType()]
            );
            if (this.historicalMeasureData && this.historicalMeasureData.length > 0) {
                this.historicalMeasureData.unshift(
                    [currenDateMs, this.historicalMeasureData[0].Score, this.getScoreType()]
                );
            }
        }

        if (!this.showMeasurePoints || this.isExternallyCalculated) {
            this.historicalMeasureData = [];
            this.measurePointsChanged.emit([]);
        }
        else {
            this.measurePointsChanged.emit(this.measurePoints);
        }



        this.scoreHistory = {
            chart: {
                zoomType: 'xy',
                style: {
                    fontFamily: 'Source Sans Pro'
                },
                height: '250px'
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

                series: {
                    cursor: 'pointer',
                    step: 'right',
                    animationLimit: 0,
                    point: {
                        events: {
                            click: e => {
                                this.scoreDate = Highcharts.dateFormat('%Y-%m-%d', e.point.x);
                                this.selectPointOnGraph();
                                this.datePointChanged.emit(this.scoreDate);
                            }
                        }
                    },
                    animation: {
                        complete: function () {
                            if (this.selectPointOnGraph)
                                this.selectPointOnGraph();
                        }
                    }
                },
            },
            tooltip: {
                shared: true,
                pointFormatter: function () {

                    var additionalValue = this.series.userOptions.name;
                    return '<div><span>' + additionalValue + '<span style="padding-left: 4px;">' + this.y + '%</span></span></div>';
                },
                headerFormat: '<span>{point.key}</span><br/>',
                useHTML: true,
                shape: 'square',
                backgroundColor: 'white',
                borderColor: '#e4e4e4',
                borderWidth: 1,
                animation: false
            },
            series: [{
                type: 'area',
                name: this.getScoreType() + ' score',
                data: this.historicalData,
                color: this.mainScoreGraphColor,
                marker: {
                    enabled: false,
                    symbol: 'circle',
                    radius: 6,
                    states: {
                        hover: {
                            fillColor: '#81b3bd',
                            lineColor: '#2e2e2e',
                            lineWidth: 1
                        },
                        select: {
                            fillColor: '#81b3bd',
                            lineColor: '#2e2e2e',
                            lineWidth: 1
                        }
                    }
                }
            },
            {
                type: 'area',
                name: 'Measure score',
                data: this.historicalMeasureData,
                color: this.measureScoreGraphColor,
                marker: {
                    enabled: false,
                    symbol: 'circle',
                    radius: 6,
                    fillOpacity: 1,
                    states: {
                        hover: {
                            fillColor: '#afe1eb',
                            lineColor: '#2e2e2e',
                            lineWidth: 1,
                        },
                        select: {
                            fillColor: '#afe1eb',
                            lineColor: '#2e2e2e',
                            lineWidth: 1
                        }
                    }
                }
            }
            ]
        };

        this.chartInstance = Highcharts.chart('healthChart', this.scoreHistory);
        this.getCurrentScoreDateText();
    }

    private formatCalculatedScoreText(milliseconds: number, score: number) {
        var day = 1000 * 60 * 60 * 24;
        var days = Math.floor(milliseconds / day);
        var months = Math.floor(days / 31);
        var years = Math.floor(months / 12);
        let type = this.scoreType == ScoreType.Governance ? 'Governance ' : ' Data Quality';
        let latestScore = score;
        let hasEndDate: boolean = false;

        if (latestScore > 1) {
            latestScore /= 100;
        }

        let scorePercentage = this.getAsPrecentage(latestScore);

        let verb: string = hasEndDate ? 'was' : 'has been';
        if (days == 0 || days == 1) {
            this.calculatedScoreText = "Your " + type + " Score changed to  <strong> " + scorePercentage + " </strong> today</strong>";
        }
        else if (days > 0 && days <= 90) {
            this.calculatedScoreText = "Your " + type + " Score " + verb + " <strong> " + scorePercentage + " </strong> for <strong>" + days + " days</strong>";
        }
        else if (days > 90 && days <= 780) {
            this.calculatedScoreText = "Your " + type + " Score " + verb + " <strong> " + scorePercentage + " </strong> for <strong>" + months + " months</strong>";
        }
        else if (days > 780) {
            this.calculatedScoreText = "Your " + type + " Score " + verb + " <strong> " + scorePercentage + " </strong> for <strong>" + years + " years</strong>";
        }

        if (hasEndDate) {
            this.calculatedScoreText += " <span class='inactive'>(latest score is no longer active)</span>";
        }
    }

    getCurrentScoreDateText() {
        if (this.scoresPoints && this.scoresPoints.length > 0) {
            let mostRecent = Date.parse(this.scoresPoints[0].EffectiveDate);
            let milliseconds = new Date(Date.now()).getTime() - new Date(mostRecent).getTime();
            this.formatCalculatedScoreText(milliseconds, this.scoresPoints[0].Score);
        }
        else {
            return "Calculating...";
        }

    }

    getScoreType() {
        switch (this.scoreType) {
            case ScoreType.Governance:
                return "Governance";
            case ScoreType.DataQuality:
                return "Data Quality";
            default:
                return "";
        }
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

    ////scoring carousel, table and graph interactivity
    private selectPointOnGraph() {
        if (this.chartInstance) {
            if (this.chartInstance.series) {
                if (this.chartInstance.series.length > 0) {

                    var ms = new Date(this.scoreDate.toString()).getTime();
                    var idx = this.chartInstance.series[0].data.findIndex(p => { return p.x == ms });

                    if (idx == -1) {
                        idx = 1;
                    }

                    for (var i = 0; i < this.chartInstance.series[0].data.length; i++) {
                        this.chartInstance.series[0].data[i].select(false, true);
                    }
                    var point = this.chartInstance.series[0].data[idx];
                    if (point) {
                        this.scoreDate = Highcharts.dateFormat('%Y-%m-%d', point.x);
                        point.setState("select");
                    }

                    //now select measure point
                    if (this.selectedPoint) {
                        for (var i = 0; i < this.chartInstance.series[1].data.length; i++) {
                            this.chartInstance.series[1].data[i].select(false, true);
                        }
                        var point = this.chartInstance.series[1].data[idx];
                        if (point) {
                            point.setState("select");
                        }
                    }

                    this.cdRef.detectChanges();
                    this.cdRef.markForCheck();
                }
            }

        }
    }


    private scoreTableClick(item: ScorePoint) {
        this.scoreDate = item.EffectiveDate;
        this.tableSelectedIDX = this.scoresPoints.indexOf(item);
        this.selectPointOnGraph();
        this.datePointChanged.emit(this.scoreDate);
    }

    @ViewChild('scoreTable', { static: false }) scoreTable: ElementRef;
    private tableSelectedIDX: number = 0;
    ngAfterViewChecked() {
        this.selectPointOnGraph();
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

            var table = tblBody as HTMLElement;
            table.style.maxHeight = (window.innerHeight - this.scoreTable.nativeElement.getBoundingClientRect().top - 72) + 'px';

            this.tableHasVerticalScrollbar = table.scrollHeight > table.clientHeight;
        }
        this.cdRef.detectChanges();
    }

    private getMeasurePoint(item: ScorePoint): ScorePoint {
        return this.measurePoints.filter(x => x.EffectiveDate == item.EffectiveDate)[0];
    }
}
