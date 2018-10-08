import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { PointBreakdown, ScorePoint, AverageScore } from '../models/score.model';

@Injectable()
export class ScoreService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getPointBreakdown(assetUid: string, date: Date = null): Promise<PointBreakdown[]> {
        let uri = `/api/v2/metrics/${assetUid}/pointbreakdown` + (date == null ? '' : `?effectiveDate=${date}`);
        return this.http.get(uri)
            .toPromise()
            .then(response => <PointBreakdown[]>response.json())
            .catch(err => this.handleError(err));
    }

    getScoreHistory(assetUid: string): Promise<ScorePoint[]> {
        return this.http.get(`queries/${assetUid}/ScoreHistoryByObject`)
            .toPromise()
            .then(response => <ScorePoint[]>response.json())
            .catch(err => this.handleError(err));
    }

    getAverageScore(assetUid: string): Promise<AverageScore> {
        return this.http.get(`queries/${assetUid}/AverageScoreByObjectType`)
            .toPromise()
            .then(response => <AverageScore>response.json())
            .catch(err => this.handleError(err));
    }
}