import { Injectable } from '@angular/core';
import { PointBreakdown, ScorePoint, AverageScore } from '../models/score.model';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class ScoreService extends BaseObservableService  {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getPointBreakdown(assetUid: string, date: Date = null): Observable<PointBreakdown[]> {
        let uri = `/api/v2/metrics/${assetUid}/pointbreakdown` + (date == null ? '' : `?effectiveDate=${date}`);
        return this.http.get(uri)
            .pipe(
                map(response => <PointBreakdown[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getScoreHistory(assetUid: string): Observable<ScorePoint[]> {
        return this.http.get(`queries/${assetUid}/ScoreHistoryByObject`)
            .pipe(
                map(response => <ScorePoint[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getAverageScore(assetUid: string): Observable<AverageScore> {
        return this.http.get(`queries/${assetUid}/AverageScoreByObjectType`)
            .pipe(
                map(response => <AverageScore>response),
                catchError(err => this.handleError(err))
            );
    }
}