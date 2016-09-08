///<reference path="../../es6-shim.d.ts"/>
import {Input, Component, OnInit } from '@angular/core';
import { FormMessage, MessageType } from '../../models/form.model';


@Component({
    selector: 'form-message',
    template: `
<div *ngIf="inline" style="display: inline;">
    <span *ngFor="let msg of messages" [class]="getClassByType(msg.MessageType)"><i [class]="'fa ' + getIconByType(msg.MessageType)"></i> {{msg.Message}}</span>
</div>
<div *ngIf="!inline">
    <ul>
        <li *ngFor="let msg of messages">
            <span [class]="getClassByType(msg.MessageType)" ><i [class]="'fa ' + getIconByType(msg.MessageType)"></i> {{msg.Message}}</span>
        </li>
    </ul>
</div>
    `,
    styles: [
        `
.msg-success {
    color: green;
}
.msg-error {
    color: maroon;
}
.msg-info {
    color: black;
}
.msg-warning {
    color: goldenrod;
}
`
    ]
})

export class FormMessagePart implements OnInit {
    @Input() messages: FormMessage[] = new Array<FormMessage>();
    @Input() message: FormMessage = null;
    @Input() inline: boolean = false;

    private getClassByType(t: MessageType): string {
        switch (t) {
            case MessageType.Success:
                return "msg-success";
            case MessageType.Error:
                return "msg-error";
            case MessageType.Info:
                return "msg-info";
            case MessageType.Warning:
                return "msg-warning";
            default:
                return "";
        }
    }

    private getIconByType(t: MessageType): string {
        switch (t) {
            case MessageType.Success:
                return "fa-check-circle";
            case MessageType.Error:
                return "fa-exclamation-circle";
            case MessageType.Info:
                return "fa-info-circle";
            case MessageType.Warning:
                return "fa-exclamation-triangle";
            default:
                return "";
        }
    }


    ngOnInit() {
        if (this.message) {
            this.messages.push(this.message);
        }
    }

}
