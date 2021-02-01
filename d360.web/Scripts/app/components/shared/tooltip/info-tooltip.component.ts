import { Component, EventEmitter, Output, Input, HostBinding, ChangeDetectionStrategy, OnInit } from '@angular/core';

@Component({
    selector: 'd3s-info-tooltip',
    template: `                 
               <div class="d3s-info-tooltip">
                    <div class="value" [innerHtml]="titleValue"></div>
                    <div class="tooltip-content">
                    <ng-content></ng-content>
                    </div>
                </div>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class InfoTooltipComponent implements OnInit {
    @Input() titleValue: string = '';
    @Input() style: string;

    ngOnInit() {
        if (this.titleValue == '') {
            if (!this.style || this.style == 'info') {
                this.titleValue = `<i class='fa fa-question-circle'><i/>`;
            }
            if (this.style == 'warning') {
                this.titleValue = `<i class='fa fa-exclamation-triangle'></i>`;
            }
        }
    }
}

import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';


@NgModule({
    declarations: [
        InfoTooltipComponent
    ],
    exports: [

        InfoTooltipComponent
    ]
    , imports: [
        CommonModule
    ]

})

export class InfoTooltipModule { }
