
import { Component, Input } from '@angular/core';

@Component({
    selector: 'ig-message-box',
    template:
        `
<div class="ig-message-box" [ngStyle]="{backgroundColor : IsWarning ? '#fbe7bd' : '@ig-slate-t5'}">
    <div [ngClass]="IsWarning ? 'fa fa-exclamation-triangle' : 'fa fa-info-circle'"></div>
    <div class="message-box-text">{{message}}<ng-content></ng-content></div>
</div>
`,
    styleUrls: ['./message-box.less']
})
export class MessageBoxComponent {

    @Input() message: string;
    @Input() IsWarning: boolean = false;

    constructor() {
    }
}
