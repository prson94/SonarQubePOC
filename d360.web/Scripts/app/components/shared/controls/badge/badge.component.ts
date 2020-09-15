
import { Component, Input, AfterViewInit } from '@angular/core';

@Component({
    selector: 'ig-badge',
    styleUrls: ['./badge.less'],
    template: `
<div [style.background-color]="backgroundColor" [class]="'ig-badge ' + variant">
    <span [style.color]="textColor">{{text}}</span>
</div>
`
})
export class IgBadgeComponent {

    @Input() text: string;
    @Input() variant: string = "default";
    @Input() backgroundColor: string;
    @Input() textColor: string;

    constructor() {
    }
}
