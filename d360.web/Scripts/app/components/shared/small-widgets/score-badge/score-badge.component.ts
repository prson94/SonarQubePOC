
import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, OnChanges, SimpleChange, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import * as _ from 'lodash';
import { AssetScore } from '../../../../models/search-result.model';
import { ScoreDisplayPipe } from '../../../../pipes/score-display.pipe';

@Component({
    selector: 'd3s-score-badge',
    templateUrl: './score-badge.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class ScoreBadgeComponent implements OnInit, OnChanges {

    @Input() score: AssetScore;

    @Input() lowerThreshold: number = 50; //50%
    @Input() upperThreshold: number = 90; //90%

    @Input() igBadgeStyle: boolean = false;
    @Input() useMiniBadge: boolean = false;
    @Input() precision: number = 0;

    _type: string;

    private changeWait: any;
    constructor(
        private ref: ChangeDetectorRef,
        private router: Router,
        private scoreDislpayPipe: ScoreDisplayPipe
    ) {
    }

    public ngOnInit() {
        this._type = this.score.ScoreType.split(/(?=[A-Z])/).join(' ');
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.score.UpperThreshold && this.score.LowerThreshold) {
            this.upperThreshold = this.score.UpperThreshold;
            this.lowerThreshold = this.score.LowerThreshold;
            this.ref.markForCheck();
        }
    }

    getType(): string {
        return this._type;
    }

    getBadgeText(): string {
        return this.getType() + ' ' + this.scoreDislpayPipe.transform(this.score.Value, this.precision);
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
        return `${this.upperThreshold}.% - 100%`;
    }


    getScoreVariantColor(): string {
        if (this.score.Value <= this.lowerThreshold / 100)
            return 'negative';
        if (this.score.Value <= this.upperThreshold / 100)
            return 'warning';
        return 'positive';
    }

    getScoreCSSClass(): string {
        if (this.score.Value <= this.lowerThreshold / 100) {
            return 'poor'; //red
        }
        if (this.score.Value <= this.upperThreshold / 100) {
            return 'average'; //yellow
        }
        return 'good'; //green
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

}
