import { Injectable } from '@angular/core';
import { Headers, Http, RequestOptions } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { WebAnalyticsActivity } from '../models/web-analytics-activity.model';

@Injectable()
export class WebAnalyticsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    logActivity(activity: WebAnalyticsActivity) {
        
        let headers = new Headers({ 'Content-Type': 'application/json' });
        let options = new RequestOptions({ headers: headers });

        this.http.post('webanalytics/logactivity', JSON.stringify(activity), options)
            .toPromise()
            .catch(err => this.handleError(err));          
    }
}