
import { Component, Input } from '@angular/core';

@Component({
    selector: 'ig-message-box',
    template:
        `
<div class="ig-message-box" [ngStyle]="{backgroundColor : messagetype === 'warning' ? '#fbe7bd' : '@ig-slate-t5'}">
    <div class="fa" [ngClass]="{'fa-exclamation-triangle': messagetype === 'warning', 'fa-info-circle' : messagetype === 'information'}"></div>
    <div class="message-box-text">{{message}}<ng-content></ng-content></div>
</div>
`,
    styleUrls: ['./message-box.less']
})
export class MessageBoxComponent {

    @Input() message: string;
    @Input() messagetype: string = "information";

    constructor() {
    }
}
