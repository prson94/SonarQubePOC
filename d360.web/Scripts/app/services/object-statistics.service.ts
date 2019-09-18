import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {catchError, map} from 'rxjs/operators';

import {ObjectStatistics} from '../models/object-statistics.model';

import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';

@Injectable()
export class ObjectStatisticsService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
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

    getScoreAndStatus(Uid: string): Observable<string> {
        return this.http.get(`api/v2/assets/GetScoreAndStatus/${Uid}`).pipe(
            map(response => <any>response),
            catchError(err => this.handleError(err))
        );
    }

    getCertificationStatusColor(status: string) {
        status = status.toLowerCase().trim();

        switch (status) {
            case 'draft':
                return '#6c7884';
            case 'certified':
                return '#00853e';
            case 'under review':
                return '#FFB000';
            default:
                //custom status, we need to generate a color
                let hash = 0;
                for (let i = 0; i < status.length; i++) {
                    hash = status.charCodeAt(i) + ((hash << 5) - hash);
                    hash = hash & hash;
                }
                return `hsl(${(hash * 2) % 360}, 70%, 70%)`;
        }
    }
}
