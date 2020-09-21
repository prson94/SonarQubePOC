
import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, AfterViewInit, OnChanges, SimpleChange, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import * as _ from 'lodash';
import { AssetScore } from '../../../../models/search-result.model';

@Component({
    selector: 'd3s-score-badge',
    templateUrl: './score-badge.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class ScoreBadgeComponent implements OnInit, AfterViewInit, OnChanges {

    @Input() score: AssetScore;
    @Input() mast: boolean = false;
    @Input() showSparkline: boolean = true;
    @Input() displayAsField: boolean = false;
    @Input() displayAsFieldClass: string = "";

    @Input() lowerThreshold: number = 50; //50%
    @Input() upperThreshold: number = 90; //90%

    scoreBadgeClass: string;

    private changeWait: any;
    constructor(
        private ref: ChangeDetectorRef,
        private router: Router
    ) {
    }

    public ngOnInit() {
        this.scoreBadgeClass = this.displayAsField ? "d3s-score-badge-inline" : "d3s-score-badge";
        if (this.displayAsFieldClass == "") {
            this.displayAsFieldClass = "scoretitle";
        }
        this.scoreBadgeClass += (this.mast) ? " mast" : " nomast";
    }

    ngAfterViewInit(): void {

    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.score.UpperThreshold && this.score.LowerThreshold) {
            this.upperThreshold = this.score.UpperThreshold;
            this.lowerThreshold = this.score.LowerThreshold;
            this.ref.markForCheck();
        }
    }

    getType(): string {
        var type = this.score.ScoreType.split(/(?=[A-Z])/).join(' ');
        if (this.mast || this.displayAsField) {
            type += ' Score';
            if (this.displayAsField) {
                type += ': ';
            }
        }
        return type;
    }

    getValuePct() {
        return this.score.Value.toFixed(1);
    }

    getCurrentScoreThreshold() {
        var score = this.score.Value * 100;
        if (score <= this.lowerThreshold)
            return `0% - ${this.lowerThreshold}%`;
        if (score <= this.upperThreshold)
            return `${this.lowerThreshold}.% - ${this.upperThreshold}%`;
        return `${this.upperThreshold}.% - 100%`;;
    }


    getScoreCSSClass() {
        if (this.score.Value <= this.lowerThreshold / 100)
            return 'score-poor'; //red
        if (this.score.Value <= this.upperThreshold / 100)
            return 'score-average'; //yellow
        return 'score-good'; //green
    }

    private lastCalculatedMessage() {
        if (!this.score.EffectiveDate) {
            return this.getType() + " not yet calculated";
        }
        var diff = new Date(Date.now() - Date.parse(this.score.EffectiveDate));

        var years = diff.getUTCFullYear() - 1970;

        if (years > 0) return this.getType() + " last calculated " + years + " years ago.";

        var months = diff.getUTCMonth();

        if (months > 0) return this.getType() + " last calculated " + months + " months ago.";

        var days = diff.getUTCDate() - 1;

        if (days > 0) return this.getType() + " last calculated " + days + " days ago.";

        var hours = diff.getUTCHours();

        if (hours > 0) return this.getType() + " last calculated " + hours + " hours ago.";

        var minutes = diff.getUTCMinutes();

        if (minutes > 0) return this.getType() + " last calculated " + minutes + " minutes ago.";

        return this.getType() + " last calculated a few seconds ago.";
    }

    get lastRunDate(): string {
        if (this.score != null) {
            if (this.score.RunDate)
                return this.score.RunDate;
            if (this.score.EffectiveDate)
                return this.score.EffectiveDate;
        }
        return null;
    }

};
