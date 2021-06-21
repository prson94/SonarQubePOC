import { Observable } from "rxjs";
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { Artifact } from '../models/artifacts.model';
import { Count } from '../models/counts.model';
import { JsonResult } from '../models/jsonresult.model';
import { AssetDetail } from '../models/asset.model';
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';
import { ApiResult } from "../models/apiresult.model";
import { Router } from "@angular/router";

@Injectable({
    providedIn: 'root'
})
export class ArtifactService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        protected messagesService: MessagesObservableService,
        private router: Router
    ) {
        super(messagesService);
    }

    getArtifact(id: number): Observable<Artifact> {
        return this
            .http
            .get(`api/artifact/${id}`)
            .pipe(
                map(response => <Artifact>response),
                catchError((err) => this.handleError(err, false, this.router))
            )
            ;
    }

    getActivityCount(daysToLookBack: number): Observable<Count[]> {
        return this
            .http
            .get(`api/count/activity/${daysToLookBack}`)
            .pipe(
                map(response => <Count[]>response),
                catchError(err => this.handleError(err))
            )
            ;
    }

    getActivityDetails(
        artifactTypeId: number,
        daysToLookBack
    ): Observable<AssetDetail[]> {
        return this
            .http
            .get(`api/countitems/activity/${artifactTypeId}/${daysToLookBack}`)
            .pipe(
                map(response => <AssetDetail[]>response),
                catchError(err => this.handleError(err))
            )
            ;
    }

    public requestCertification(assetUid: string): Observable<JsonResult> {
        const httpOptions = {
            headers: new HttpHeaders({ 'Content-Type': 'application/json' })
        };
        return this
            .http
            .post(`api/v2/assets/RequestCertification/${assetUid}`, httpOptions)
            .pipe(
                map((res: ApiResult) => {
                    return res;
                }),
                catchError(err => this.handleError(err))
            );
    } 

}
