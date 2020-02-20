
import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, AfterViewInit, OnChanges, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';
import * as _ from 'lodash';
import { AssetScore } from '../../../../models/search-result.model';

@Component({
    selector: 'd3s-score-badge',
    templateUrl: './score-badge.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class ScoreBadgeComponent implements AfterViewInit, OnChanges {

    @Input() score: AssetScore;
    @Input() mast: boolean = false;
    @Input() lowerThreshold: number = 0.5; //50%
    @Input() upperThreshold: number = 0.9; //90%

    @Input() lowColour: string = "#ed3765";
    @Input() mediumColour: string = "#f8a41a";
    @Input() goodColour: string = "#4ecc89";

    private changeWait: any;
    constructor(
        ref: ChangeDetectorRef,
        private router: Router
    ) {
    }

    ngAfterViewInit(): void {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {

    }

    getType(): string {
        var type = this.score.ScoreType.split(/(?=[A-Z])/).join(' ');
        return type + (this.mast ? ' Score' : '');
    }

    getValuePct() {
        return (this.score.Value * 100).toFixed(1);
    }

    getCurrentScoreThreshold() {
        if (this.score.Value <= this.lowerThreshold)
            return `0% - ${this.lowerThreshold * 100}%`;
        if (this.score.Value <= this.upperThreshold)
            return `>${this.lowerThreshold * 100}% - ${this.upperThreshold * 100}%`;
        return `>${this.upperThreshold * 100}% - 100%`;;
    }


    getBackgroundColor() {
        if (this.score.Value <= this.lowerThreshold)
            return this.lowColour; //red
        if (this.score.Value <= this.upperThreshold)
            return this.mediumColour; //yellow
        return this.goodColour; //green
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

};
