import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JsonResult } from '../models/jsonresult.model';
import { MetricAssetViewModel, MetricFieldTypeViewModel, ScoreType, MetricAssetHistoryViewModel, MetricPathOptionViewModel, ScoreTypeAllocation } from '../models/metrics.model';
import { Observable } from 'rxjs';
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';
import { catchError, map, tap } from 'rxjs/operators';

@Injectable({
    providedIn: 'root'
})
export class MetricsService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    public getFieldTypeViewModelsByAssetType(assetTypeUid: string): Observable<MetricFieldTypeViewModel[]> {
        return this.http
            .get<MetricFieldTypeViewModel[]>(`/api/v2/metrics/fields/${assetTypeUid}`)
            .pipe(catchError(err => this.handleError(err)));
    }

    public getActiveAllocationsByAssetUid(uid: string): Observable<ScoreTypeAllocation[]> {

        return this.http
            .get<ScoreTypeAllocation[]>(`/api/v2/scoring/allocations/?assetUid=${uid}&state=1&_order=scoretype&_direction=asc`)
            .pipe(
                map((res: ScoreTypeAllocation[]) => {
                    return res;
                }),
                catchError(err => this.handleError(err))
            );
    }

    public getAllocationByUid(uid: string): Observable<ScoreTypeAllocation> {

        return this.http
            .get<ScoreTypeAllocation[]>(`/api/v2/scoring/allocations/?allocationUid=${uid}`)
            .pipe(
                map((res: ScoreTypeAllocation[]) => {
                    return res[0];
                }),
                catchError(err => this.handleError(err))
            );
    }

    public getMetricsByAllocation(allocationUid: string, includeDisabled: boolean = false): Observable<MetricAssetViewModel[]> {

        return this.http
            .get<MetricAssetViewModel[]>(`/api/v2/scoring/allocations/${allocationUid}/structure?_includeDisabled=${includeDisabled}`)
            .pipe(
                tap((res: MetricAssetViewModel[]) => {
                    res.forEach((r) => {
                        //r.HasThreshold = (r.Threshold && r.Threshold !== undefined && r.Threshold > 0)
                        if (!r.Threshold || r.Threshold === undefined || r.Threshold <= 0) {
                            r.HasThreshold = false;
                        }
                        else {
                            r.HasThreshold = true;
                        }
                    });
                }),
                catchError(err => this.handleError(err))
            );
    }

    public getRuleResultPathOptions(assetTypeUid: string, type: ScoreType): Observable<MetricPathOptionViewModel[]> {

        return this.http
            .get<MetricPathOptionViewModel[]>(`/api/v2/metrics/${assetTypeUid}/${type}/pathoptions`)
            .pipe(catchError(err => this.handleError(err)));
    }

    public getRuleResultPathOptionFields(ruleResultPathUid: string): Observable<MetricFieldTypeViewModel[]> {

        return this.http
            .get<MetricFieldTypeViewModel[]>(`/api/v2/metrics/pathoptions/${ruleResultPathUid}/fields`)
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