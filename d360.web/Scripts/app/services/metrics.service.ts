import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JsonResult } from '../models/jsonresult.model';
import { MetricAssetViewModel, MetricFieldTypeViewModel, ScoreType } from '../models/metrics.model';
import { AssetTypeMetricModel } from '../models/asset.model';
import { Observable } from 'rxjs';
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';
import { catchError } from 'rxjs/operators';



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
            .get<MetricAssetViewModel[]>(`/api/v2/metrics/structure/${allocationUid}`)
            .pipe(catchError(err => this.handleError(err)));
    }

    public saveMetric(model: MetricAssetViewModel): Observable<JsonResult> {
        return this.http
            .post<JsonResult>(`/api/v2/metrics`, model)
            .pipe(catchError(err => this.handleError(err)));
    }
}