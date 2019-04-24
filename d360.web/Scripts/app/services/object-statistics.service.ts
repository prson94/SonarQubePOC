import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {catchError, map} from 'rxjs/operators';

import {ObjectStatistics} from '../models/object-statistics.model';

import {MessagesService} from './messages.service';
import {BaseObservableService} from './baseObservable.service';

@Injectable()
export class ObjectStatisticsService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    getObjectStatistics(
        objectID: number,
        objectType: string
    ): Observable<ObjectStatistics> {
        return this.http.get(`api/${objectType}/${objectID}/object/statistics`).pipe(
            map(response => <ObjectStatistics>response),
            catchError(err => this.handleError(err))
        );
    }

    getObjectStatus(
        objectID: number,
        objectType: string
    ): Observable<string> {
        return this.http.get(`api/${objectType}/${objectID}/status`).pipe(
            map(response => <string>response),
            catchError(err => this.handleError(err))
        );
    }
}
