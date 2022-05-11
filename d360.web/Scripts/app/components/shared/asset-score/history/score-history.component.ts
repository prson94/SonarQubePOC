import { Component, Input, OnChanges, ChangeDetectorRef, ViewChild, ElementRef, Output, SimpleChanges, EventEmitter, ViewEncapsulation, Inject, LOCALE_ID } from '@angular/core';

import * as Highcharts from 'highcharts';
import { ObjectStatisticsService } from '../../../../services/object-statistics.service';
import { ScoreService } from '../../../../services/score.service';
import { ScoreType } from '../../../../models/metrics.model';
import { BaseComponent } from '../../base.component';
import { PointBreakdown, ScorePoint } from '../../../../models/score.model';
import { DatePipe } from '@angular/common';
import { CompanySettingsService } from '../../../../services/settings.service';


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

    scoresPoints: ScorePoint[] = [];
    private measurePoints: ScorePoint[];

    private historicalData: any[];
    private historicalMeasureData: any[];

    tableHasVerticalScrollbar: boolean = false;

    private lastScorePoint: Date;
    private chartInstance: Highcharts.Chart;
    calculatedScoreText: string = $localize`Calculating...`;
    scoreHistory: Highcharts.Options;

    private allLoadedPoints: any[];

    mainScoreGraphColor = '#9edae5';
    measureScoreGraphColor = '#d2edf4';

    private graphHash: string = '';
    showMeasurePoints: boolean = false;
    isHistoryLoaded: boolean = false;

    constructor(
        protected objectStatisticsService: ObjectStatisticsService,
        protected scoreService: ScoreService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef,
        private datePipe: DatePipe,
        @Inject(LOCALE_ID) private locale: string
    ) {
        super(settingsService);
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes && (changes.selectedPoint || changes.assetUid)) {
            if (this.selectedPoint)
                this.measureChanged();
        }

        if (changes && (changes.scoreType || changes.assetUid)) {
            this.loadDataPoints();
        }
    }

    loadDataPoints() {
        this.isHistoryLoaded = false;
        this.allLoadedPoints = [];
        this.scoresPoints = [];
        this.scoreService.getAssetScoreGraphPoints(this.assetUid, this.scoreType).
            subscribe((res) => {
                this.allLoadedPoints = res;
                this.scoresPoints = this.getDataForKey('score');
                this.drawGraph();
                this.isHistoryLoaded = true;
            });
    }

    private getDataForKey(key: string): any[] {
        let arr: ScorePoint[] = [];
        try {
            let measureAdjustmentRatio = 1;
            if (this.selectedPoint && this.selectedPoint._groupDisplayMaxWeight) {
                measureAdjustmentRatio = this.selectedPoint._groupDisplayMaxWeight;
            }
            this.allLoadedPoints.forEach((dataSet) => {
                if (dataSet['key'] == key) {
                    (dataSet.data as []).forEach((pt) => {
                        var sp = new ScorePoint();
                        sp.EffectiveDate = pt['EffectiveDate'];
                        var score = (+pt['Value'] * measureAdjustmentRatio * 1000) / 1000;
                        sp.Score = Math.round(score * 100 * 10) / 10;
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


        this.loadState();

        this.historicalData = [];
        this.historicalMeasureData = [];

        this.lastScorePoint = new Date(this.scoresPoints[0].EffectiveDate);

        this.historicalData = this.scoresPoints.map((val) => {
            return [Date.parse(val.EffectiveDate), val.Score, this.getScoreType()];
        });

        if (this.selectedPoint && this.selectedPoint.Uid) {
            this.measurePoints = this.getDataForKey(this.selectedPoint.Uid);
            this.historicalMeasureData = this.measurePoints.map((val) => {
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
                    [currenDateMs, this.historicalMeasureData[0][1], this.getScoreType()]
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

        var historicalTempData = this.historicalData;
        var datePipeRef = this.datePipe;
        var locale = this.locale;

        this.scoreHistory = {
            chart: {
                zoomType: 'xy',
                style: {
                    fontFamily: 'Precisely'
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
                    animation: false
                },
            },
            tooltip: {
                shared: true,
                headerFormat: '',
                footerFormat: '',
                formatter: function () {
                    var tooltipString = '';
                    var startIdx = historicalTempData.findIndex(x => x.x == this.points[0].x);
                    this.points.forEach((point) => {
                        tooltipString += `<div><span>${point.series.userOptions.name}<span style="padding-left: 4px;">${point.y}%</span></span></div>`;
                    });



                    var startDate = datePipeRef.transform(new Date(historicalTempData[startIdx].x), 'shortDate', locale);
                    var endDate = '';
                    if (startIdx == 0) {
                        endDate = 'present';
                    }
                    else {
                        endDate = datePipeRef.transform(new Date(historicalTempData[startIdx - 1].x), 'shortDate', locale);
                    }

                    tooltipString += `<div><span>Effective ${startDate} to ${endDate}</span></div>`;

                    return tooltipString;
                },
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
                opacity: 1,
                fillOpacity: 1,
                marker: {
                    enabled: false,
                    symbol: 'circle',
                    radius: 6,
                    states: {
                        select: {
                            fillColor: '#81b3bd',
                            lineColor: '#2e2e2e',
                            lineWidth: 1
                        },
                        hover: {
                            fillColor: '#81b3bd',
                            lineColor: '#2e2e2e',
                            lineWidth: 1
                        },
                    }
                }
            },
            {
                type: 'area',
                name: 'Measure score',
                data: this.historicalMeasureData,
                color: this.measureScoreGraphColor,
                opacity: 1,
                fillOpacity: 1,
                marker: {
                    enabled: false,
                    symbol: 'circle',
                    radius: 6,
                    fillOpacity: 1,
                    states: {
                        select: {
                            fillColor: '#afe1eb',
                            lineColor: '#2e2e2e',
                            lineWidth: 1
                        },
                        hover: {
                            fillColor: '#81b3bd',
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
        let type = this.scoreType == ScoreType.Governance ? $localize`Governance` : $localize`Data Quality`;
        let latestScore = score;
        let hasEndDate: boolean = false;

        if (latestScore > 1) {
            latestScore /= 100;
        }

        let scorePercentage = this.getAsPrecentage(latestScore);

        let verb: string = hasEndDate ? $localize`was` : $localize`has been`;
        if (days == 0 || days == 1) {
            this.calculatedScoreText = $localize`Your ${type} Score changed to <strong> ${scorePercentage} </strong> today</strong>`;
        }
        else if (days > 0 && days <= 90) {
            this.calculatedScoreText = $localize`Your ${type} Score ${verb} <strong> ${scorePercentage} </strong> for <strong>${days} days</strong>`;
        }
        else if (days > 90 && days <= 780) {
            this.calculatedScoreText = $localize`Your ${type} Score ${verb} <strong> ${scorePercentage} </strong> for <strong>${months} months</strong>`;
        }
        else if (days > 780) {
            this.calculatedScoreText = $localize`Your ${type} Score ${verb} <strong> ${scorePercentage} </strong> for <strong>${years} years</strong>`;
        }

        if (hasEndDate) {
            var subText = $localize`latest score is no longer active`;
            this.calculatedScoreText += " <span class='inactive'>(" + subText + ")</span>";
        }
    }

    getCurrentScoreDateText() {
        if (this.scoresPoints && this.scoresPoints.length > 0) {
            let mostRecent = Date.parse(this.scoresPoints[0].EffectiveDate);
            let milliseconds = new Date(Date.now()).getTime() - new Date(mostRecent).getTime();
            this.formatCalculatedScoreText(milliseconds, this.scoresPoints[0].Score);
        }
        else {
            return $localize`Calculating...`;
        }

    }

    getScoreType() {
        switch (this.scoreType) {
            case ScoreType.Governance:
                return $localize`Governance`;
            case ScoreType.DataQuality:
                return $localize`Data Quality`;
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
                    var idx = this.chartInstance.series[0].data.findIndex((p) => { return p.x == ms });

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
        var point = null;
        if (this.measurePoints)
            point = this.measurePoints.filter(x => x.EffectiveDate == item.EffectiveDate)[0];

        if (point == null || point == undefined) {
            return null;
        }
        else return point;
    }

    private getStorageKey() {
        return 'scoring_checkbox_storage_' + this.assetUid;
    }

    saveState() {
        var data = {};
        data['showMeasurePoints'] = this.showMeasurePoints;
        localStorage.setItem(this.getStorageKey(), JSON.stringify(data));
    }

    loadState() {
        var data = JSON.parse(localStorage.getItem(this.getStorageKey()));
        if (data) {
            if (data['showMeasurePoints']) {
                this.showMeasurePoints = data['showMeasurePoints'] as boolean;
            }
        }
    }
}
