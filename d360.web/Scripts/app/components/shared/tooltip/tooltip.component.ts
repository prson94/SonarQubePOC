import { Component, EventEmitter, Output, Input, HostBinding, ChangeDetectionStrategy } from '@angular/core';

@Component({
    selector: 'd3s-info-toolip',
    template: `                 
               <div class="d3s-info-toolip">
                    <div class="value">
                        {{titleValue}}
                    </div>
                    <div class="content">
                    <ng-content></ng-content>
                    </div>
                </div>
              `,
    changeDetection: ChangeDetectionStrategy.OnPush    
})

export class InfoTooltipComponent  {
    @Input() titleValue: string;
};
