
import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Input, AfterViewInit, OnChanges, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-status-badge',
    templateUrl: './status-badge.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class StatusBadgeComponent implements AfterViewInit, OnChanges {

    @Input() status: string;

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


    getBackgroundColor() {
        status = this.status.toLowerCase().trim();

        switch (status) {
            case 'draft':
                return '#d1dce4';
            case 'certified':
                return '#4ecc89';
            case 'under review':
                return '#e2792a';
            default:
                //custom status, we need to generate a color
                let hash = 0;
                for (let i = 0; i < status.length; i++) {
                    hash = status.charCodeAt(i) + ((hash << 5) - hash);
                    hash = hash & hash;
                }
                return `hsl(${(hash * 2) % 360}, 70%, 70%)`;
        }
    }

    private hsl2rgb(h:number, s:number, l:number): number[]{
        let a = s * Math.min(l, 1 - l);
        let f = (n, k = (n + h / 30) % 12) => l - a * Math.max(Math.min(k - 3, 9 - k, 1), -1);
        return [f(0), f(8), f(4)];
    }

    private hslStringIsLight(color: string, lumaLimit: number = 128 ): boolean {
        var hue = parseInt(color.substr(4, color.indexOf(",")), 10);
        hue = (hue + 360) % 360;
        //assuming sat/light is 70%
        var rgb = this.hsl2rgb(hue, 0.7, 0.7).map(x => Math.round(x * 255));
        var luma = ((rgb[0] * 299) + (rgb[1] * 587) + (rgb[2] * 114)) / 1000;
        return (luma >= lumaLimit);
    }

    getForegroundColor() {
        var dark = '#515667';
        var light = '#ffffff';

        switch (this.status.toLowerCase().trim()) {
            case 'draft':
            case 'certified':
                return dark;
            case 'under review':
                return light;
            default:
                return this.hslStringIsLight(this.getBackgroundColor(), 170) ? dark : light;
        }
    }

    getStatusIcon() {
        switch (this.status.toLowerCase().trim()) {
            case 'draft':
                return 'fa-adjust fa-flip-horizontal';
            case 'certified':
                return 'fa-check-circle-o';
            case 'under review':
                return 'fa-circle';
            default:
                return 'fa-circle-o';
        }
    }
};
