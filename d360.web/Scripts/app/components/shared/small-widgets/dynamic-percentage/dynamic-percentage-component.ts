
import { Component, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef, Input, ViewChild, AfterViewInit, OnChanges, SimpleChange } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import * as _ from 'lodash';

@Component({
	selector: 'd3s-dynamic-percentage',
	template: `
				  <div class="d3s-chart">
                    <svg viewBox="0 0 38 38" class="d3s-circular-chart">
                      <path class="d3s-circle-bg"
                        d="M18 2.0845
                          a 15.9155 15.9155 0 0 1 0 31.831
                          a 15.9155 15.9155 0 0 1 0 -31.831"
                      />
                      <path *ngIf="percentage > 0" class="d3s-circle"
                        [attr.stroke-dasharray]="percentage + ', 100'"
                        d="M18 2.0845
                          a 15.9155 15.9155 0 0 1 0 31.831
                          a 15.9155 15.9155 0 0 1 0 -31.831"
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


};
