import { Component, Input, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { MessageBarItem } from '../../models/message-bar-item.model';

@Component({
    selector: 'd3s-messages-bar',
    template: `   
            <div *ngIf="messages.length > 0" class="row">
                <div class="col s12">         
                    <div class="message-bar" *ngFor="let message of messages; let indx=index;">
                        <a (click)="messageClick.emit()" [innerHtml]="message.content"></a>
                        <span *ngIf="message.showClose" class="close" (click)="remove(indx)"><i class="fa fa-times"></i></span>
                    </div>
                </div>
            </div>
        `
})

export class MessagesBarComponent extends BaseComponent {    
    @Input() messages: MessageBarItem[];

    @Output() messageClick = new EventEmitter();

    constructor() {
        super();
    }
        
    private handleMessageClick(message) {
        this.messageClick.emit(message);
    }

    private remove(index) {
        this.messages.splice(index, 1);
    }
}