import { Injectable } from "@angular/core";
import { PointBreakdown, ScorePoint, AverageScore, DataQualityEvidenceModel } from "../models/score.model";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { catchError, map } from "rxjs/operators";
import { Observable, Subject, of } from "rxjs";
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from "./messages-observable.service";
import { ScoreType } from "../models/metrics.model";

@Injectable({
    providedIn: 'root'
})
export class ScoreService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getDataQualityEvidenceForScoreItem(scoreItemUid: string, pageNumber: number, pageSize: number, simpleFilter: string, sortField: string = 'OwningAssetDisplayPath', sortOrder: string = 'asc'): Observable<DataQualityEvidenceModel> {
        let url: string = `/api/v2/scoring/${scoreItemUid}/quality/evidence`;
        if (!pageSize) {
            pageSize = 200;
        }
        if (!pageNumber || pageNumber <= 0) {
            pageNumber = 1;
        }
        url += "?_pageNum=" + pageNumber;
        url += "&_pageSize=" + pageSize;
        if (sortField) {
            url += "&_order=" + sortField;
        }
        if (sortOrder) {
            url += "&_sort=" + sortOrder;
        }
        if (simpleFilter && simpleFilter !== "") {
            url += "&_simpleFilter=" + simpleFilter;
        }
        return this.http.get(url)
            .pipe(
                map((response) => <DataQualityEvidenceModel>response),
                catchError((err) => this.handleError(err))
            );
    }

    public getDataQualityEvidenceForScoreItemExcel(scoreItemUid: string, simpleFilter: string) {
        let url: string = `/api/v2/scoring/${scoreItemUid}/quality/evidence`;
        if (simpleFilter && simpleFilter !== "") {
            url += "?_simpleFilter=" + simpleFilter;
        }
        this.http.get(url, { headers: new HttpHeaders({ "Accept": "application/octet-stream" }), responseType: "blob" })
            .subscribe((data) => this.downloadFile(data, "Rule Results"));
    }

    getPointBreakdown(assetUid: string, type: ScoreType, date: string = null): Observable<PointBreakdown[]> {
        let uri = `/api/v2/metrics/${type}/${assetUid}/pointbreakdown` + (date == null ? "" : `?effectiveDate=${date}`);
        return this.http.get(uri)
            .pipe(
                map((response) => <PointBreakdown[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getScoreHistory(type: ScoreType, assetUid: string): Observable<ScorePoint[]> {
        return this.http.get(`/api/v2/metrics/history/${type}/${assetUid}`)
            .pipe(
                map((response) => <ScorePoint[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getScoreHistoryByAllocationAndAsset(allocationUid: string, assetUid: string): Observable<ScorePoint[]> {
        return this.http.get(`/api/v2/scoring/history/${allocationUid}/${assetUid}/scores`)
            .pipe(
                map((response) => <ScorePoint[]>response),
                catchError((err) => this.handleError(err))
            );
    }

    getScoreHistoryByAllocationAndAssetAndEffectiveDate(allocationUid: string, assetUid: string, effectiveDate: string): Observable<ScorePoint> {
        return this.http.get(`/api/v2/scoring/history/${allocationUid}/${assetUid}/scores` + (effectiveDate == null ? "" : `?effectiveDate=${effectiveDate}`))
            .pipe(
                map((response) => <ScorePoint>response[0]),
                catchError((err) => this.handleError(err))
            );
    }

    getAverageScore(assetUid: string): Observable<AverageScore> {
        return this.http.get(`queries/${assetUid}/AverageScoreByObjectType`)
            .pipe(
                map((response) => <AverageScore>response),
                catchError((err) => this.handleError(err))
            );
    }

    getAssetScoreGraphPoints(assetUid: string, type: ScoreType): Observable<number[]> {
        return this.http.get(`/api/v2/metrics/${type}/${assetUid}/graphPoints`)
            .pipe(
                map((response) => <any>response),
                catchError((err) => this.handleError(err))
            );
    }

    recalculateMeasure(allocationUid: string, measureUid: string): Observable<boolean> {
        return this.http.put(`/api/v2/scoring/${allocationUid}/measures/${measureUid}/recalculations`, {})
            .pipe(
                map(() => true),
                catchError((err) => this.handleError(err))
            );
    }

}