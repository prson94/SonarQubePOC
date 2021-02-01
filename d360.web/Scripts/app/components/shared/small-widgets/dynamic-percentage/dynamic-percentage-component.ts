
import { Component, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, AfterViewInit, OnChanges, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import * as _ from 'lodash';

@Component({
	selector: 'd3s-dynamic-percentage',
	template: `
				  <div class="d3s-chart">
                    <svg viewBox="-16 -16 32 32" class="d3s-circular-chart">
                      <defs>
                        <circle id="basecircle" cx="0" cy="0" r="15.9155" />
                        <clipPath id="clip">
                            <use xlink:href="#basecircle"/>
                        </clipPath>
                      </defs>
                      <use xlink:href="#basecircle" class="d3s-circle-bg" clip-path="url(#clip)"/>
                      <path *ngIf="percentage > 0" class="d3s-circle"
                        [attr.stroke-dasharray]="percentage + ', 100'"
                        d="M0 -15.9155
                          a 15.9155 15.9155 0 0 1 0 31.831
                          a 15.9155 15.9155 0 0 1 0 -31.831"
                        clip-path="url(#clip)"
                      />
                    </svg>
                  </div>
			  `,
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class DynamicPercentageComponent implements AfterViewInit, OnChanges {

    @Input() percentage: number;

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


}
