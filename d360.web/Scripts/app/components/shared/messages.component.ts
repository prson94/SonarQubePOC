///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import { MessagesService } from '../../services/messages.service';
import { SiteMessage } from '../../models/site-message.model';
import { Subscription }   from 'rxjs/Subscription';
import { Message } from 'primeng/primeng';

@Component({
    selector: 'd3s-messages',
    template: `
            <p-growl [value]="msgs"></p-growl>
        `
})

export class MessagesComponent {
    subscription: Subscription;
    msgs: Message[];

    constructor(private messagesService: MessagesService) {
        this.msgs = [];
        this.subscription = messagesService.errorMessage$.subscribe(
            errorMsg => {                
                this.msgs.push({ severity: 'error', summary: errorMsg.summary, detail: errorMsg.detail });                
            });
        this.subscription = messagesService.infoMessage$.subscribe(
            infoMsg => {                
                this.msgs.push({ severity: 'info', summary: infoMsg.summary, detail: infoMsg.detail });
            });
    }

    ngOnDestroy() {
        // prevent memory leak when component destroyed
        this.subscription.unsubscribe();
    }
}