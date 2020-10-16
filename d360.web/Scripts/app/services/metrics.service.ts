import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JsonResult } from '../models/jsonresult.model';
import { MetricAssetViewModel, MetricFieldTypeViewModel, ScoreType, MetricAssetHistoryViewModel } from '../models/metrics.model';
import { AssetTypeMetricModel } from '../models/asset.model';
import { Observable } from 'rxjs';
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';
import { catchError, map } from 'rxjs/operators';

@Injectable()
export class MetricsService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    public getAssetTypes(): Observable<AssetTypeMetricModel[]> {
        return this.http
            .get<AssetTypeMetricModel[]>(`/api/metrics/assettypes`)
            .pipe(catchError(err => this.handleError(err)));
    }

    public getFieldTypeViewModelsByAssetType(assetTypeUid: string): Observable<MetricFieldTypeViewModel[]> {
        return this.http
            .get<MetricFieldTypeViewModel[]>(`/api/v2/metrics/fields/${assetTypeUid}`)
            .pipe(catchError(err => this.handleError(err)));
    }

    public getMetricsByAllocation(allocationUid: string): Observable<MetricAssetViewModel[]> {
        return this.http
            .get<MetricAssetViewModel[]>(`/api/v2/scoring/allocations/${allocationUid}/structure`)
            .pipe(catchError(err => this.handleError(err)));
    }

    public getMetricsVersionHistory(measureUid: string): Observable<MetricAssetHistoryViewModel[]> {
        return this.http
            .get<MetricAssetHistoryViewModel[]>(`/api/v2/scoring/history/measure/${measureUid}`)
            .pipe(catchError(err => this.handleError(err)));
    }

    public saveMetric(model: MetricAssetViewModel): Observable<JsonResult> {
        return this.http
            .post<JsonResult>(`/api/v2/metrics`, model)
            .pipe(catchError(err => this.handleError(err)));
    }

    public getMetricsScores(assetTypeUid: string,type:any): Observable<any> {
        return this.http
            .get<any>(`/api/v2/metrics/${assetTypeUid}/scores?_scoreType=${type}`)
            .pipe(catchError(err => this.handleError(err)));
    }
}