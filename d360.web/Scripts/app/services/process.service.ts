import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import { DiagramNodeBase } from '../models/process.model';


@Injectable({
    providedIn: 'root'
})
export class ProcessService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    public getAvailableNodes(uid: string): Observable<DiagramNodeBase[]> {
        return this
            .http
            .get(`/api/v2/process/${uid}/availableNodes`)
            .pipe(
                map(response => <DiagramNodeBase[]>response),
                catchError(err => this.handleError(err, true))
            );
    }
    public getProcessDiagram(uid: string): Observable<any> {
        return this
            .http
            .get(`/api/v2/process/${uid}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err, true))
            );
    }

    public getProcessDiagramColors(uid: string): Observable<any> {
        return this
            .http
            .get(`/api/v2/process/${uid}/governanceRoleColors`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err, true))
            );
    }

    public getProcessDiagramBadges(uid: string): Observable<any[]> {
        return this
            .http
            .get(`/api/v2/process/${uid}/badges`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err, true))
            );
    }

    public putProcessDiagram(uid: string, model: any): Observable<any> {
        var headers = new HttpHeaders();
        headers.append('Content-Type', 'application/json');
        return this
            .http
            .put(`/api/v2/process/${uid}`, model, { headers: headers })
            .pipe(
                map(response => <any>response)
            );
    }

    public replaceProcessDiagram(targetUid: string, sourceUid: any): Observable<any> {
        var headers = new HttpHeaders();
        headers.append('Content-Type', 'application/json');
        var model = {};
        return this
            .http
            .put(`/api/v2/process/${targetUid}?sourceAssetUid=${sourceUid}`, model, { headers: headers })
            .pipe(
                map(response => <any>response)
            );
    }

    public getProcessUrlByDiagramAssetUid(uid: string): Observable<any> {
        var headers = new HttpHeaders();
        headers.append('Content-Type', 'application/json');
        return this
            .http
            .get(`/api/v2/process/urlByDiagramAsset/${uid}`, { headers: headers })
            .pipe(
                map(response => <any>response)
            );
    }

    public getImportOptions(uid: string): Observable<any> {
        var headers = new HttpHeaders();
        headers.append('Content-Type', 'application/json');
        return this
            .http
            .get(`/api/v2/process/${uid}/importOptions`, { headers: headers })
            .pipe(
                map(response => <any>response)
            );
    }

    public getIgnoredRelationshipsForCopy(targetUid: string): Observable<any[]> {
        var headers = new HttpHeaders();
        headers.append('Content-Type', 'application/json');
        return this
            .http
            .get(`/api/v2/process/ignoredCopyRelationships/${targetUid}`, { headers: headers })
            .pipe(
                map(response => <any[]>response)
            );
    }

    public downloadProcessExcel(assetUid: string, imageData: string): Observable<any> {
        return this.
            http
            .post(`/api/v2/process/export/${assetUid}`, imageData, { headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }), responseType: 'blob' });
    }

    public downloadFile(data: Blob, name: string) {
        super.downloadFile(data, name);
    }
}
