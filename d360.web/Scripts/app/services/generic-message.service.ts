import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { GenericMessageModel } from '../models/generic-message.model'

@Injectable({
    providedIn: 'root'
})
export class GenericMessageService extends BaseObservableService {
    private subject = new Subject<GenericMessageModel>();

    sendMessage(message: GenericMessageModel) {
        this.subject.next(message);
    }

    clearMessages() {
        this.subject.next();
    }

    getMessage(): Observable<GenericMessageModel> {
        return this.subject.asObservable();
    }
}