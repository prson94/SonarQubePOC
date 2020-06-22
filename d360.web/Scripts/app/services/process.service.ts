import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import { DiagramNodeBase } from '../models/process.model';


@Injectable()
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
}
