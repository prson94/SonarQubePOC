import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { JsonResult } from '../models/jsonresult.model';
import { Group, Map, Item, Condition, MapForm, ConditionForm, MetricAssetViewModel, MetricFieldTypeViewModel } from '../models/metrics.model';
import { AssetTypeMetricModel } from '../models/asset.model';
import { HttpErrorResponse } from '@angular/common/http';


@Injectable()
export class MetricsService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    public getAssetTypes(): Promise<AssetTypeMetricModel[]> {
        return this.http.get(`/api/metrics/assettypes`)
            .toPromise()
            .then(response => <AssetTypeMetricModel[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getFieldTypeViewModelsByAssetType(assetTypeUid: string): Promise<MetricFieldTypeViewModel[]> {
        return this.http.get(`/api/v2/metrics/fields/${assetTypeUid}`)
            .toPromise()
            .then(response => <MetricFieldTypeViewModel[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getMetricsByAssetType(assetTypeUid: string): Promise<MetricAssetViewModel[]> {
        return this.http.get(`/api/v2/metrics/structure/${assetTypeUid}`)
            .toPromise()
            .then(response => <MetricAssetViewModel[]>response.json())
            .catch(err => this.handleError(err));
    }

    public saveMetric(model: MetricAssetViewModel): Promise<JsonResult> {
        return this.http.post(`/api/v2/metrics`, model)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }
}