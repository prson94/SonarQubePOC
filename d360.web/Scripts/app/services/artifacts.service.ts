import { Observable } from "rxjs";
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Injectable } from '@angular/core';

import { Artifacts, Artifact } from '../models/artifacts.model';
import { ArtifactType } from '../models/artifact-type.model';
import { SortOrder } from '../models/enums.model';
import {
    GridFilterExpression,
    GridRelationshipFilterExpression,
    GridFilterFieldType,
    GridOwnerFilter
} from '../models/grid-definition.model';
import { Count } from '../models/counts.model';
import { JsonResult } from '../models/jsonresult.model';
import { AssetDetail } from '../models/asset.model';

import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';
import { ApiResult } from "../models/apiresult.model";

@Injectable()
export class ArtifactService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        protected messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    downloadFile(
        data: Blob,
        artifactTypeName: string
    ) {
        console.log("Downloading file");
        var filename = `Filtered ${artifactTypeName} List ${new Date().toDateString()}.xlsx`;

        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename);
        } else {
            var url = window.URL.createObjectURL(data);
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }

    getArtifact(id: number): Observable<Artifact> {
        return this
            .http
            .get(`api/artifact/${id}`)
            .pipe(
                map(response => <Artifact>response),
                catchError(err => this.handleError(err))
            )
            ;
    }

    saveArtifact(artifact: any): Observable<JsonResult> {
        let methodName;

        if (artifact.ID == undefined || !artifact.ID) {
            methodName = 'postDynamic';
        } else {
            methodName = 'putDynamic';
        }

        return this[methodName](this.http, 'artifact', artifact);
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

    getSimilarArtifactNames(
        typeID: number,
        query: string
    ): Observable<any[]> {
        return this
            .http
            .get(`form/Artifact_SimilarItems?typeID=${typeID}&query=${query}`)
            .pipe(
                map(response => <any[]>response),
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
