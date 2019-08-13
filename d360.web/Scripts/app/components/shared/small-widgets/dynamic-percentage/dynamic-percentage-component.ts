
import { Component, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, AfterViewInit, OnChanges, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import * as _ from 'lodash';

@Component({
	selector: 'd3s-dynamic-percentage',
	template: `
				<div #self class="d3s-dynamic-percentage">
					<div class="d3s-dynamic-percentageInner" [ngStyle]="{'background': innerCircleColor}">
						<div class="d3s-dynamic-percentage-text"></div>
					</div>
				</div>
			  `,
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class DynamicPercentageComponent implements AfterViewInit, OnChanges {

    @Input() percentage: number;
    @Input() innerCircleColor: string = "rgb(0, 0, 0)";

    @ViewChild("self") self: ElementRef;
    private changeWait: any;
    constructor(
        ref: ChangeDetectorRef,
        private router: Router
    ) {
    }

    ngAfterViewInit(): void {
        //allow time for items to render before animation begins
        setTimeout(() => {
            this.calculatePercent(this.self, this.percentage, 0);
        }, 200);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        clearTimeout(this.changeWait);
        this.changeWait = setTimeout(() => {
            this.calculatePercent(this.self, this.percentage, 0);
        }, 200);
    }

    private calculatePercent(event, end, i) {
        if (end < 0)
            end = 0;
        else if (end > 100)
            end = 100;
        if (typeof i === 'undefined')
            i = 0;
        var curr = (100 * i) / 360;
        if (i <= 180) {
            var m = event.nativeElement, c = m.style;
            c.backgroundImage = 'linear-gradient(' + (90 + i) + 'deg, transparent 50%, #ccc 50%),linear-gradient(90deg, #ccc 50%, transparent 50%)';
        } else {
            var m = event.nativeElement, c = m.style;
            c.backgroundImage = 'linear-gradient(' + (i - 90) + 'deg, transparent 50%, #ffffff 50%),linear-gradient(90deg, #ccc 50%, transparent 50%)';
        }
        if (curr < end) {
            setTimeout(() => {
                this.calculatePercent(event, end, 2 + i);
            }, 1);
        }
    }

};
