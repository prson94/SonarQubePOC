import { Injectable } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import { catchError } from "rxjs/operators";
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { UsageEntry } from '../models/web-analytics-activity.model';

@Injectable({
    providedIn: 'root'
})
export class WebAnalyticsService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

	logActivity(activity: UsageEntry) {
        this
			.http
			.post('/api/v2/environment/usage', activity, { headers: { 'Content-Type': 'application/json' } })            
			.pipe(
				catchError((err) => this.handleError(err))
            ).subscribe();
    }
}