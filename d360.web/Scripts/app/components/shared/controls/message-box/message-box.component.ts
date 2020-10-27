
import { Component, Input } from '@angular/core';

@Component({
    selector: 'ig-message-box',
    template:
        `
<div class="ig-message-box">
    <div class="fa fa-info-circle"></div>
    <div class="message-box-text">{{message}}<ng-content></ng-content></div>
</div>
`,
    styleUrls: ['./message-box.less']
})
export class MessageBoxComponent {

    @Input() message: string;

    constructor() {
    }
}
