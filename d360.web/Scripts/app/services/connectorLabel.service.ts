import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';
import { DiagramNodeBase } from '../models/process.model';


@Injectable()
export class ConnectorLabelService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }


    public getAvailableLabels(uid: string, q: string): Observable<any[]> {
        return this
            .http
            .get(`/api/v2/connectorLabels/search?q=` + q)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err, true))
            );
    }

    public createOrGetLabel(q: string): Observable<any> {
        return this
            .http
            .post(`/api/v2/connectorLabels/`, { Value: q })
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err, true))
            );
    }

}
