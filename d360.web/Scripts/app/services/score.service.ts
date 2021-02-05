import { Injectable } from "@angular/core";
import { PointBreakdown, ScorePoint, AverageScore, DataQualityEvidenceModel } from "../models/score.model";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { catchError, map } from "rxjs/operators";
import { Observable, Subject, of } from "rxjs";
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from "./messages-observable.service";
import { ScoreType } from "../models/metrics.model";

@Injectable()
export class ScoreService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getDataQualityEvidenceForScoreItem(scoreItemUid: string, simpleFilter: string): Observable<DataQualityEvidenceModel> {
        let url: string = `/api/v2/scoring/${scoreItemUid}/quality/evidence`;
        if (simpleFilter && simpleFilter !== "") {
            url += "?_simpleFilter=" + simpleFilter;
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
                map(response => <PointBreakdown[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getScoreHistory(type: ScoreType, assetUid: string): Observable<ScorePoint[]> {
        return this.http.get(`/api/v2/metrics/history/${type}/${assetUid}`)
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

    getScoreDefinition(assetUid: string): Observable<any> {
        return this.http.get(`/api/v2/metrics/${assetUid}/definition`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    //getScoreTypes(assetUid: string): Observable<any[]> {
    //    return this.http.get(`/api/v2/metrics/ScoreTypes/${assetUid}`)
    //        .pipe(
    //            map(response => <any>response),
    //            catchError(err => this.handleError(err))
    //        );
    //}

    getAssetScoreGraphPoints(assetUid: string, type: ScoreType): Observable<number[]> {
        return this.http.get(`/api/v2/metrics/${type}/${assetUid}/graphPoints`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }
}