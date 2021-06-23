import { Injectable } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import { catchError } from "rxjs/operators";
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { WebAnalyticsActivity } from '../models/web-analytics-activity.model';

@Injectable({
    providedIn: 'root'
})
export class WebAnalyticsService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    logActivity(activity: WebAnalyticsActivity) {                
        this
            .http
            .post('webanalytics/logactivity', JSON.stringify(activity), { headers: {'Content-Type':'application/json'}})            
            .pipe(                
            catchError(err => this.handleError(err))
            ).subscribe();
    }
}