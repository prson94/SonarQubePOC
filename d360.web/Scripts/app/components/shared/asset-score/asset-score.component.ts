import { Component, Input, OnChanges, SimpleChange, ChangeDetectorRef, AfterViewChecked, ViewEncapsulation, ViewChildren } from '@angular/core';
import { BaseComponent } from '../base.component';
import { ScoreService } from '../../../services/score.service';
import { PointBreakdown, ScorePoint } from '../../../models/score.model';
import { MetricFieldTypeViewModel, ScoreType, ScoreTypeAllocation } from '../../../models/metrics.model';
import { Observable, Subject } from 'rxjs';
import { SelectItem } from 'primeng/api';
import { MetricsService } from '../../../services/metrics.service';
import { AssetService } from '../../../services/asset.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-asset-score',
    templateUrl: `./asset-score.component.html`,
    styleUrls: ['asset-score.less'],
    encapsulation: ViewEncapsulation.None,
    providers: [ScoreService, MetricsService, AssetService],
})
export class AssetScoreComponent extends BaseComponent implements OnChanges, AfterViewChecked {
    @Input() uid: string;
    @Input() objectName: string;    
    assetTypeUid: string;

    assetTypeName: string = "";
    assetName: string = "";

    scoresPoints: ScorePoint[];
    measurePoints: ScorePoint[];

    scoresPointsDDL: SelectItem[];
    scoresPointsShowMeasure: boolean = false;
    scoresPointSelected: SelectItem;
    scorePointsMaxHeight: number = 200;
    panelHeight: number = 200;
    sidePanelHeight: number = 200;

    averageScore: number;
    scoreDate: string = null;
    showGovernanceScores: boolean = true;
    showDQScores: boolean = false;

    pointBreakdown: PointBreakdown[] = [];
    selectedPoint: PointBreakdown;

    private selectedMeasureUid = '';

    ScoreType = ScoreType;
    selectedScoreType = ScoreType.Governance;
    allocationUid: string = "";
    private scoreTypes: number[] = [];
    private allocationData: ScoreTypeAllocation[] = [];
    showEmptyMessage: boolean = false;
    
    fields: MetricFieldTypeViewModel[];

    showExpandAndCollapse: boolean = true;
    totalScore: number;
    totalScoreBadgeStyle: string = 'positive';
    lowerThreshold: number = 0;
    upperThreshold: number = 0;
    activeTab: string = 'History';
    isDataLoaded: boolean = false;

    selectedMetric: any;
    isExternallyCalculated: boolean = false;
    dropdownClassName: string = 'scoring-picker-dropdown';

    private headerMenu = [
        {
            "title": "Expand All",
            "callback": () => this.setCollapsed(false)
        },
        {
            "title": "Collapse All",
            "callback": () => this.setCollapsed(true)
        }

    ]

    constructor(
        protected assetService: AssetService,
        protected metricService: MetricsService,
        protected scoreService: ScoreService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef
    ) {
        super(settingsService);
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

    private selectScoreItem(item: PointBreakdown) {
        this.pointBreakdown.forEach((x) => {
            x._isSelected = false;
            if (x.Measures) {
                x.Measures.forEach(m => m._isSelected = false);
            }
        });
        item._isSelected = true;
        this.selectedPoint = JSON.parse(JSON.stringify(item));
        this.selectedMeasureUid = this.selectedPoint.Uid;
        this.setCurrentDefinition();
    }

    private setCurrentDefinition() {
        if (!this.selectedPoint || !this.allocationData) return;

        this.allocationData.forEach(alloc => {
            if (alloc['metricDefinition']) {
                var arr = alloc['metricDefinition'] as any[];
                arr.forEach(metric => {
                    if (metric['Uid'] == this.selectedPoint.Uid) {
                        this.selectedMetric = null;
                        var effDate = new Date(metric.EffectiveDate);
                        var selectedDate = new Date(this.scoreDate);
                        if (effDate < selectedDate) {
                            this.selectedMetric = metric;
                            this.selectedMetric.AdjustedWeight = this.selectedPoint.DisplayMaxWeight;
                        }
                        else {
                            var isMetricSet: boolean = false;
                            this.metricService.getMetricsVersionHistory(metric['Uid']).subscribe(res => {
                                res.forEach(item => {
                                    if (!isMetricSet) {
                                        var date = new Date(item.EffectiveDate);
                                        if (selectedDate > date) {
                                            this.selectedMetric = item;
                                            this.selectedMetric['Uid'] = this.selectedMetric['MeasureUid'];
                                            this.selectedMetric['State'] = 3;
                                            this.selectedMetric.AdjustedWeight = this.selectedPoint.DisplayMaxWeight;
                                            isMetricSet = true;
                                            this.cdRef.markForCheck();
                                        }
                                    }
                                })
                            });
                        }
                    }
                })
            }
        })
    }

    private loadTypesAndLatestScore() {
        if (this.uid) {

            this.assetService.getUIDetailsForAssetUID(this.uid).subscribe(res => {
                this.assetTypeUid = res.AssetTypeUid;
                this.assetName = res.DisplayValue;
                this.assetTypeName = res.TypeName;
                this.metricService.getFieldTypeViewModelsByAssetType(this.assetTypeUid).subscribe((x) => {
                    this.fields = x;
                });
            });

            this.metricService.getActiveAllocationsByAssetUid(this.uid).subscribe(x => {
                this.scoreTypes = x.map(x => <any>ScoreType[x.scoreType]);
                this.allocationData = x;
                if (x.length > 0) {
                    this.setSelectedButton(this.scoreTypes[0])
                }
                this.allocationData.forEach(alloc => {
                    this.metricService.getMetricsByAllocation(alloc.uid, true)
                        .subscribe(res => {
                            alloc['metricDefinition'] = res;
                            this.setCurrentDefinition();
                        });

                });
            });

        }
    }

    private loadSeriesData(): Observable<boolean> {
        var subject = new Subject<boolean>();
        this.isDataLoaded = false;

        if (this.uid) {
            this.scoreService.getScoreHistory(this.selectedScoreType, this.uid)
                .subscribe(res => {
                    this.scoresPoints = null;
                    this.scoresPoints = res.sort(function (a, b) {
                        if (a.EffectiveDate > b.EffectiveDate) return -1;
                        if (a.EffectiveDate < b.EffectiveDate) return 1;
                    });

                    this.scoreDate = this.scoresPoints[0].EffectiveDate;

                    for (var i = 0; i < this.scoresPoints.length - 1; i++) {
                        if (this.scoresPoints[i].Score > this.scoresPoints[i + 1].Score)
                            this.scoresPoints[i].ScoreProgression = 1;

                        if (this.scoresPoints[i].Score < this.scoresPoints[i + 1].Score)
                            this.scoresPoints[i].ScoreProgression = -1;

                        if (this.scoresPoints[i].Score == this.scoresPoints[i + 1].Score)
                            this.scoresPoints[i].ScoreProgression = 0;

                    }

                    this.scoresPoints[this.scoresPoints.length - 1].ScoreProgression = 2;

                    //Set data for UI
                    this.scoresPointsDDL = [];
                    if (this.scoresPoints.length > 0) {
                        this.scoresPoints.forEach(p => {
                            this.scoresPointsDDL.push({ value: p, label: 'Default' });
                        });

                        this.scoresPointsDDL[0].value['isFirst'] = true;
                        this.scoresPointsDDL[this.scoresPointsDDL.length - 1].value['isLast'] = true;
                        this.scoresPointSelected = this.scoresPointsDDL[0].value;
                    }

                    subject.next(true);

                    if (this.allocationData) {
                        let stype = ScoreType[this.selectedScoreType];
                        let selected = this.allocationData.filter(x => x.scoreType.toString() == stype.toString());
                        if (selected.length > 0) {
                            this.isExternallyCalculated = selected[0]['isExternallyCalculated'];
                            this.lowerThreshold = +selected[0]['lowerThreshold'] / 100;
                            this.upperThreshold = +selected[0]['upperThreshold'] / 100;
                        }
                    }
                });
        }
        else {
            subject.next(false);
        }
        return subject.asObservable();
    }

    onScorePointChange($event) {
        this.scoreDate = $event.value.EffectiveDate;
        this.loadPoints();
    }

    private calculateBadgeStyle(alloc: ScoreTypeAllocation, actualRatio: number, isDecimal: boolean = true) {
        let style = 'positive';

        let lowerThreshold: number = (isDecimal ? (alloc.lowerThreshold / 100) : alloc.lowerThreshold); 
        let upperThreshold: number = (isDecimal ? (alloc.upperThreshold / 100) : alloc.upperThreshold); 

        if (actualRatio > lowerThreshold && actualRatio <= upperThreshold) {
            style = 'warning';
        }
        else if (actualRatio <= lowerThreshold) {
            style = 'negative';
        }

        return style;
    }

    round(value: number) {
        return Math.round(value * 1000) / 1000;
    }

    private loadPoints(isTabChange: boolean = false) {
        this.isDataLoaded = false;
        if (this.uid) {
            if (this.scoreDate.indexOf('0Z') == -1) {
                this.scoreDate += 'T00:00:00.000Z';
            }

            let selectedAllocation = this.allocationData.find(o => { return <any>ScoreType[o.scoreType] == this.selectedScoreType });
            this.allocationUid = selectedAllocation.uid;

            this.scoreService.getScoreHistoryByAllocationAndAssetAndEffectiveDate(this.allocationUid, this.uid, this.scoreDate)
                .subscribe(res => {
                    this.totalScore = (res ? res.Score : 0 );

                    if (this.totalScore >= 100) {
                        this.totalScore = 100;
                    }
                    this.totalScoreBadgeStyle = this.calculateBadgeStyle(selectedAllocation, this.totalScore, false);
                });

            this.scoreService.getPointBreakdown(this.uid, this.selectedScoreType, this.scoreDate)
                .subscribe(res => {

                    this.pointBreakdown = res;
                    this.isDQAndNoItems();

                    if (isTabChange) {
                        this.scoreDate = new Date().toDateString();
                    }

                    // Get sum of root measures.
                    let rootRawWeightSum: number = this.calculateRawWeightSumAtThisLevel(this.pointBreakdown);

                    this.pointBreakdown.forEach(pb => {
                        pb._groupDisplayMaxWeight = null;
                        pb._badgeStyle = this.calculateBadgeStyle(selectedAllocation, pb.DisplayWeight / pb.DisplayMaxWeight);
                        pb._rawWeightSum = rootRawWeightSum;

                        if (pb.Measures) {
                            // Calculate the raw weight sum under this specific group.
                            let childrenRawWeightSum = this.calculateRawWeightSumAtThisLevel(pb.Measures);
                            pb.Measures.forEach(m => {
                                m._rawWeightSum = childrenRawWeightSum;// pb.DisplayWeight;
                                m._groupDisplayWeight = pb.DisplayWeight;
                                m._groupDisplayMaxWeight = pb.DisplayMaxWeight;
                                m._badgeStyle = this.calculateBadgeStyle(selectedAllocation, m.DisplayWeight / m.DisplayMaxWeight);
                            });
                        }
                    });

                    var preselected: PointBreakdown;
                    if (this.selectedMeasureUid && this.pointBreakdown) {
                        this.pointBreakdown.forEach(pb => {
                            if (pb.Uid == this.selectedMeasureUid)
                                preselected = pb;

                            if (pb.Measures) {
                                pb.Measures.forEach(m => {
                                    if (m.Uid == this.selectedMeasureUid) {
                                        preselected = m;
                                    }
                                })
                            }
                        });
                    }

                    if (preselected) {
                        this.selectScoreItem(preselected);
                    }
                    else if (this.pointBreakdown.length > 0) {
                        this.selectScoreItem(this.pointBreakdown[0]);
                    }

                    this.isDataLoaded = true;
                    this.loadState();
                    this.cdRef.markForCheck();
                });
        }
    }

    calculateRawWeightSumAtThisLevel(points: PointBreakdown[]) {
        let sum: number = 0;

        points.forEach(pb => {
            let match = pb.Conditions?.find((c) => c.Uid === pb.ConditionUid);
            let weight = 0;
            if (match) {
                // GOV-13832 Make sure the weight is defined on the condition, if it is not fall back to the weight on the measure.
                weight = (isNaN(+match.Weight) ? +pb.Weight : +match.Weight);
            } else {
                weight = +pb.Weight;
            }
            sum += +weight;
        });

        return sum;
    }

    private setDropdownHeader() {
        var dropdown = document.getElementsByClassName('scoring-picker-dropdown')[0];
        var panel = dropdown.getElementsByClassName('p-dropdown-panel').length > 0 ? dropdown.getElementsByClassName('p-dropdown-panel')[0] : null;
        if (panel) {
            if (panel.getElementsByClassName('score-dropdown-header').length == 0) {

                var div = document.createElement('div');
                div.className = 'score-dropdown-header';
                this.dropdownClassName = 'scoring-picker-dropdown';

                if (this.scoresPointsDDL.length > 10) {
                    this.dropdownClassName += ' has-scroll';
                }

                var date = document.createElement('div');
                date.className = 'date-wrapper';
                date.innerHTML = 'Date (UTC)';

                var measure = document.createElement('div');
                measure.className = 'measure-wrapper';
                measure.innerHTML = 'Measure';

                var score = document.createElement('div');
                score.className = 'score-wrapper';
                score.innerHTML = 'Score';

                if (this.measurePoints && this.measurePoints.length > 0) {
                    this.scoresPointsShowMeasure = true;
                } else {
                    this.scoresPointsShowMeasure = false;
                }

                if (this.scoresPointsShowMeasure) {
                    this.dropdownClassName += ' has-measures';
                }

                div.append(date);
                if (this.scoresPointsShowMeasure) {
                    div.append(measure);
                }
                div.append(score);

                panel.prepend(div);
            }
        }
    }

    private getMeasurePoint(effectiveDate: any): ScorePoint {
        var point = null;
        if (this.measurePoints)
            point = this.measurePoints.filter(x => x.EffectiveDate == effectiveDate)[0];

        if (point == null || point == undefined) {
            return null;
        }
        else return point;
    }

    private isDQAndNoItems() {
        if (this.pointBreakdown) {
            this.showEmptyMessage = this.pointBreakdown.filter(x => { return x.ScoreType == ScoreType.DataQuality; }).length == 0
                && this.selectedScoreType == ScoreType.DataQuality;
        }
    }

    private setSelectedButton(scoreType: ScoreType) {
        switch (scoreType) {
            case ScoreType.Governance:
                this.showGovernanceScores = true;
                this.showDQScores = false;
                this.activeTab = 'History';

                this.scoreDate = new Date().toDateString();
                this.selectedScoreType = ScoreType.Governance;
                this.loadSeriesData().subscribe(b => {
                    this.loadPoints(true);
                    this.isDQAndNoItems();
                });

                break;
            case ScoreType.DataQuality:
                this.showGovernanceScores = false;
                this.showDQScores = true;
                this.activeTab = 'History';

                this.scoreDate = new Date().toDateString();
                this.selectedScoreType = ScoreType.DataQuality;
                this.loadSeriesData().subscribe(b => {
                    this.loadPoints(true);
                    this.isDQAndNoItems();
                });
                break;
            default:
        }
    }

    private setCollapsed(val: boolean) {
        this.pointBreakdown.forEach(p => {
            p._isCollapsed = !val;
            if (p.Measures) {
                p.Measures.forEach(m => {
                    m._isCollapsed = !val;
                });
            }
        })
        this.saveState();
    }

    hasAnyScoreType(scoreType: ScoreType) {
        if (this.scoreTypes && this.scoreTypes.length > 0) {
            return this.scoreTypes.indexOf(scoreType) !== -1;
        }
    }

    getAsPrecentage(val: number, isDecimal: boolean = true) {
        if (val == 0)
            return '0%';
        if (!val)
            return;

        let s: string = "";

        if (isDecimal) {
            val *= 100;
        }

        s = (Number(Math.round(Number(`${val}e1`)) + "e-1")) + "%";

        return s;
    }

    dateChanged($event) {
        this.scoreDate = $event;
        this.scoresPointsDDL.forEach(point => {
            if (point.value['EffectiveDate'].indexOf(this.scoreDate) == 0) {
                this.scoresPointSelected = point.value;
            }
        });
        this.loadPoints();
    }

    ngAfterViewChecked() {

        //height - to top of the screen - to bottom of the screen - padding
        this.panelHeight = window.innerHeight - 180;
        this.scorePointsMaxHeight = this.panelHeight - 100 - 18;        
        if (this.scorePointsMaxHeight < 100)
            this.scorePointsMaxHeight = 100;

        this.sidePanelHeight = this.panelHeight - 70;

        this.setDropdownHeader();
        this.cdRef.detectChanges();

    }

    getAsExternalBadgeText(item: PointBreakdown) {
        return item.Value ? 'Pass' : 'Fail';
    }

    getAsExternalBadgeStyle(item: PointBreakdown) {
        return item.Value ? 'positive' : 'negative';
    }

    private getStorageKey() {
        return 'scoring_storage_' + this.selectedScoreType + '_' + this.assetTypeUid;
    }

    saveState() {
        if (this.pointBreakdown) {
            var data = {};
            data['expanded'] = this.pointBreakdown.filter(x => x._isCollapsed == true).map(x => x.Uid);
            localStorage.setItem(this.getStorageKey(), JSON.stringify(data));
        }
    }

    loadState() {
        if (this.pointBreakdown) {
            var data = JSON.parse(localStorage.getItem(this.getStorageKey()));
            if (data) {
                if (data['expanded']) {
                    var expanded = data['expanded'] as [];
                    expanded.forEach(ex => {
                        this.pointBreakdown.forEach(pb => {
                            if (pb.Uid == ex)
                                pb._isCollapsed = true;
                        })
                    })
                }
            }
        }
    }

    getTooltipForScoreBadge(item: PointBreakdown) {
        if (item.IsGroup)
            return "Measure Score = sum of sub-measures " + this.getAsPrecentage(item.DisplayWeight);;

        return "Measure Score = " + this.getAsPrecentage(item.DisplayWeight);
    }

    getTooltipForWeightBadge(item: PointBreakdown) {
        if (item.IsGroup)
            return "Maximum possible score = adjusted measure weight = " + this.getAsPrecentage(item.DisplayMaxWeight);

        return "Maximum possible score for measure = measure weight = " + this.getAsPrecentage(item.DisplayMaxWeight);
    }

}
