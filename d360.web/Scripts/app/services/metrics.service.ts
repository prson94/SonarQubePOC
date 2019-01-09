import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { JsonResult } from '../models/jsonresult.model';
import { Group, Map, Item, Condition, MapForm, ConditionForm, MetricAssetViewModel, MetricFieldTypeViewModel } from '../models/metrics.model';
import { AssetTypeMetricModel } from '../models/asset.model';
import { Observable } from 'rxjs';



@Injectable()
export class MetricsService extends BaseService {

    constructor(private http: HttpClient, messagesService: MessagesService) { super(messagesService); }

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