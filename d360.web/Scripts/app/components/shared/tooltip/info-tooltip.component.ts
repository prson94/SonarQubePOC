import { Component, EventEmitter, Output, Input, HostBinding, ChangeDetectionStrategy } from '@angular/core';

@Component({
    selector: 'd3s-info-tooltip',
    template: `                 
               <div class="d3s-info-tooltip">
                    <div class="value">
                        {{titleValue}}
                    </div>
                    <div class="tooltip-content">
                    <ng-content></ng-content>
                    </div>
                </div>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush    
})

export class InfoTooltipComponent  {
    @Input() titleValue: string;
};

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
