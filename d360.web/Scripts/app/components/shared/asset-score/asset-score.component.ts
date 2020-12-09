import { Component, Input, OnChanges, SimpleChange, ChangeDetectorRef, AfterViewChecked, ViewEncapsulation } from '@angular/core';
import { BaseComponent } from '../base.component';
import { ScoreService } from '../../../services/score.service';
import { PointBreakdown, ScorePoint } from '../../../models/score.model';
import { ScoreType } from '../../../models/metrics.model';
import { Observable, Subject } from 'rxjs';
import { SelectItem } from 'primeng/api';
import { MetricsService } from '../../../services/metrics.service';
import { AssetService } from '../../../services/asset.service';

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

    scoresPoints: ScorePoint[];
    measurePoints: ScorePoint[];

    scoresPointsDDL: SelectItem[];
    scoresPointsShowMeasure: boolean = false;
    scoresPointSelected: SelectItem;
    scorePointsMaxHeight: number = 200;

    averageScore: number;
    scoreDate: string = null;
    showGovernanceScores: boolean = true;
    showDQScores: boolean = false;

    private pointBreakdown: PointBreakdown[] = [];
    private selectedPoint: PointBreakdown;

    ScoreType = ScoreType;
    private selectedScoreType = ScoreType.Governance;
    private scoreTypes: number[] = [];
    private allocationData: any[] = [];
    showEmptyMessage: boolean = false;

    showExpandAndCollapse: boolean = true;
    totalScore: number;
    activeTab: string = 'History';
    isDataLoaded: boolean = false;

    selectedMetric: any;
    isExternallyCalculated: boolean = false;

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

    constructor(protected scoreService: ScoreService,
        protected assetService: AssetService,
        protected metricService: MetricsService,
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

    private selectScoreItem(item: PointBreakdown) {
        this.pointBreakdown.forEach(x => {
            x._isSelected = false;
            if (x.Measures) {
                x.Measures.forEach(m => m._isSelected = false);
            }
        });
        item._isSelected = true;
        this.selectedPoint = JSON.parse(JSON.stringify(item));
        this.setCurrentDefinition();
    }

    private setCurrentDefinition() {
        if (!this.selectedPoint || !this.allocationData) return;

        this.allocationData.forEach(alloc => {
            if (alloc['metricDefinition']) {
                var arr = alloc['metricDefinition'] as any[];
                arr.forEach(metric => {
                    if (metric['Uid'] == this.selectedPoint.Uid) {
                        this.selectedMetric = metric;
                    }
                })
            }
        })
    }

    private loadTypesAndLatestScore() {
        if (this.uid) {
            this.assetService.getUIDetailsForAssetUID(this.uid).subscribe(res => {
                this.assetTypeUid = res.AssetTypeUid;
            })

            this.scoreService.getScoreTypes(this.uid).subscribe(x => {
                this.scoreTypes = x.map(x => x.scoretype as ScoreType);
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

                    this.scoresPointsDDL = this.scoresPointsDDL.slice(0, 10);
                    subject.next(true);

                    if (this.allocationData) {
                        var selected = this.allocationData.filter(x => x.scoretype == this.selectedScoreType);
                        if (selected.length > 0) {
                            this.isExternallyCalculated = selected[0]['isexternallycalculated'];
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

    private loadPoints(isTabChange: boolean = false) {
        this.isDataLoaded = false;
        if (this.uid) {
            if (this.scoreDate.indexOf('0Z') == -1) {
                this.scoreDate += 'T00:00:00.000Z';
            }
            this.scoreService.getPointBreakdown(this.uid, this.selectedScoreType, this.scoreDate)
                .subscribe(res => {
                    this.pointBreakdown = res;
                    this.isDQAndNoItems();

                    if (isTabChange) {
                        this.scoreDate = new Date().toDateString();
                    }

                    //Set data for UI
                    this.totalScore = 0;
                    this.pointBreakdown.forEach(pb => {
                        if (!pb.IsGroup) {
                            if (pb.Value) {
                                pb._badgeStyle = 'positive';
                                pb._finalScore = pb.AdjustedWeight;
                            }
                            else {
                                pb._badgeStyle = 'negative';
                                pb._finalScore = 0;
                            }
                        }
                        else {
                            if (pb.Measures) {
                                pb.Measures.forEach(m => {
                                    if (m.Value) {
                                        m._badgeStyle = 'positive';
                                        m._finalScore = m.AdjustedMaxWeight = m.AdjustedMaxWeight * pb.AdjustedMaxWeight;
                                    }
                                    else {
                                        m._badgeStyle = 'negative';
                                        m.AdjustedMaxWeight = m.AdjustedMaxWeight * pb.AdjustedMaxWeight;
                                        m._finalScore = 0;
                                    }
                                });

                                var positive = pb.Measures.filter(x => x.Value);
                                if (positive.length == 0) {
                                    pb._badgeStyle = 'negative';
                                    pb._finalScore = 0;
                                }
                                else if (positive.length == pb.Measures.length) {
                                    pb._badgeStyle = 'positive';
                                    pb._finalScore = pb.AdjustedMaxWeight;
                                }
                                else {
                                    pb._badgeStyle = 'warning';
                                    var sum = 0;
                                    positive.forEach(x => {
                                        sum += x._finalScore;
                                    });

                                    pb._finalScore = sum;;
                                }



                            }
                        }

                        this.totalScore += pb._finalScore;
                    })
                    if (this.pointBreakdown.length > 0)
                        this.selectScoreItem(this.pointBreakdown[0]);


                    this.isDataLoaded = true;
                    this.cdRef.markForCheck();
                });
        }
    }

    private setDropdownHeader() {
        var dropdown = document.getElementsByClassName('scoring-picker-dropdown')[0];
        var panel = dropdown.getElementsByClassName('ui-dropdown-panel').length > 0 ? dropdown.getElementsByClassName('ui-dropdown-panel')[0] : null;
        if (panel) {
            if (panel.getElementsByClassName('score-dropdown-header').length == 0) {
                console.log("adding element");
                var div = document.createElement('div');
                div.className = 'score-dropdown-header';
                if (this.scoresPointsDDL.length > 10) {
                    div.className = 'score-dropdown-header has-scroll';
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

                console.log(this.measurePoints);

                div.append(date);
                if (this.scoresPointsShowMeasure)
                    div.append(measure);
                div.append(score);

                panel.prepend(div);
            }
        }
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
    }

    hasAnyScoreType(scoreType: ScoreType) {
        if (this.scoreTypes && this.scoreTypes.length > 0)
            return this.scoreTypes.indexOf(scoreType) !== -1;
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
        this.scorePointsMaxHeight = window.innerHeight - 228 - 60 - 16;
        if (this.scorePointsMaxHeight < 100)
            this.scorePointsMaxHeight = 100;

        this.setDropdownHeader();

        this.cdRef.detectChanges();

    }

    getAsDQBadgeText(item: PointBreakdown) {
        return item.Value ? 'Pass' : 'Fail';
    }
}
