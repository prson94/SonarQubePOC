
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
        return Math.round(this.score.Value * 100);
    }

 
    getBackgroundColor() {
        if (this.score.Value <= this.lowerThreshold)
            return "#ed3765"; //red
        if (this.score.Value <= this.upperThreshold)
            return "#f8a41a"; //yellow
        return "#4ecc89"; //green
    }

    private lastCalculatedMessage() {
        if (!this.score.EffectiveDate) {
            return "Governance Score not yet calculated";
        }
        var diff = new Date(Date.now() - Date.parse(this.score.EffectiveDate));

        var years = diff.getUTCFullYear() - 1970;

        if (years > 0) return "Governance Score last calculated " + years + " years ago.";

        var months = diff.getUTCMonth();

        if (months > 0) return "Governance Score last calculated " + months + " months ago.";

        var days = diff.getUTCDate() - 1;

        if (days > 0) return "Governance Score last calculated " + days + " days ago.";

        var hours = diff.getUTCHours();

        if (hours > 0) return "Governance Score last calculated " + hours + " hours ago.";

        var minutes = diff.getUTCMinutes();

        if (minutes > 0) return "Governance Score last calculated " + minutes + " minutes ago.";

        return "Governance Score last calculated a few seconds ago.";
    }

};
