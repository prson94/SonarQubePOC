import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { JsonResult } from '../models/jsonresult.model';
import { Group, Map, Item, Condition, MapForm, ConditionForm, MetricAssetViewModel, MetricFieldTypeViewModel } from '../models/metrics.model';
import { AssetTypeMetricModel } from '../models/asset.model';
import { Observable } from 'rxjs';
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';



@Injectable()
export class MetricsService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    public getAssetTypes(): Observable<AssetTypeMetricModel[]> {
        return this.http
            .get<AssetTypeMetricModel[]>(`/api/metrics/assettypes`);
    }

    public getFieldTypeViewModelsByAssetType(assetTypeUid: string): Observable<MetricFieldTypeViewModel[]> {
        return this.http
            .get<MetricFieldTypeViewModel[]>(`/api/v2/metrics/fields/${assetTypeUid}`);
    }

    public getMetricsByAssetType(assetTypeUid: string): Observable<MetricAssetViewModel[]> {
        return this.http
            .get<MetricAssetViewModel[]>(`/api/v2/metrics/structure/${assetTypeUid}`);
    }

    public saveMetric(model: MetricAssetViewModel): Observable<JsonResult> {
        return this.http
            .post<JsonResult>(`/api/v2/metrics`, model);
    }
}