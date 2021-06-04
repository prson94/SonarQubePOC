
import { Component, Input, AfterViewInit } from '@angular/core';
import { TooltipModule } from 'primeng/tooltip';

@Component({
    selector: 'ig-badge',
    styleUrls: ['./badge.less'],
    template: `
<div [style.background-color]="backgroundColor" [class]="'ig-badge ' + variant" pTooltip="{{tooltipText}}" tooltipPosition="bottom" tooltipStyleClass="ig-tooltip">
    <span>{{text}}</span>
</div>
`
}) 
export class IgBadgeComponent {

    @Input() text: string;
    @Input() variant: string = "default";
    @Input() backgroundColor: string;
    @Input() tooltipText: string;

    constructor() {
    }
}
